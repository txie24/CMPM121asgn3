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
    private ChooseClassManager classManager;

    private void TriggerWin()
    {
        GameManager.Instance.playerWon = true;
        GameManager.Instance.IsPlayerDead = false;
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        Debug.Log(" You Win: all waves completed.");
    }

    void Awake()
    {
        // Find the ChooseClassManager
        classManager = FindObjectOfType<ChooseClassManager>();
        if (classManager == null)
        {
            Debug.LogWarning("EnemySpawner: No ChooseClassManager found in scene!");
        }
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

        // Initialize player
        var playerController = GameManager.Instance.player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.StartLevel();
        }
        else
        {
            Debug.LogError("StartLevel: Could not find PlayerController on player.");
        }

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

        // Scale player stats - do this safely
        SafeScalePlayerForWave(currentWave);

        // 2) Countdown
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        for (int i = 3; i > 0; i--)
        {
            GameManager.Instance.countdown = i;
            yield return new WaitForSeconds(1);
        }
        GameManager.Instance.countdown = 0;

        // 3) In‑wave
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        // 4) Spawn
        int totalSpawned = 0;
        foreach (var spawn in currentLevel.spawns)
            yield return StartCoroutine(SpawnEnemies(spawn, c => totalSpawned += c));
        lastWaveEnemyCount = totalSpawned;

        // 5) Wait for clear
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);

        // 6) Win check (only non‑Endless)
        if (!isEndless && currentWave >= currentLevel.waves)
        {
            TriggerWin();
            yield break;
        }

        // 7) Reward screen
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        GameManager.Instance.wavesCompleted++;

        // 8) Prep next wave exactly like Easy/Medium
        currentWave++;
        waveInProgress = false;
    }

    // A wrapper that ensures we never crash during player scaling
    private void SafeScalePlayerForWave(int wave)
    {
        try
        {
            // Get the player controller
            if (GameManager.Instance == null || GameManager.Instance.player == null)
            {
                Debug.LogError("SafeScalePlayerForWave: GameManager.Instance.player is null!");
                return;
            }

            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            if (pc == null)
            {
                Debug.LogError("SafeScalePlayerForWave: PlayerController not found!");
                return;
            }

            // Initialize components
            pc.InitializeComponents();

            // Now call the regular method
            ScalePlayerForWave(wave);
        }
        catch (System.Exception e)
        {
            // Log but don't crash
            Debug.LogError($"Error in SafeScalePlayerForWave: {e.Message}\n{e.StackTrace}");
        }
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

        // Get the player controller
        var pc = GameManager.Instance.player.GetComponent<PlayerController>();
        if (pc == null || pc.hp == null || pc.spellcaster == null)
        {
            Debug.LogError("ScalePlayerForWave: Player controller or its components are null!");
            return;
        }

        // Get the selected class
        string className = ChooseClassManager.SelectedClass ?? "mage";
        Debug.Log($"[EnemySpawner] Using class: {className}");

        // Get class stats from ChooseClassManager
        Dictionary<string, float> classStats;
        if (classManager != null)
        {
            classStats = classManager.GetClassStatsForWave(className, wave);
            Debug.Log($"[EnemySpawner] Got class stats from ChooseClassManager: {string.Join(", ", classStats.Select(kv => $"{kv.Key}={kv.Value}"))}");
        }
        else
        {
            // Fallback to default stats if ChooseClassManager not available
            Debug.LogWarning("ScalePlayerForWave: No ChooseClassManager available, using default stats");
            var v = new Dictionary<string, float> { { "wave", wave } };
            classStats = new Dictionary<string, float>
            {
                { "health", RPNEvaluator.SafeEvaluateFloat("95 wave 5 * +", v) },
                { "mana", RPNEvaluator.SafeEvaluateFloat("90 wave 10 * +", v) },
                { "mana_regeneration", RPNEvaluator.SafeEvaluateFloat("10 wave +", v) },
                { "spellpower", RPNEvaluator.SafeEvaluateFloat("wave 10 *", v) },
                { "speed", RPNEvaluator.SafeEvaluateFloat("5", v) }
            };
        }

        // Apply stats
        pc.hp.SetMaxHP(Mathf.RoundToInt(classStats["health"]), true);
        pc.spellcaster.max_mana = Mathf.RoundToInt(classStats["mana"]);
        pc.spellcaster.mana = pc.spellcaster.max_mana;
        pc.spellcaster.mana_reg = Mathf.RoundToInt(classStats["mana_regeneration"]);
        pc.spellcaster.spellPower = Mathf.RoundToInt(classStats["spellpower"]);
        pc.speed = Mathf.RoundToInt(classStats["speed"]);

        // Update UI
        if (pc.healthui != null)
            pc.healthui.SetHealth(pc.hp);

        if (pc.manaui != null)
            pc.manaui.SetSpellCaster(pc.spellcaster);

        Debug.Log($" → PlayerStats ({className}): HP={pc.hp.hp}/{pc.hp.max_hp}, Mana={classStats["mana"]:F1}, " +
                  $"Regen={classStats["mana_regeneration"]:F1}, Power={classStats["spellpower"]:F1}, Speed={classStats["speed"]:F1}");
    }
}