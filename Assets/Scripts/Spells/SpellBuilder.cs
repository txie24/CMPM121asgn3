using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;  // for Count and Enumerable extensions

/// <summary>
/// Builds a random Spell by instantiating global‐namespace spell classes
/// and loading all numeric stats from spells.json via RPN.
/// Ensures wave 1 is always a plain Arcane Bolt.
/// </summary>
public class SpellBuilder
{
    private readonly Dictionary<string, JObject> catalog;

    // JSON keys for base spells and modifiers
    static readonly string[] BaseKeys = {
        "arcane_bolt",
        "arcane_spray",
        "magic_missile",
        "arcane_blast",
        "railgun",
    };
    static readonly string[] ModifierKeys = {
        "splitter",
        "doubler",
        "damage_amp",
        "speed_amp",
        "chaos",
        "homing",
        "knockback",
    };

    public SpellBuilder()
    {
        var ta = Resources.Load<TextAsset>("spells");
        if (ta == null)
        {
            Debug.LogError("SpellBuilder: spells.json not found!");
            catalog = new Dictionary<string, JObject>();
        }
        else
        {
            catalog = JsonConvert
                .DeserializeObject<Dictionary<string, JObject>>(ta.text);
            Debug.Log($"SpellBuilder loaded {catalog.Count} spells.");
        }
    }

    public Spell Build(SpellCaster owner)
    {
        // grab wave from the spawner
        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
        int wave = spawner != null ? spawner.currentWave : 1;

        // RPN vars
        var vars = new Dictionary<string, float> {
            { "power", owner.spellPower },
            { "wave",  wave }
        };

        // wave 1: always a plain ArcaneBolt (or customized test spell)
        if (wave <= 1)
        {
            Spell blast = new ArcaneBlast(owner);
            if (catalog.TryGetValue("arcane_blast", out var baseJson))
                blast.LoadAttributes(baseJson, vars);

            blast = new Doubler(blast);
            if (catalog.TryGetValue("doubler", out var doublerJson))
                blast.LoadAttributes(doublerJson, vars);

            blast = new SpeedModifier(blast);
            if (catalog.TryGetValue("speed_amp", out var speedJson))
                blast.LoadAttributes(speedJson, vars);

            Debug.Log("[SpellBuilder] Wave 1 forced spell: ArcaneSpray + Doubler + SpeedModifier");
            return blast;
        }

        // pick random base spell
        var localRng = new System.Random(
            System.DateTime.Now.Millisecond + System.Environment.TickCount
        );
        int b = localRng.Next(BaseKeys.Length);
        Debug.Log($"Selected base spell index: {b}, spell type: {BaseKeys[b]}");

        Spell s = CreateRandomBaseSpell(owner, b);
        if (catalog.TryGetValue(BaseKeys[b], out var bd))
            s.LoadAttributes(bd, vars);
        else
            Debug.LogError($"Failed to find base spell: {BaseKeys[b]} in catalog");

        // wrap with 0–2 random modifiers
        int modCount = localRng.Next(3); // 0, 1, or 2
        Debug.Log($"Applying {modCount} modifiers");

        // 1) Pick your mods into a list
        var modIndices = new List<int>();
        for (int i = 0; i < modCount; i++)
        {
            int m = localRng.Next(ModifierKeys.Length);
            Debug.Log($"Selected modifier index: {m}, type: {ModifierKeys[m]}");
            modIndices.Add(m);
        }

        // 2) If any of them is Doubler (index 1), move it to the front
        const int doublerKeyIndex = 1; // ModifierKeys[1] == "doubler"
        int doublerCount = modIndices.Count(x => x == doublerKeyIndex);
        if (doublerCount > 0)
        {
            modIndices.RemoveAll(x => x == doublerKeyIndex);
            for (int i = 0; i < doublerCount; i++)
                modIndices.Insert(0, doublerKeyIndex);
        }

        // 3) Apply in that order
        foreach (int m in modIndices)
        {
            s = ApplyRandomModifier(s, m);
            if (catalog.TryGetValue(ModifierKeys[m], out var md))
                s.LoadAttributes(md, vars);
            else
                Debug.LogError($"Missing modifier in catalog: {ModifierKeys[m]}");
        }

        Debug.Log($"Final spell created: {s.DisplayName}");
        return s;
    }

    private Spell CreateRandomBaseSpell(SpellCaster owner, int i)
    {
        switch (i)
        {
            case 0: return new ArcaneBolt(owner);
            case 1: return new ArcaneSpray(owner);
            case 2: return new MagicMissile(owner);
            case 3: return new ArcaneBlast(owner);
            case 4: return new Railgun(owner);
            default: return new ArcaneBolt(owner);
        }
    }

    private Spell ApplyRandomModifier(Spell inner, int i)
    {
        switch (i)
        {
            case 0: return new Splitter(inner);
            case 1: return new Doubler(inner);
            case 2: return new DamageMagnifier(inner);
            case 3: return new SpeedModifier(inner);
            case 4: return new ChaoticModifier(inner);
            case 5: return new HomingModifier(inner);
            case 6: return new KnockbackModifier(inner);
            default: return inner;
        }
    }
}
