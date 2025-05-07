using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class SpeedModifier : ModifierSpell
{
    private float speedMultiplier = 1.75f;
    private string modifierName = "speed-amplified";
    private string modifierDescription = "Faster projectile speed";
    
    public SpeedModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "speed-amplified";
        modifierDescription = j["description"]?.Value<string>() ?? "Faster projectile speed";
        
        if (j["speed_multiplier"] != null)
        {
            string expr = j["speed_multiplier"].Value<string>();
            speedMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // 增加速度
        mods.speed.Add(new ValueMod(ModOp.Mul, speedMultiplier));
    }
}