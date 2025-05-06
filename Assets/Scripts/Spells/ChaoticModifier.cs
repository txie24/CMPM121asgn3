using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ChaoticModifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private string projectileTrajectory = "spiraling";
    private string modifierName = "chaotic";
    private string modifierDescription = "Significantly increased damage, but projectile is spiraling.";
    
    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "chaotic";
        modifierDescription = j["description"]?.Value<string>() ?? "Significantly increased damage, but projectile is spiraling.";
        
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        projectileTrajectory = j["projectile_trajectory"]?.Value<string>() ?? "spiraling";
    }

    protected override void InjectMods(StatBlock mods)
    {
        // 增加伤害
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        
        // 注意：目前无法直接修改轨迹类型，这需要在Cast中处理
    }
}