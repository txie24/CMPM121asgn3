// Assets/Scripts/Core/GameManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class GameManager
{
    public enum GameState { PREGAME, INWAVE, WAVEEND, COUNTDOWN, GAMEOVER }
    public GameState state;
    public int countdown;
    private static GameManager theInstance;
    public static GameManager Instance => theInstance ??= new GameManager();

    public GameObject player;
    public ProjectileManager projectileManager;
    public SpellIconManager spellIconManager;
    public EnemySpriteManager enemySpriteManager;
    public PlayerSpriteManager playerSpriteManager;
    public RelicIconManager relicIconManager;

    List<GameObject> enemies = new();
    public int enemy_count => enemies.Count;

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

        foreach (var e in new List<GameObject>(enemies))
            if (e != null) GameObject.Destroy(e);
        enemies.Clear();
    }

    public void AddEnemy(GameObject e) => enemies.Add(e);

    public void RemoveEnemy(GameObject e)
    {
        if (enemies.Remove(e))
        {
            totalEnemiesKilled++;
            // now legal because OnEnemyKilled is a delegate, not an event
            EnemySpawner.OnEnemyKilled?.Invoke(e);
        }
    }

    public GameObject GetClosestEnemy(Vector3 pt)
    {
        if (enemies.Count == 0) return null;
        if (enemies.Count == 1) return enemies[0];
        return enemies.Aggregate((a, b) =>
            (a.transform.position - pt).sqrMagnitude < (b.transform.position - pt).sqrMagnitude ? a : b);
    }

    private GameManager()
    {
        var eTxt = Resources.Load<TextAsset>("enemies");
        if (eTxt != null)
            enemyDefs = JsonConvert.DeserializeObject<List<Enemy>>(eTxt.text);
        var lTxt = Resources.Load<TextAsset>("levels");
        if (lTxt != null)
            levelDefs = JsonConvert.DeserializeObject<List<Level>>(lTxt.text);
    }
}
