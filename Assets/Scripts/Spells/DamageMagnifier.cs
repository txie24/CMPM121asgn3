using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class DamageMagnifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "damage-amplified";
    private string modifierDescription = "Increased damage and increased mana cost.";
    
    public DamageMagnifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "damage-amplified";
        modifierDescription = j["description"]?.Value<string>() ?? "Increased damage and increased mana cost.";
        
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // 使用从JSON加载的值
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }
}