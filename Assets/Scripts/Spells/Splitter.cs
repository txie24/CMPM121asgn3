using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Splitter : ModifierSpell
{
    private float angle = 10f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "split";
    private string modifierDescription = "Spell is cast twice in slightly different directions; increased mana cost.";
    
    public Splitter(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "split";
        modifierDescription = j["description"]?.Value<string>() ?? "Spell is cast twice in slightly different directions; increased mana cost.";
        
        if (j["angle"] != null)
        {
            string expr = j["angle"].Value<string>();
            angle = float.Parse(expr);
        }
        
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // 增加魔法消耗
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 清空内部法术的修饰符，避免累积
        inner.mods = new StatBlock();
        
        // 注入修饰符
        InjectMods(inner.mods);
        
        // 计算两个略微不同的方向
        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 使用从JSON加载的角度偏移
        Vector3 dir1 = new Vector3(
            Mathf.Cos((baseAngle + angle) * Mathf.Deg2Rad),
            Mathf.Sin((baseAngle + angle) * Mathf.Deg2Rad),
            0
        );
        Vector3 dir2 = new Vector3(
                Mathf.Cos((baseAngle - angle) * Mathf.Deg2Rad),
                Mathf.Sin((baseAngle - angle) * Mathf.Deg2Rad),
                0
            );
            
        // 向两个方向施放法术
        Vector3 target1 = from + dir1 * 10f;
        Vector3 target2 = from + dir2 * 10f;
        
        yield return inner.TryCast(from, target1);
        yield return inner.TryCast(from, target2);
        
        // 施法完成后清空修饰符
        inner.mods = new StatBlock();
    }
}
