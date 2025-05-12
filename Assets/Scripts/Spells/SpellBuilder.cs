using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

/// <summary>
/// Builds a random Spell by instantiating global‐namespace spell classes
/// and loading all numeric stats from spells.json via RPN.
/// Ensures wave 1 is always a plain Arcane Bolt.
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
        "splitter",
        "doubler",
        "damage_amp",
        "speed_amp",
        "chaos",
        "homing",
        "knockback",
        "bounce",
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

        // wave 1: always a plain ArcaneBolt
        if (wave <= 1)
        {
            Spell spray = new ArcaneSpray(owner);
            if (catalog.TryGetValue("arcane_spray", out var baseJson))
                spray.LoadAttributes(baseJson, vars);

            spray = new BounceModifier(spray);
            if (catalog.TryGetValue("bounce", out var bounceJson))
                spray.LoadAttributes(bounceJson, vars);


            Debug.Log("[SpellBuilder] Wave 1 forced spell: ArcaneSpray + KnockbackModifier");
            return spray;
        }

        // Ensure true randomness with a new seed based on current time
        System.Random localRng = new System.Random(
            System.DateTime.Now.Millisecond + System.Environment.TickCount);

        // pick random base spell
        int b = localRng.Next(BaseKeys.Length);
        Debug.Log($"Selected base spell index: {b}, spell type: {BaseKeys[b]}");

        Spell s = CreateRandomBaseSpell(owner, b);
        if (catalog.TryGetValue(BaseKeys[b], out var bd))
            s.LoadAttributes(bd, vars);
        else
            Debug.LogError($"Failed to find base spell: {BaseKeys[b]} in catalog");

        // wrap with 0–2 random modifiers
        int mods = localRng.Next(3); // 0, 1, or 2 modifiers
        Debug.Log($"Applying {mods} modifiers");

        // collect modifier indices
        List<int> modIndices = new List<int>();
        for (int i = 0; i < mods; i++)
        {
            int m = localRng.Next(ModifierKeys.Length);
            Debug.Log($"Selected modifier index: {m}, modifier type: {ModifierKeys[m]}");
            modIndices.Add(m);
        }

        // ensure 'doubler' (ModifierKeys[1]) is always the second modifier if present
        if (modIndices.Count > 1 && modIndices.Contains(1))
        {
            modIndices.Remove(1);
            modIndices.Insert(1, 1);
        }

        // apply modifiers in order
        foreach (int m in modIndices)
        {
            s = ApplyRandomModifier(s, m);
            if (catalog.TryGetValue(ModifierKeys[m], out var md))
                s.LoadAttributes(md, vars);
            else
                Debug.LogError($"Failed to find modifier: {ModifierKeys[m]} in catalog");
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
            case 7: return new BounceModifier(inner);
            default: return inner;
        }
    }
}
