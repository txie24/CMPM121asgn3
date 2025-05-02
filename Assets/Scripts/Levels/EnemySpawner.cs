using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
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
            selector.transform.localPosition = new Vector3(0, 130 - 100 * GameManager.Instance.levelDefs.IndexOf(lvl));
            var controller = selector.GetComponent<MenuSelectorController>();
            controller.spawner = this;
            controller.SetLevel(lvl.name);
            selector.GetComponent<Button>().onClick.AddListener(controller.StartLevel);
        }
    }

    public void StartLevel(string levelname)
    {
        level_selector.gameObject.SetActive(false);
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();

        currentLevel = GameManager.Instance.levelDefs.Find(l => l.name == levelname);
        if (currentLevel == null)
        {
            Debug.LogError($"StartLevel failed: level '{levelname}' not found.");
            return;
        }
        currentWave = 1;
        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        if (!waveInProgress)
        {
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        if (waveInProgress) yield break;
        waveInProgress = true;

        if (currentLevel == null)
        {
            Debug.LogError("No current level set.");
            waveInProgress = false;
            yield break;
        }

        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;

        for (int i = 3; i > 0; i--)
        {
            GameManager.Instance.countdown = i;
            yield return new WaitForSeconds(1);
        }

        GameManager.Instance.countdown = 0;
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        int totalSpawned = 0;
        foreach (var spawn in currentLevel.spawns)
        {
            yield return StartCoroutine(SpawnEnemies(spawn, count => totalSpawned += count));
        }

        lastWaveEnemyCount = totalSpawned;

        // Wait for all enemies to die
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);

        // ✅ Check win condition after final wave completes
        if (!isEndless && currentWave >= currentLevel.waves)
        {
            TriggerWin();
            yield break;
        }

        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        GameManager.Instance.wavesCompleted++;
        currentWave++;
        waveInProgress = false;
    }

    IEnumerator SpawnEnemies(Spawn spawn, System.Action<int> onSpawnComplete = null)
    {
        var baseEnemy = GameManager.Instance.enemyDefs.Find(e => e.name == spawn.enemy);
        if (baseEnemy == null)
            yield break;

        var vars = new Dictionary<string, int> {
            { "base", baseEnemy.hp },
            { "wave", currentWave }
        };

        int total = RPNEvaluator.SafeEvaluate(spawn.count, vars, 0);
        int hp = spawn.hp != null ? RPNEvaluator.SafeEvaluate(spawn.hp, vars, baseEnemy.hp) : baseEnemy.hp;
        float speed = spawn.speed != null ? RPNEvaluator.SafeEvaluate(spawn.speed, new() { { "base", (int)baseEnemy.speed }, { "wave", currentWave } }, (int)baseEnemy.speed) : baseEnemy.speed;
        int damage = spawn.damage != null ? RPNEvaluator.SafeEvaluate(spawn.damage, new() { { "base", baseEnemy.damage }, { "wave", currentWave } }, baseEnemy.damage) : baseEnemy.damage;
        float delay = spawn.delay != null ? RPNEvaluator.SafeEvaluate(spawn.delay, vars, 2) : 2f;

        Debug.Log($"[Spawn] Spawning {total} '{spawn.enemy}' (HP={hp}, Speed={speed}, Damage={damage}) in Wave {currentWave}");

        if (speed < 1f) speed = 1f;
        if (speed > 20f) speed = 20f;

        List<int> seq = (spawn.sequence != null && spawn.sequence.Count > 0) ? spawn.sequence : new List<int> { 1 };
        int spawned = 0, seqIndex = 0;

        while (spawned < total)
        {
            int batch = seq[seqIndex % seq.Count];
            seqIndex++;

            for (int i = 0; i < batch && spawned < total; i++)
            {
                SpawnPoint point = PickSpawnPoint(spawn.location);
                Vector2 offset = GetNonOverlappingOffset(point.transform.position);
                Vector3 initial_position = point.transform.position + new Vector3(offset.x, offset.y, 0);

                GameObject new_enemy = Instantiate(enemy, initial_position, Quaternion.identity);
                new_enemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(baseEnemy.sprite);

                var en = new_enemy.GetComponent<EnemyController>();
                en.hp = new Hittable(hp, Hittable.Team.MONSTERS, new_enemy);
                en.speed = (int)speed;
                GameManager.Instance.AddEnemy(new_enemy);

                spawned++;
            }

            yield return new WaitForSeconds(delay);
        }

        onSpawnComplete?.Invoke(spawned);
    }

    private Vector2 GetNonOverlappingOffset(Vector3 spawnCenter)
    {
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 offset = Random.insideUnitCircle * 3.0f;
            Vector3 testPosition = spawnCenter + new Vector3(offset.x, offset.y, 0);
            Collider2D[] hit = Physics2D.OverlapCircleAll(testPosition, 0.75f);
            if (hit.Length == 0) return offset;
        }
        return Random.insideUnitCircle * 3.0f;
    }

    private SpawnPoint PickSpawnPoint(string location)
    {
        if (string.IsNullOrEmpty(location) || !location.StartsWith("random"))
            return SpawnPoints[Random.Range(0, SpawnPoints.Length)];

        if (location == "random")
            return SpawnPoints[Random.Range(0, SpawnPoints.Length)];

        string kind = location.Split(' ')[1].Trim().ToUpperInvariant();
        var matches = SpawnPoints.Where(sp => sp.kind.ToString().ToUpperInvariant() == kind).ToList();
        if (matches.Count > 0)
            return matches[Random.Range(0, matches.Count)];

        return SpawnPoints[Random.Range(0, SpawnPoints.Length)];
    }
}
