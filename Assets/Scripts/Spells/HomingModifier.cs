// File: Assets/Scripts/Spells/HomingModifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class HomingModifier : ModifierSpell
{
    private float damageMultiplier = 1f;
    private float manaAdder = 0f;
    private string modifierName = "homing";
    private string trajectoryOverride = "homing";

    public HomingModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(), vars, damageMultiplier);
        if (j["mana_adder"] != null)
            manaAdder = RPNEvaluator.SafeEvaluateFloat(
                j["mana_adder"].Value<string>(), vars, manaAdder);
        trajectoryOverride = j["projectile_trajectory"]?.Value<string>() ?? trajectoryOverride;

        // rebuild this.mods
        mods = new StatBlock();
        InjectMods(mods);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        var pm = GameManager.Instance.projectileManager;
        // 1) stash previous
        string previous = pm.forcedTrajectory;

        // 2) append our override
        pm.forcedTrajectory = string.IsNullOrEmpty(pm.forcedTrajectory)
            ? trajectoryOverride
            : pm.forcedTrajectory + "+" + trajectoryOverride;

        // 3) find the leaf spell
        Spell leaf = inner;
        while (leaf is ModifierSpell ms) leaf = ms.InnerSpell;

        // 4) merge original leaf.mods + this.mods
        var original = leaf.mods;
        var merged = new StatBlock();
        foreach (var m in original.damage) merged.damage.Add(m);
        foreach (var m in original.mana) merged.mana.Add(m);
        foreach (var m in original.speed) merged.speed.Add(m);
        foreach (var m in original.cd) merged.cd.Add(m);
        foreach (var m in this.mods.damage) merged.damage.Add(m);
        foreach (var m in this.mods.mana) merged.mana.Add(m);
        foreach (var m in this.mods.speed) merged.speed.Add(m);
        foreach (var m in this.mods.cd) merged.cd.Add(m);

        leaf.mods = merged;

        // 5) cast the full chain
        yield return inner.TryCast(from, to);

        // 6) restore
        leaf.mods = original;
        pm.forcedTrajectory = previous;
    }
}
