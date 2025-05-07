// File: Assets/Scripts/Spells/SpellBuilder.cs

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

    static readonly string[] BaseKeys = {
        "arcane_bolt",
        "arcane_spray",
        "magic_missile",
        "arcane_blast",
        "railgun"
    };

    static readonly string[] ModifierKeys = {
        "splitter",
        "doubler",
        "damage_amp",
        "speed_amp",
        "chaos",
        "homing"
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
            catalog = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(ta.text);
            Debug.Log($"SpellBuilder loaded {catalog.Count} spells.");
        }
    }

    public Spell Build(SpellCaster owner)
    {
        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
        int wave = spawner != null ? spawner.currentWave : 1;

        var vars = new Dictionary<string,float> {
            { "power", owner.spellPower },
            { "wave",  wave }
        };

        if (wave <= 1)
        //Starting spell
        {
            var bolt = new ArcaneBolt(owner);
            if (catalog.TryGetValue("arcane_bolt", out var jd))
                bolt.LoadAttributes(jd, vars);
            return bolt;
        }

        System.Random localRng = new System.Random(System.DateTime.Now.Millisecond + System.Environment.TickCount);

        int b = localRng.Next(BaseKeys.Length);
        Debug.Log($"Selected base spell index: {b}, spell type: {BaseKeys[b]}");

        Spell s = CreateRandomBaseSpell(owner, b);
        if (catalog.TryGetValue(BaseKeys[b], out var bd))
            s.LoadAttributes(bd, vars);
        else
            Debug.LogError($"Failed to find base spell: {BaseKeys[b]} in catalog");

        int mods = localRng.Next(3);
        Debug.Log($"Applying {mods} modifiers");

        for (int i = 0; i < mods; i++)
        {
            int m = localRng.Next(ModifierKeys.Length);
            Debug.Log($"Selected modifier index: {m}, modifier type: {ModifierKeys[m]}");

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
            default: return inner;
        }
    }
}