using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        "arcane_explosion"
    };
    static readonly string[] ModifierKeys = {
        "splitter",
        "doubler",
        "damage_magnifier",
        "speed_modifier",
        "chaotic_modifier",
        "homing_modifier"
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
            catalog = JsonConvert.
                DeserializeObject<Dictionary<string, JObject>>(ta.text);
            Debug.Log($"SpellBuilder loaded {catalog.Count} spells.");
        }
    }

    public Spell Build(SpellCaster owner)
    {
        // grab wave from the spawner
        var spawner = Object.FindObjectOfType<EnemySpawner>();
        int wave = spawner != null ? spawner.currentWave : 1;

        // RPN vars
        var vars = new Dictionary<string,float> {
            { "power", owner.spellPower },
            { "wave",  wave }
        };

        // wave 1: always a plain ArcaneBolt
        if (wave <= 1)
        {
            var bolt = new ArcaneBolt(owner);
            if (catalog.TryGetValue("arcane_bolt", out var jd))
                bolt.LoadAttributes(jd, vars);
            return bolt;
        }

        // pick random base
        int b = rng.Next(BaseKeys.Length);
        Spell s = CreateRandomBaseSpell(owner, b);
        if (catalog.TryGetValue(BaseKeys[b], out var bd))
            s.LoadAttributes(bd, vars);

        // wrap with 0–2 random modifiers
        int mods = rng.Next(0, 3);
        for (int i = 0; i < mods; i++)
        {
            int m = rng.Next(ModifierKeys.Length);
            s = ApplyRandomModifier(s, m);
            if (catalog.TryGetValue(ModifierKeys[m], out var md))
                s.LoadAttributes(md, vars);
        }

        return s;
    }

    private Spell CreateRandomBaseSpell(SpellCaster owner, int i)
    {
        switch (i)
        {
            case 0: return new ArcaneBolt(owner);
            case 1: return new ArcaneSpray(owner);
            case 2: return new MagicMissile(owner);
            case 3: return new ArcaneExplosion(owner);
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
            default: return inner;
        }
    }
}