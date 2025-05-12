// File: Assets/Scripts/Spells/ChaoticModifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ChaoticModifier : ModifierSpell
{
    private float damageMultiplier = 1f;
    private string modifierName = "chaotic";
    private string trajectoryOverride = "spiraling";

    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(), vars, damageMultiplier);
        trajectoryOverride = j["projectile_trajectory"]?.Value<string>() ?? trajectoryOverride;

        // rebuild this.mods
        mods = new StatBlock();
        InjectMods(mods);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        var pm = GameManager.Instance.projectileManager;
        string previous = pm.forcedTrajectory;
        pm.forcedTrajectory = string.IsNullOrEmpty(pm.forcedTrajectory)
            ? trajectoryOverride
            : pm.forcedTrajectory + "+" + trajectoryOverride;

        Spell leaf = inner;
        while (leaf is ModifierSpell ms) leaf = ms.InnerSpell;

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

        yield return inner.TryCast(from, to);

        leaf.mods = original;
        pm.forcedTrajectory = previous;
    }
}
