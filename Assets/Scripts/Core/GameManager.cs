using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class GameManager
{
    public enum GameState
    {
        PREGAME,
        INWAVE,
        WAVEEND,
        COUNTDOWN,
        GAMEOVER
    }

    public GameState state;

    public int countdown;
    private static GameManager theInstance;
    public static GameManager Instance
    {
        get
        {
            if (theInstance == null)
                theInstance = new GameManager();
            return theInstance;
        }
    }

    public GameObject player;

    public ProjectileManager projectileManager;
    public SpellIconManager spellIconManager;
    public EnemySpriteManager enemySpriteManager;
    public PlayerSpriteManager playerSpriteManager;
    public RelicIconManager relicIconManager;

    private List<GameObject> enemies;
    public int enemy_count { get { return enemies.Count; } }

    public List<Enemy> enemyDefs { get; private set; }
    public List<Level> levelDefs { get; private set; }

    public bool playerWon { get; set; }
    public bool IsPlayerDead = false;

    public int totalEnemiesKilled = 0;
    public float timeSurvived = 0f;
    public int totalDamageDealt = 0;
    public int totalDamageTaken = 0;

    public int wavesCompleted = 0; 

    public void ResetGame()
    {
        state = GameState.PREGAME;
        countdown = 0;
        IsPlayerDead = false;
        playerWon = false;
        totalEnemiesKilled = 0;
        totalDamageDealt = 0;
        totalDamageTaken = 0;
        timeSurvived = 0f;
        wavesCompleted = 0;

        if (player != null)
        {
            GameObject.Destroy(player);
            player = null;
        }

        if (enemy_count > 0)
        {
            foreach (var e in new List<GameObject>(enemies))
            {
                if (e != null)
                    GameObject.Destroy(e);
            }
            enemies.Clear();
        }
    }

    public void AddEnemy(GameObject enemy)
    {
        enemies.Add(enemy);
    }

    public void RemoveEnemy(GameObject enemy)
    {
        enemies.Remove(enemy);
        totalEnemiesKilled++;
        EnemySpawner.OnEnemyKilled?.Invoke(enemy);

    }

    public GameObject GetClosestEnemy(Vector3 point)
    {
        if (enemies == null || enemies.Count == 0) return null;
        if (enemies.Count == 1) return enemies[0];
        return enemies.Aggregate((a, b) =>
            (a.transform.position - point).sqrMagnitude
            < (b.transform.position - point).sqrMagnitude ? a : b);
    }

    private GameManager()
    {
        enemies = new List<GameObject>();

        var eTxt = Resources.Load<TextAsset>("enemies");
        if (eTxt == null)
            Debug.LogError("GameManager: could not find Resources/enemies.json");
        else
            enemyDefs = JsonConvert.DeserializeObject<List<Enemy>>(eTxt.text);

        var lTxt = Resources.Load<TextAsset>("levels");
        if (lTxt == null)
            Debug.LogError("GameManager: could not find Resources/levels.json");
        else
            levelDefs = JsonConvert.DeserializeObject<List<Level>>(lTxt.text);

        Debug.Log($"GameManager: Loaded {enemyDefs?.Count ?? 0} enemy types and {levelDefs?.Count ?? 0} levels.");
    }
}
