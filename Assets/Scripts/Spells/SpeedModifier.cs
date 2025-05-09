// File: SpeedModifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class SpeedModifier : ModifierSpell
{
    private float speedMultiplier = 1.75f;
    private string modifierName = "speed-amplified";

    public SpeedModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["speed_multiplier"] != null)
            speedMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["speed_multiplier"].Value<string>(), vars, speedMultiplier);
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.speed.Add(new ValueMod(ModOp.Mul, speedMultiplier));
    }
}
