using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class RelicDataList { public RelicData[] relics; }

[System.Serializable]
public class RelicData
{
    public string name;
    public int sprite;
    public TriggerData trigger;
    public EffectData effect;
}

[System.Serializable]
public class TriggerData { public string type, amount, until; }
[System.Serializable]
public class EffectData { public string type, amount, until; }

public class RelicManager : MonoBehaviour
{
    public static RelicManager I { get; private set; }

    List<Relic> allRelics;
    List<Relic> owned = new List<Relic>();

    void Awake()
    {
        if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        LoadRelics();
        EnemySpawner.OnWaveEnd += OnWaveEnd;
    }

    void LoadRelics()
    {
        var txt = Resources.Load<TextAsset>("relics");
        if (txt == null)
        {
            Debug.LogError("Could not find relics.json");
            return;
        }
        var list = JsonUtility.FromJson<RelicDataList>(txt.text);
        allRelics = list.relics.Select(d => new Relic(d)).ToList();
    }

    void OnWaveEnd(int wave)
    {
        if (wave % 3 != 0) return;

        var choices = allRelics
            .Except(owned)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(3)
            .ToArray();

        if (choices.Length > 0)
            RewardScreenManager.Instance.ShowRelics(choices);
    }

    public void PickRelic(Relic r)
    {
        if (owned.Contains(r)) return;
        owned.Add(r);
        r.Init();
    }
}
