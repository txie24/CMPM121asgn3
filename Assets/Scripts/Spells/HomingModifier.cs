// File: Assets/Scripts/Spells/HomingModifier.cs
using UnityEngine;
using System.Collections;                // for IEnumerator
using System.Collections.Generic;       // for Dictionary<,>
using Newtonsoft.Json.Linq;             // for JObject

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
                j["damage_multiplier"].Value<string>(),
                vars,
                damageMultiplier
            );
        if (j["mana_adder"] != null)
            manaAdder = RPNEvaluator.SafeEvaluateFloat(
                j["mana_adder"].Value<string>(),
                vars,
                manaAdder
            );
        trajectoryOverride = j["projectile_trajectory"]?.Value<string>()
                             ?? trajectoryOverride;
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        var pm = GameManager.Instance.projectileManager;
        var previous = pm.forcedTrajectory;

        // special: if inner chain contains a ChaoticModifier, skip our override
        bool hasChaos = false;
        Spell walker = inner;
        while (walker is ModifierSpell ms)
        {
            if (ms is ChaoticModifier) { hasChaos = true; break; }
            walker = ms.InnerSpell;
        }
        if (!hasChaos)
            pm.forcedTrajectory = trajectoryOverride;

        // swap in our damage+mana mods onto the leaf spell
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
