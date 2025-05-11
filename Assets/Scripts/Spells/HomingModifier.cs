// File: Assets/Scripts/Spells/HomingModifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;   // for Dictionary<,>
using Newtonsoft.Json.Linq;         // for JObject

public sealed class HomingModifier : ModifierSpell
{
    // these fields are loaded from JSON
    private float damageMultiplier = 1f;
    private float manaAdder = 0f;
    private string modifierName = "homing";
    private string trajectoryOverride = "homing";

    public HomingModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        // 1) Name / Display suffix
        modifierName = j["name"]?.Value<string>() ?? modifierName;

        // 2) Damage multiplier
        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(),
                vars,
                damageMultiplier
            );

        // 3) Mana adder
        if (j["mana_adder"] != null)
            manaAdder = RPNEvaluator.SafeEvaluateFloat(
                j["mana_adder"].Value<string>(),
                vars,
                manaAdder
            );

        // 4) Which trajectory to force (must match your JSON key)
        trajectoryOverride = j["projectile_trajectory"]?.Value<string>()
                             ?? trajectoryOverride;

        // rebuild this.mods with InjectMods
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Multiply damage, then add to mana cost
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 1) Hijack trajectory
        var pm = GameManager.Instance.projectileManager;
        var previous = pm.forcedTrajectory;
        pm.forcedTrajectory = trajectoryOverride;

        // 2) Drill down to the “leaf” spell (e.g. ArcaneSpray)
        Spell leaf = inner;
        while (leaf is ModifierSpell ms)
            leaf = ms.InnerSpell;

        // 3) Swap in our mods so leaf.Damage and leaf.Mana use them
        var originalLeafMods = leaf.mods;
        leaf.mods = this.mods;

        // 4) Cast the entire chain—including ArcaneSpray’s own loop—under homing
        yield return inner.TryCast(from, to);

        // 5) Restore
        leaf.mods = originalLeafMods;
        pm.forcedTrajectory = previous;
    }
}
