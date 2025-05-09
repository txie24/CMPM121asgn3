// File: DamageMagnifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class DamageMagnifier : ModifierSpell
{
    private float damageMultiplier = 2f;
    private float manaMultiplier = 2f;
    private string modifierName = "damage-amplified";

    public DamageMagnifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(), vars, damageMultiplier);
        if (j["mana_multiplier"] != null)
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["mana_multiplier"].Value<string>(), vars, manaMultiplier);
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }
}
