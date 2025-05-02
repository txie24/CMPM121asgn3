using System.Collections.Generic;
using UnityEngine;

// wrapper for JsonUtility
[System.Serializable]
public class EnemyList { public List<Enemy> enemies; }
[System.Serializable]
public class LevelList { public List<Level> levels; }

[System.Serializable]
public class Enemy {
    public string name;
    public int sprite;
    public int hp;
    public float speed;
    public int damage;
}

[System.Serializable]
public class Spawn {
    public string enemy;        // matches Enemy.name
    public string count;        // RPN
    public List<int> sequence;  // optional
    public string delay;        // RPN or number
    public string location;     // e.g. "random red"
    public string hp;           // RPN or "base"
    public string speed;        // RPN or "base"
    public string damage;       // RPN or "base"
}

[System.Serializable]
public class Level {
    public string name;
    public int waves;           // optional—if missing, treat as endless
    public List<Spawn> spawns;
}
