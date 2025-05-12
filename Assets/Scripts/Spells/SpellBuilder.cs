using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

/// <summary>
/// Builds a random Spell by instantiating global‐namespace spell classes
/// and loading all numeric stats from spells.json via RPN.
/// Ensures wave 1 is always a plain Arcane Bolt.
/// </summary>
public class SpellBuilder
{
    private readonly Dictionary<string, JObject> catalog;
    private readonly System.Random rng = new System.Random();

    // JSON keys for base spells and modifiers
    static readonly string[] BaseKeys = {
        "arcane_bolt",
        "arcane_spray",
        "magic_missile",
        "arcane_blast",
        "railgun",
    };
    static readonly string[] ModifierKeys = {
        "splitter",    // index 0
        "doubler",     // index 1
        "damage_amp",  // index 2
        "speed_amp",   // index 3
        "chaos",       // index 4
        "homing",      // index 5
        "knockback",   // index 6
        "bounce",      // index 7
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
        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        int wave = spawner != null ? spawner.currentWave : 1;

        // RPN vars
        var vars = new Dictionary<string, float> {
            { "power", owner.spellPower },
            { "wave",  wave }
        };

        // wave 1: always a plain ArcaneBolt
        if (wave <= 1)
        {
            Spell bolt = new ArcaneBolt(owner);
            if (catalog.TryGetValue("arcane_bolt", out var baseJson))
                bolt.LoadAttributes(baseJson, vars);
            return bolt;
        }

        // new local RNG for unpredictability
        System.Random localRng = new System.Random(
            DateTime.Now.Millisecond + Environment.TickCount);

        // pick random base spell
        int b = localRng.Next(BaseKeys.Length);
        Debug.Log($"Selected base spell: {BaseKeys[b]} (index {b})");

        Spell s = CreateRandomBaseSpell(owner, b);
        if (catalog.TryGetValue(BaseKeys[b], out var bd))
            s.LoadAttributes(bd, vars);
        else
            Debug.LogError($"Missing base spell in catalog: {BaseKeys[b]}");

        double roll = localRng.NextDouble();
        int mods = (roll < 0.3) ? 2 : localRng.Next(2);

        Debug.Log($"Applying {mods} modifier(s)");

        List<int> modIndices = new List<int>();
        for (int i = 0; i < mods; i++)
        {
            int m = localRng.Next(ModifierKeys.Length);
            Debug.Log($" → picked {ModifierKeys[m]} (index {m})");
            modIndices.Add(m);
        }

        if (modIndices.Count > 1 && modIndices.Contains(1))
        {
            modIndices.Remove(1);
            modIndices.Insert(1, 1);
        }
        if (modIndices.Count > 1 && modIndices.Contains(0))
        {
            modIndices.Remove(0);
            modIndices.Insert(1, 0);
        }

        foreach (int mi in modIndices)
        {
            s = ApplyRandomModifier(s, mi);
            if (catalog.TryGetValue(ModifierKeys[mi], out var md))
                s.LoadAttributes(md, vars);
            else
                Debug.LogError($"Missing modifier in catalog: {ModifierKeys[mi]}");
        }

        Debug.Log($"Final spell: {s.DisplayName}");
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
            case 7: return new BounceModifier(inner);
            default: return inner;
        }
    }
}
