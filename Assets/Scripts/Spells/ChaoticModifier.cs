// File: Assets/Scripts/Spells/ChaoticModifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ChaoticModifier : ModifierSpell
{
    // loaded from JSON
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
                j["damage_multiplier"].Value<string>(),
                vars,
                damageMultiplier);
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
        // 1) force spiraling trajectory
        var pm = GameManager.Instance.projectileManager;
        var prevTraj = pm.forcedTrajectory;
        pm.forcedTrajectory = trajectoryOverride;

        // 2) find the deepest “leaf” spell
        Spell leaf = inner;
        while (leaf is ModifierSpell ms) leaf = ms.InnerSpell;

        // 3) merge original + our chaotic damage mod
        var original = leaf.mods;
        var merged = new StatBlock();
        // copy all
        foreach (var m in original.damage) merged.damage.Add(m);
        foreach (var m in original.mana) merged.mana.Add(m);
        foreach (var m in original.speed) merged.speed.Add(m);
        foreach (var m in original.cd) merged.cd.Add(m);
        // then ours
        foreach (var m in this.mods.damage) merged.damage.Add(m);
        foreach (var m in this.mods.mana) merged.mana.Add(m);
        foreach (var m in this.mods.speed) merged.speed.Add(m);
        foreach (var m in this.mods.cd) merged.cd.Add(m);

        leaf.mods = merged;

        // 4) cast entire chain under spiraling + extra damage
        yield return inner.TryCast(from, to);

        // 5) restore
        leaf.mods = original;
        pm.forcedTrajectory = prevTraj;
    }
}
