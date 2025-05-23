// Assets/Scripts/Levels/EnemySpawner.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

public class EnemySpawner : MonoBehaviour
{
    // relic hooks (no longer events so you can invoke from GameManager)
    public static Action<int> OnWaveEnd;
    public static Action<GameObject> OnEnemyKilled;

    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    public Level currentLevel { get; private set; }
    public int currentWave { get; private set; }
    public int lastWaveEnemyCount { get; private set; }
    bool isEndless => currentLevel != null && currentLevel.waves <= 0;

    bool waveInProgress = false;
    ChooseClassManager classManager;

    void Awake()
    {
        classManager = FindFirstObjectByType<ChooseClassManager>();
        if (classManager == null)
            Debug.LogWarning("EnemySpawner: No ChooseClassManager found in scene!");
    }

    void Start()
    {
        foreach (var lvl in GameManager.Instance.levelDefs)
        {
            var selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition =
                new Vector3(0, 130 - 100 * GameManager.Instance.levelDefs.IndexOf(lvl));
            var ctrl = selector.GetComponent<MenuSelectorController>();
            ctrl.spawner = this;
            ctrl.SetLevel(lvl.name);
            selector.GetComponent<Button>().onClick.AddListener(ctrl.StartLevel);
        }
    }

    public void StartLevel(string levelname)
    {
        level_selector.gameObject.SetActive(false);
        currentLevel = GameManager.Instance.levelDefs.Find(l => l.name == levelname);
        if (currentLevel == null)
        {
            Debug.LogError($"StartLevel: '{levelname}' not found");
            return;
        }

        currentWave = 1;
        var pc = GameManager.Instance.player.GetComponent<PlayerController>();
        if (pc != null) pc.StartLevel();
        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        if (!waveInProgress) StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        if (waveInProgress) yield break;
        waveInProgress = true;

        SafeScalePlayerForWave(currentWave);

        // countdown
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        for (int i = 3; i > 0; i--)
        {
            GameManager.Instance.countdown = i;
            yield return new WaitForSeconds(1);
        }
        GameManager.Instance.countdown = 0;

        // in-wave
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        // spawn batches
        int totalSpawned = 0;
        foreach (var spawn in currentLevel.spawns)
            yield return StartCoroutine(SpawnEnemies(spawn, c => totalSpawned += c));
        lastWaveEnemyCount = totalSpawned;

        // wait clear
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);

        // win check
        if (!isEndless && currentWave >= currentLevel.waves)
        {
            GameManager.Instance.playerWon = true;
            GameManager.Instance.IsPlayerDead = false;
            GameManager.Instance.state = GameManager.GameState.GAMEOVER;
            yield break;
        }

        // reward screen
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        GameManager.Instance.wavesCompleted++;

        // fire relic hook
        OnWaveEnd?.Invoke(currentWave);

        // prep next
        currentWave++;
        waveInProgress = false;
    }

    private void SafeScalePlayerForWave(int wave)
    {
        try
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.player == null) return;
            var pc = gm.player.GetComponent<PlayerController>();
            if (pc == null) return;
            pc.InitializeComponents();
            ScalePlayerForWave(wave);
        }
        catch (Exception e)
        {
            Debug.LogError($"SafeScalePlayerForWave error: {e}");
        }
    }

    IEnumerator SpawnEnemies(Spawn spawn, Action<int> done)
    {
        var def = GameManager.Instance.enemyDefs.Find(e => e.name == spawn.enemy);
        if (def == null) yield break;

        var vars = new Dictionary<string, int> { { "base", def.hp }, { "wave", currentWave } };
        int total = RPNEvaluator.SafeEvaluate(spawn.count, vars, 0);
        int hp = spawn.hp != null ? RPNEvaluator.SafeEvaluate(spawn.hp, vars, def.hp) : def.hp;
        float spd = spawn.speed != null ? RPNEvaluator.SafeEvaluate(spawn.speed, new Dictionary<string, int> { { "base", (int)def.speed }, { "wave", currentWave } }, (int)def.speed) : def.speed;
        float delay = spawn.delay != null ? RPNEvaluator.SafeEvaluate(spawn.delay, vars, 2) : 2f;

        spd = Mathf.Clamp(spd, 1f, 20f);
        var seq = (spawn.sequence != null && spawn.sequence.Count > 0) ? spawn.sequence : new List<int> { 1 };

        int spawned = 0, idx = 0;
        while (spawned < total)
        {
            int batch = seq[idx++ % seq.Count];
            for (int i = 0; i < batch && spawned < total; i++)
            {
                var pt = PickSpawnPoint(spawn.location);
                var ofs = GetNonOverlappingOffset(pt.transform.position);
                var go = Instantiate(enemy, pt.transform.position + (Vector3)ofs, Quaternion.identity);
                go.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(def.sprite);
                var ec = go.GetComponent<EnemyController>();
                ec.hp = new Hittable(hp, Hittable.Team.MONSTERS, go);
                ec.speed = (int)spd;
                GameManager.Instance.AddEnemy(go);
                spawned++;
            }
            yield return new WaitForSeconds(delay);
        }
        done?.Invoke(spawned);
    }

    Vector2 GetNonOverlappingOffset(Vector3 center)
    {
        for (int i = 0; i < 10; i++)
        {
            var ofs = UnityEngine.Random.insideUnitCircle * 3f;
            if (Physics2D.OverlapCircleAll(center + (Vector3)ofs, .75f).Length == 0)
                return ofs;
        }
        return UnityEngine.Random.insideUnitCircle * 3f;
    }

    SpawnPoint PickSpawnPoint(string loc)
    {
        if (string.IsNullOrEmpty(loc) || !loc.StartsWith("random"))
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        if (loc == "random")
            return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        var kind = loc.Split(' ')[1].ToUpperInvariant();
        var matches = SpawnPoints.Where(sp => sp.kind.ToString().ToUpperInvariant() == kind).ToList();
        return matches.Count > 0 ? matches[UnityEngine.Random.Range(0, matches.Count)] : SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
    }

    void ScalePlayerForWave(int wave)
    {
        var pc = GameManager.Instance.player.GetComponent<PlayerController>();
        if (pc == null || pc.hp == null || pc.spellcaster == null) return;
        string cls = ChooseClassManager.SelectedClass ?? "mage";
        Dictionary<string, float> stats = classManager != null
            ? classManager.GetClassStatsForWave(cls, wave)
            : new Dictionary<string, float>
            {
                {"health", RPNEvaluator.SafeEvaluateFloat("95 wave 5 * +", new Dictionary<string,float>{{"wave",wave}})},
                {"mana",   RPNEvaluator.SafeEvaluateFloat("90 wave 10 * +", new Dictionary<string,float>{{"wave",wave}})},
                {"mana_regeneration", RPNEvaluator.SafeEvaluateFloat("10 wave +", new Dictionary<string,float>{{"wave",wave}})},
                {"spellpower", RPNEvaluator.SafeEvaluateFloat("wave 10 *", new Dictionary<string,float>{{"wave",wave}})},
                {"speed", RPNEvaluator.SafeEvaluateFloat("5", new Dictionary<string,float>{{"wave",wave}})}
            };

        pc.hp.SetMaxHP(Mathf.RoundToInt(stats["health"]), true);
        pc.spellcaster.max_mana = Mathf.RoundToInt(stats["mana"]);
        pc.spellcaster.mana = pc.spellcaster.max_mana;
        pc.spellcaster.mana_reg = Mathf.RoundToInt(stats["mana_regeneration"]);
        pc.spellcaster.spellPower = Mathf.RoundToInt(stats["spellpower"]);
        pc.speed = Mathf.RoundToInt(stats["speed"]);

        pc.healthui?.SetHealth(pc.hp);
        pc.manaui?.SetSpellCaster(pc.spellcaster);
    }
}
