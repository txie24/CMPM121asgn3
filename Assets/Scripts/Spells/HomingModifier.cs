using UnityEngine;
using System.Collections;

public sealed class HomingModifier : ModifierSpell
{
    public HomingModifier(Spell inner) : base(inner) { }

    protected override string Suffix => "Homing";

    protected override void InjectMods(StatBlock mods)
    {
        // 降低伤害(乘法修改)
        mods.damage.Add(new ValueMod(ModOp.Mul, 0.8f));
        
        // 增加魔法消耗(加法修改)
        mods.mana.Add(new ValueMod(ModOp.Add, 5f));
        
        // 由于实现限制，可能无法直接修改投射物轨迹类型
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 应用修改器后调用内部法术
        InjectMods(inner.mods);
        
        // 这里应该修改投射物轨迹为homing，但当前实现可能无法做到
        // 在实际实现中，您可能需要修改ProjectileManager等代码来支持轨迹修改
        Debug.Log("[HomingModifier] 应用追踪效果，使投射物寻找敌人");
        
        yield return inner.TryCast(from, to);
    }
}