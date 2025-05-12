// File: Assets/Scripts/Spells/DamageMagnifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class DamageMagnifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "damage-amplified";

    public DamageMagnifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(),
                vars,
                damageMultiplier
            );
        if (j["mana_multiplier"] != null)
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["mana_multiplier"].Value<string>(),
                vars,
                manaMultiplier
            );
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 1) Find the true “leaf” spell
        Spell leaf = inner;
        while (leaf is ModifierSpell ms) leaf = ms.InnerSpell;

        // 2) Save original mods
        var original = leaf.mods;

        // 3) Merge original + our damage/mana mods
        var merged = new StatBlock();
        // copy all lists
        foreach (var m in original.damage) merged.damage.Add(m);
        foreach (var m in original.mana) merged.mana.Add(m);
        foreach (var m in original.speed) merged.speed.Add(m);
        foreach (var m in original.cd) merged.cd.Add(m);
        // add ours
        foreach (var m in this.mods.damage) merged.damage.Add(m);
        foreach (var m in this.mods.mana) merged.mana.Add(m);
        foreach (var m in this.mods.speed) merged.speed.Add(m);
        foreach (var m in this.mods.cd) merged.cd.Add(m);

        leaf.mods = merged;

        // 4) Run the full chain under combined mods
        yield return inner.TryCast(from, to);

        // 5) Restore
        leaf.mods = original;
    }
}
