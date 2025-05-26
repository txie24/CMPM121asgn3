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
        Debug.Log($"[EnemySpawner] sprite idx={idx} for class='{cls}'");
        var sr = GameManager.Instance.player
                                 .GetComponent<SpriteRenderer>();
        if (sr == null)
            Debug.LogError("StartLevel: no SpriteRenderer on player");
        else
            sr.sprite = GameManager.Instance
                             .playerSpriteManager
                             .Get(idx);

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
        var pc = GameManager.Instance.player
                              .GetComponent<PlayerController>();
        if (pc == null) throw new InvalidOperationException("no PlayerController");

        string cls = ChooseClassManager.SelectedClass ?? "mage";
        var stats = PlayerClass.GetStatsForWave(cls, wave);
        Debug.Log($"[EnemySpawner] wave={wave}, class={cls}, stats={string.Join(",", stats.Select(kv => $"{kv.Key}={kv.Value}"))}");

        // apply values
        pc.hp.SetMaxHP(Mathf.RoundToInt(stats["health"]), true);
        pc.spellcaster.max_mana = Mathf.RoundToInt(stats["mana"]);
        pc.spellcaster.mana = pc.spellcaster.max_mana;
        pc.spellcaster.mana_reg = Mathf.RoundToInt(stats["mana_regeneration"]);
        pc.spellcaster.spellPower = Mathf.RoundToInt(stats["spellpower"]);
        pc.speed = Mathf.RoundToInt(stats["speed"]);

        // update UI
        pc.healthui?.SetHealth(pc.hp);
        pc.manaui?.SetSpellCaster(pc.spellcaster);
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
        int hp = spawn.hp != null ? RPNEvaluator.SafeEvaluate(spawn.hp, ivars, def.hp) : def.hp;
        float spd = spawn.speed != null ? RPNEvaluator.SafeEvaluate(spawn.speed, new Dictionary<string, int> { { "base", (int)def.speed }, { "wave", currentWave } }, (int)def.speed) : def.speed;
        float delay = spawn.delay != null ? RPNEvaluator.SafeEvaluate(spawn.delay, ivars, 2) : 2f;

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
                var ofs = UnityEngine.Random.insideUnitCircle * 3f;  // fully qualified now
                var go = Instantiate(enemy, pt.transform.position + (Vector3)ofs, Quaternion.identity);

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

    private SpawnPoint PickSpawnPoint(string loc)
    {
        if (string.IsNullOrEmpty(loc) || loc == "random")
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];

        var kind = loc.Split(' ')[1].ToUpperInvariant();
        var list = SpawnPoints.Where(sp => sp.kind.ToString().ToUpperInvariant() == kind).ToList();
        return list.Count > 0
            ? SpawnPoints[UnityEngine.Random.Range(0, list.Count)]
            : SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
    }
}
