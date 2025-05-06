using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class HomingModifier : ModifierSpell
{
    private float damageMultiplier = 0.75f;
    private float manaAdder = 10f;
    private string projectileTrajectory = "homing";
    private string modifierName = "homing";
    private string modifierDescription = "Homing projectile, with decreased damage and increased mana cost.";
    
    public HomingModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "homing";
        modifierDescription = j["description"]?.Value<string>() ?? "Homing projectile, with decreased damage and increased mana cost.";
        
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        if (j["mana_adder"] != null)
        {
            string expr = j["mana_adder"].Value<string>();
            manaAdder = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        projectileTrajectory = j["projectile_trajectory"]?.Value<string>() ?? "homing";
    }

    protected override void InjectMods(StatBlock mods)
    {
        // 降低伤害
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        
        // 增加魔法消耗（加法）
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
        
        // 注意：目前无法直接修改轨迹类型，这需要在Cast中处理
    }
}