using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class RelicDataList
{
    public RelicData[] relics;
}

[System.Serializable]
public class RelicData
{
    public string name;
    public int sprite;
    public TriggerData trigger;
    public EffectData effect;
}

[System.Serializable]
public class TriggerData
{
    public string description;
    public string type;
    public string amount;
    public string until;
}

[System.Serializable]
public class EffectData
{
    public string description;
    public string type;
    public string amount;
    public string until;
}

public class RelicManager : MonoBehaviour
{
    public static RelicManager I { get; private set; }

    List<Relic> allRelics;
    List<Relic> owned = new List<Relic>();

    void Awake()
    {
        if (I == null)
        {
            I = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadRelics();
        EnemySpawner.OnWaveEnd += OnWaveEnd;
        Debug.Log("RelicManager initialized and subscribed to OnWaveEnd");
    }

    void LoadRelics()
    {
        var txt = Resources.Load<TextAsset>("relics");
        if (txt == null)
        {
            Debug.LogError("Could not find relics.json");
            return;
        }

        // relics.json is an array, so wrap it in an object
        var list = JsonUtility.FromJson<RelicDataList>("{\"relics\":" + txt.text + "}");
        allRelics = list.relics.Select(d => new Relic(d)).ToList();
        Debug.Log($"RelicManager loaded {allRelics.Count} relics");
    }

    void OnWaveEnd(int wave)
    {
        Debug.Log($"OnWaveEnd triggered for wave {wave}");

        if (wave % 3 != 0)
        {
            Debug.Log($"Wave {wave} is not a relic wave");
            return;
        }

        var available = allRelics.Where(r => !owned.Contains(r)).ToList();
        if (available.Count < 3)
        {
            Debug.Log("Not enough relics to choose from");
            return;
        }

        var choices = available.OrderBy(_ => Random.value).Take(3).ToArray();
        if (RewardScreenManager.Instance != null)
        {
            Debug.Log("Calling ShowRelics");
            RewardScreenManager.Instance.ShowRelics(choices);
        }
        else
        {
            Debug.LogError("RewardScreenManager.Instance is NULL!");
        }
    }

    public void PickRelic(Relic r)
    {
        if (owned.Contains(r)) return;
        owned.Add(r);
        r.Init();
        Debug.Log($"Picked relic: {r.Name}");
    }

    [ContextMenu("Force Show Relics")]
    public void ForceShowRelics()
    {
        if (allRelics != null && allRelics.Count > 0)
        {
            var choices = allRelics.Take(3).ToArray();
            Debug.Log($"Force showing {choices.Length} relics");
            RewardScreenManager.Instance?.ShowRelics(choices);
        }
    }
}
