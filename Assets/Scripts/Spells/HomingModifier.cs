// File: HomingModifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class HomingModifier : ModifierSpell
{
    private float damageMultiplier = 0.75f;
    private float manaAdder = 10f;
    private string modifierName = "homing";

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
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
    }

    // Override via PreCast so the doubling logic—or any other modifier—still runs
    protected override IEnumerator PreCast(Vector3 from, Vector3 to)
    {
        // e.g. flag the projectile manager to use homing for this shot
        ProjectileManager.Instance.OverrideTrajectory("homing");
        yield break;
    }
}
