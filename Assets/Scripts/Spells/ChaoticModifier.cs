// File: Assets/Scripts/Spells/ChaoticModifier.cs
using UnityEngine;
using System.Collections;                // for IEnumerator
using System.Collections.Generic;       // for Dictionary<,>
using Newtonsoft.Json.Linq;             // for JObject

public sealed class ChaoticModifier : ModifierSpell
{
    // these get overridden by JSON
    private float damageMultiplier = 1f;
    private string modifierName = "chaotic";
    private string trajectoryOverride = "spiraling";

    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        // 1) display name / suffix
        modifierName = j["name"]?.Value<string>() ?? modifierName;

        // 2) damage multiplier (RPN allowed)
        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(),
                vars,
                damageMultiplier
            );

        // 3) which trajectory to force (e.g. "spiraling")
        trajectoryOverride = j["projectile_trajectory"]?.Value<string>()
                             ?? trajectoryOverride;

        // rebuild this.mods via InjectMods
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // apply the damage multiplier so leaf.Damage uses it
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }

    // Swap in both the damage buff and the forced spiraling trajectory
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 1) stash & set trajectory override
        var pm = GameManager.Instance.projectileManager;
        var previous = pm.forcedTrajectory;
        pm.forcedTrajectory = trajectoryOverride;

        // 2) find the deepest wrapped spell (the “leaf” that actually does damage)
        Spell leaf = inner;
        while (leaf is ModifierSpell ms)
            leaf = ms.InnerSpell;

        // 3) swap in our mods on that leaf
        var originalLeafMods = leaf.mods;
        leaf.mods = this.mods;

        // 4) fire the entire chain (including ArcaneSpray’s own loop) under spiraling + our damage buff
        yield return inner.TryCast(from, to);

        // 5) restore everything
        leaf.mods = originalLeafMods;
        pm.forcedTrajectory = previous;
    }
}
