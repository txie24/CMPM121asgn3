// File: Assets/Scripts/Spells/ChaoticModifier.cs
using UnityEngine;
using System.Collections;                // for IEnumerator
using System.Collections.Generic;       // for Dictionary<,>
using Newtonsoft.Json.Linq;             // for JObject

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
                j["damage_multiplier"].Value<string>(),
                vars,
                damageMultiplier
            );
        trajectoryOverride = j["projectile_trajectory"]?.Value<string>()
                             ?? trajectoryOverride;
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        var pm = GameManager.Instance.projectileManager;
        var previous = pm.forcedTrajectory;

        // special: if inner chain contains a HomingModifier, skip our override
        bool hasHoming = false;
        Spell walker = inner;
        while (walker is ModifierSpell ms)
        {
            if (ms is HomingModifier) { hasHoming = true; break; }
            walker = ms.InnerSpell;
        }
        if (!hasHoming)
            pm.forcedTrajectory = trajectoryOverride;

        // swap in our damage buff onto the leaf
        Spell leaf = inner;
        while (leaf is ModifierSpell ms) leaf = ms.InnerSpell;
        var savedMods = leaf.mods;
        leaf.mods = this.mods;

        // cast the full chain
        yield return inner.TryCast(from, to);

        // restore
        leaf.mods = savedMods;
        pm.forcedTrajectory = previous;
    }
}
