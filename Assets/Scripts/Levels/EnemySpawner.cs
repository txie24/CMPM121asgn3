using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    public Level currentLevel { get; private set; }
    public int currentWave { get; private set; }
    public int lastWaveEnemyCount { get; private set; }
    private bool isEndless => currentLevel != null && currentLevel.waves <= 0;

    private bool waveInProgress = false;

    private void TriggerWin()
    {
        GameManager.Instance.playerWon = true;
        GameManager.Instance.IsPlayerDead = false;
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        Debug.Log("✅ You Win: all waves completed.");
    }

    void Start()
    {
        foreach (var lvl in GameManager.Instance.levelDefs)
        {
            GameObject selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition =
                new Vector3(0, 130 - 100 * GameManager.Instance.levelDefs.IndexOf(lvl));
            var controller = selector.GetComponent<MenuSelectorController>();
            controller.spawner = this;
            controller.SetLevel(lvl.name);
            selector.GetComponent<Button>().onClick.AddListener(controller.StartLevel);
        }
    }

    public void StartLevel(string levelname)
    {
        Debug.Log($"[EnemySpawner] StartLevel() called for '{levelname}'");
        level_selector.gameObject.SetActive(false);

        currentLevel = GameManager.Instance.levelDefs
            .Find(l => l.name == levelname);
        if (currentLevel == null)
        {
            Debug.LogError($"StartLevel failed: level '{levelname}' not found.");
            return;
        }

        currentWave = 1;

        // Initialize player stats for wave 1
        var playerController = GameManager.Instance.player.GetComponent<PlayerController>();
        playerController.StartLevel(); // Initialize components and UI
        ScalePlayerForWave(currentWave); // Set initial stats using RPN

        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        if (!waveInProgress)
            StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        Debug.Log($"[EnemySpawner] SpawnWave() entry (wave {currentWave}), inProgress={waveInProgress}");
        if (waveInProgress) yield break;
        waveInProgress = true;

        // Scale player stats at the beginning of every wave
        ScalePlayerForWave(currentWave);

        // COUNTDOWN
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        for (int i = 3; i > 0; i--)
        {
            GameManager.Instance.countdown = i;
            yield return new WaitForSeconds(1);
        }

        GameManager.Instance.countdown = 0;
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        // SPAWN ENEMIES
        int totalSpawned = 0;
        foreach (var spawn in currentLevel.spawns)
            yield return StartCoroutine(SpawnEnemies(spawn, c => totalSpawned += c));

        lastWaveEnemyCount = totalSpawned;

        // WAIT FOR ALL ENEMIES DEAD
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);

        // WIN CHECK
        if (!isEndless && currentWave >= currentLevel.waves)
        {
            TriggerWin();
            yield break;
        }

        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        GameManager.Instance.wavesCompleted++;

        // PREPARE FOR NEXT WAVE
        currentWave++;
        waveInProgress = false;
    }

    IEnumerator SpawnEnemies(Spawn spawn, System.Action<int> onSpawnComplete = null)
    {
        var baseEnemy = GameManager.Instance.enemyDefs
                           .Find(e => e.name == spawn.enemy);
        if (baseEnemy == null) yield break;

        var vars = new Dictionary<string, int> {
            { "base", baseEnemy.hp },
            { "wave", currentWave }
        };

        int total = RPNEvaluator.SafeEvaluate(spawn.count, vars, 0);
        int hp = spawn.hp != null 
            ? RPNEvaluator.SafeEvaluate(spawn.hp, vars, baseEnemy.hp) 
            : baseEnemy.hp;
        float speed = spawn.speed != null
            ? RPNEvaluator.SafeEvaluate(spawn.speed, new() {
                  { "base", (int)baseEnemy.speed }, { "wave", currentWave }
              }, (int)baseEnemy.speed)
            : baseEnemy.speed;
        int damage = spawn.damage != null
            ? RPNEvaluator.SafeEvaluate(spawn.damage, new() {
                  { "base", baseEnemy.damage }, { "wave", currentWave }
              }, baseEnemy.damage)
            : baseEnemy.damage;
        float delay = spawn.delay != null
            ? RPNEvaluator.SafeEvaluate(spawn.delay, vars, 2)
            : 2f;

        Debug.Log($"[Spawn] {total}×'{spawn.enemy}' (HP={hp}, Spd={speed}, Dmg={damage}) @ wave {currentWave}");

        speed = Mathf.Clamp(speed, 1f, 20f);
        var seq = (spawn.sequence != null && spawn.sequence.Count > 0)
                  ? spawn.sequence
                  : new List<int> { 1 };

        int spawned = 0, idx = 0;
        while (spawned < total)
        {
            int batch = seq[idx++ % seq.Count];
            for (int i = 0; i < batch && spawned < total; i++)
            {
                var pt = PickSpawnPoint(spawn.location);
                var ofs = GetNonOverlappingOffset(pt.transform.position);
                var pos = pt.transform.position + (Vector3)ofs;
                var go = Instantiate(enemy, pos, Quaternion.identity);
                go.GetComponent<SpriteRenderer>()
                  .sprite = GameManager.Instance.enemySpriteManager.Get(baseEnemy.sprite);

                var en = go.GetComponent<EnemyController>();
                en.hp = new Hittable(hp, Hittable.Team.MONSTERS, go);
                en.speed = (int)speed;
                GameManager.Instance.AddEnemy(go);

                spawned++;
            }
            yield return new WaitForSeconds(delay);
        }

        onSpawnComplete?.Invoke(spawned);
    }

    private Vector2 GetNonOverlappingOffset(Vector3 center)
    {
        for (int i = 0; i < 10; i++)
        {
            var ofs = Random.insideUnitCircle * 3f;
            if (Physics2D.OverlapCircleAll(center + (Vector3)ofs, .75f).Length == 0)
                return ofs;
        }
        return Random.insideUnitCircle * 3f;
    }

    private SpawnPoint PickSpawnPoint(string loc)
    {
        if (string.IsNullOrEmpty(loc) || !loc.StartsWith("random"))
            return SpawnPoints[Random.Range(0, SpawnPoints.Length)];
        if (loc == "random")
            return SpawnPoints[Random.Range(0, SpawnPoints.Length)];

        var kind = loc.Split(' ')[1].Trim().ToUpperInvariant();
        var matches = SpawnPoints
            .Where(sp => sp.kind.ToString().ToUpperInvariant() == kind)
            .ToList();
        return matches.Count > 0
            ? matches[Random.Range(0, matches.Count)]
            : SpawnPoints[Random.Range(0, SpawnPoints.Length)];
    }

    private void ScalePlayerForWave(int wave)
    {
        Debug.Log($"[EnemySpawner] ScalePlayerForWave({wave})");

        var v = new Dictionary<string, float> { { "wave", wave } };
        float rHP = RPNEvaluator.EvaluateFloat("95 wave 5 * +", v);
        float rMana = RPNEvaluator.EvaluateFloat("90 wave 10 * +", v);
        float rRe = RPNEvaluator.EvaluateFloat("10 wave +", v);
        float rPow = RPNEvaluator.EvaluateFloat("wave 10 *", v);
        float rSpd = RPNEvaluator.EvaluateFloat("5", v);

        var pc = GameManager.Instance.player.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogError("ScalePlayerForWave: no PlayerController!");
            return;
        }

        // Update HP
        pc.hp.SetMaxHP(Mathf.RoundToInt(rHP), true); // true to refill HP

        // Update mana and regen
        pc.spellcaster.max_mana = Mathf.RoundToInt(rMana);
        pc.spellcaster.mana = pc.spellcaster.max_mana; // Refill mana
        pc.spellcaster.mana_reg = Mathf.RoundToInt(rRe);

        // Update spell power
        pc.spellcaster.spellPower = Mathf.RoundToInt(rPow);

        // Update movement speed
        pc.speed = Mathf.RoundToInt(rSpd);

        // Update UI to reflect new stats
        pc.healthui.SetHealth(pc.hp);
        pc.manaui.SetSpellCaster(pc.spellcaster);

        Debug.Log($" → PlayerStats: HP={rHP:F1}, Mana={rMana:F1}, Regen={rRe:F1}, " +
                  $"Power={rPow:F1}, Speed={rSpd:F1}");
    }
}