// Assets/Scripts/Levels/EnemySpawner.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EnemySpawner : MonoBehaviour
{
    // hooks for relics (and anything else)
    public static Action<int> OnWaveEnd;
    public static Action<GameObject> OnEnemyKilled;

    [Header("Level Selection UI")]
    public Image level_selector;
    public GameObject button;

    [Header("Enemy Prefab & Spawn Points")]
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    public Level currentLevel { get; private set; }
    public int currentWave { get; private set; }
    public int lastWaveEnemyCount { get; private set; }

    private bool _waveInProgress = false;
    private bool isEndless => currentLevel != null && currentLevel.waves <= 0;

    void Start()
    {
        // build the level-select buttons
        for (int i = 0; i < GameManager.Instance.levelDefs.Count; i++)
        {
            var lvl = GameManager.Instance.levelDefs[i];
            var go = Instantiate(button, level_selector.transform);
            go.transform.localPosition = new Vector3(0, 130 - 100 * i, 0);

            var ctrl = go.GetComponent<MenuSelectorController>();
            ctrl.spawner = this;
            ctrl.SetLevel(lvl.name);

            go.GetComponent<Button>()
              .onClick.AddListener(ctrl.StartLevel);
        }
    }

    // called by ChooseClassManager after you pick a class
    public void StartLevel(string levelName)
    {
        level_selector.gameObject.SetActive(false);
        currentLevel = GameManager.Instance.levelDefs
                         .FirstOrDefault(l => l.name == levelName);
        if (currentLevel == null)
        {
            Debug.LogError($"StartLevel: '{levelName}' not found");
            return;
        }

        currentWave = 1;

        // initialize player
        var pc = GameManager.Instance.player
                              .GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogError("StartLevel: no PlayerController on player");
            return;
        }
        pc.StartLevel();

        // swap sprite based on chosen class
        string cls = ChooseClassManager.SelectedClass ?? "mage";
        int idx = PlayerClass.GetSpriteIndex(cls);
        //Debug.Log($"[EnemySpawner] sprite idx={idx} for class='{cls}'");

        // **explicitly find your child named "player sprite"**
        var spriteGO = GameManager.Instance.player
                            .transform.Find("player sprite");
        if (spriteGO == null)
        {
            Debug.LogError("StartLevel: could not find child named 'player sprite'");
        }
        else
        {
            var sr = spriteGO.GetComponent<SpriteRenderer>();
            if (sr == null)
                Debug.LogError("StartLevel: 'player sprite' has no SpriteRenderer");
            else
                sr.sprite = GameManager.Instance
                              .playerSpriteManager
                              .Get(idx);
        }

        // begin spawning
        StartCoroutine(SpawnWave());
    }

    // external UI calls this to go to next wave
    public void NextWave()
    {
        if (!_waveInProgress)
            StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        if (_waveInProgress) yield break;
        _waveInProgress = true;

        // 1) scale player stats for this wave
        SafeScalePlayerForWave(currentWave);

        // 2) 3-2-1 countdown
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        for (int i = 3; i > 0; i--)
        {
            GameManager.Instance.countdown = i;
            yield return new WaitForSeconds(1f);
        }
        GameManager.Instance.countdown = 0;

        // 3) mark in-wave state
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        // 4) spawn all configured enemies
        int totalSpawned = 0;
        foreach (var s in currentLevel.spawns)
            yield return StartCoroutine(SpawnEnemies(s, c => totalSpawned += c));

        lastWaveEnemyCount = totalSpawned;

        // 5) wait until all are dead
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);

        // 6) check win in finite mode
        if (!isEndless && currentWave >= currentLevel.waves)
        {
            GameManager.Instance.playerWon = true;
            GameManager.Instance.IsPlayerDead = false;
            GameManager.Instance.state = GameManager.GameState.GAMEOVER;
            yield break;
        }

        // 7) wave-end: show rewards
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        GameManager.Instance.wavesCompleted++;
        OnWaveEnd?.Invoke(currentWave);

        // 8) prep next
        currentWave++;
        _waveInProgress = false;
    }

    private void SafeScalePlayerForWave(int wave)
    {
        try
        {
            ScalePlayerForWave(wave);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SafeScalePlayerForWave error: {ex}");
        }
    }

    private void ScalePlayerForWave(int wave)
    {
        var pc = GameManager.Instance.player.GetComponent<PlayerController>();
        var stats = PlayerClass.GetStatsForWave(ChooseClassManager.SelectedClass, wave);

        int baseHP = Mathf.RoundToInt(stats["health"]);
        int bonus = pc.relicMaxHPBonus;              // <- grab your Titan’s Heart stacks
        int total = baseHP + bonus;

        Debug.Log($"[EnemySpawner] wave={wave} baseHP={baseHP} + relicBonus={bonus} => totalHP={total}");
        pc.hp.SetMaxHP(total, true);

        pc.spellcaster.max_mana = Mathf.RoundToInt(stats["mana"]);
        pc.spellcaster.mana = pc.spellcaster.max_mana;
        pc.spellcaster.mana_reg = Mathf.RoundToInt(stats["mana_regeneration"]);
        pc.spellcaster.spellPower = Mathf.RoundToInt(stats["spellpower"]);
        pc.speed = Mathf.RoundToInt(stats["speed"]);
        pc.healthui?.SetHealth(pc.hp);
        pc.manaui?.SetSpellCaster(pc.spellcaster);
        Debug.Log($"[EnemySpawner] After wave {wave} scaling → HP = {pc.hp.hp}/{pc.hp.max_hp}");

    }

    private IEnumerator SpawnEnemies(Spawn spawn, Action<int> onComplete)
    {
        var def = GameManager.Instance.enemyDefs
                         .FirstOrDefault(e => e.name == spawn.enemy);
        if (def == null)
        {
            onComplete?.Invoke(0);
            yield break;
        }

        var ivars = new Dictionary<string, int> { ["base"] = def.hp, ["wave"] = currentWave };
        int total = RPNEvaluator.SafeEvaluate(spawn.count, ivars, 0);
        int hp = spawn.hp != null
                      ? RPNEvaluator.SafeEvaluate(spawn.hp, ivars, def.hp)
                      : def.hp;
        float spd = spawn.speed != null
                      ? RPNEvaluator.SafeEvaluate(spawn.speed,
                          new Dictionary<string, int> { { "base", (int)def.speed }, { "wave", currentWave } },
                          (int)def.speed)
                      : def.speed;
        float delay = spawn.delay != null
                      ? RPNEvaluator.SafeEvaluate(spawn.delay, ivars, 2)
                      : 2f;

        spd = Mathf.Clamp(spd, 1f, 20f);
        var seq = (spawn.sequence != null && spawn.sequence.Count > 0)
                  ? spawn.sequence
                  : new List<int> { 1 };

        int spawned = 0, seqIdx = 0;
        while (spawned < total)
        {
            int batch = seq[seqIdx++ % seq.Count];
            for (int i = 0; i < batch && spawned < total; i++)
            {
                var pt = PickSpawnPoint(spawn.location);
                Vector3 spawnPos = FindValidSpawnPosition(pt.transform.position);
                var go = Instantiate(enemy, spawnPos, Quaternion.identity);

                // set enemy sprite
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = GameManager.Instance.enemySpriteManager.Get(def.sprite);

                // init health & speed
                var ec = go.GetComponent<EnemyController>();
                ec.hp = new Hittable(hp, Hittable.Team.MONSTERS, go);
                ec.speed = Mathf.RoundToInt(spd);

                GameManager.Instance.AddEnemy(go);
                spawned++;
            }
            yield return new WaitForSeconds(delay);
        }

        onComplete?.Invoke(spawned);
    }

    private Vector3 FindValidSpawnPosition(Vector3 basePosition)
    {
        // Try the base position first
        if (!IsPositionBlocked(basePosition))
            return basePosition;

        // Try nearby positions with increasing distance
        for (float radius = 1f; radius <= 5f; radius += 0.5f)
        {
            for (int attempts = 0; attempts < 8; attempts++)
            {
                float angle = attempts * 45f * Mathf.Deg2Rad;
                Vector3 testPos = basePosition + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f
                );

                if (!IsPositionBlocked(testPos))
                    return testPos;
            }
        }

        // Fallback: return base position with small random offset
        return basePosition + (Vector3)UnityEngine.Random.insideUnitCircle * 0.5f;
    }

    private bool IsPositionBlocked(Vector3 position)
    {
        // Check for colliders at this position (walls, other enemies, etc.)
        Collider2D hit = Physics2D.OverlapCircle(position, 0.4f);
        return hit != null && !hit.CompareTag("unit") && !hit.CompareTag("projectile");
    }

    private SpawnPoint PickSpawnPoint(string loc)
    {
        if (string.IsNullOrEmpty(loc) || loc == "random")
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];

        var kind = loc.Split(' ')[1].ToUpperInvariant();
        var list = SpawnPoints
            .Where(sp => sp.kind.ToString().ToUpperInvariant() == kind)
            .ToList();
        return list.Count > 0
            ? SpawnPoints[UnityEngine.Random.Range(0, list.Count)]
            : SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
    }
}