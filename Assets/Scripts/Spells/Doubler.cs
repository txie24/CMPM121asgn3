using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Doubler : ModifierSpell
{
    private float delay = 0.5f;
    private float manaMultiplier = 1.5f;
    private float cooldownMultiplier = 1.5f;
    private string modifierName = "doubled";
    private string modifierDescription = "Spell is cast a second time after a small delay; increased mana cost and cooldown.";
    
    public Doubler(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "doubled";
        modifierDescription = j["description"]?.Value<string>() ?? "Spell is cast a second time after a small delay; increased mana cost and cooldown.";
        
        if (j["delay"] != null)
        {
            string expr = j["delay"].Value<string>();
            delay = float.Parse(expr);
        }
        
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        if (j["cooldown_multiplier"] != null)
        {
            string expr = j["cooldown_multiplier"].Value<string>();
            cooldownMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // 增加魔法消耗和冷却时间
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
        mods.cd.Add(new ValueMod(ModOp.Mul, cooldownMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 清空内部法术的修饰符，避免累积
        inner.mods = new StatBlock();
        
        // 注入修饰符
        InjectMods(inner.mods);
        
        // 先施放一次法术
        yield return inner.TryCast(from, to);
        
        // 等待指定时间
        yield return new WaitForSeconds(delay);
        
        // 再施放一次法术
        yield return inner.TryCast(from, to);
        
        // 施法完成后清空修饰符
        inner.mods = new StatBlock();
    }
}