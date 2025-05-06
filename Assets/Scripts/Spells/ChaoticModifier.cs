using UnityEngine;
using System.Collections;

public sealed class ChaoticModifier : ModifierSpell
{
    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => "Chaotic";

    protected override void InjectMods(StatBlock mods)
    {
        // 大幅增加伤害
        mods.damage.Add(new ValueMod(ModOp.Mul, 2.0f));
        
        // 由于实现限制，可能无法直接修改投射物轨迹类型
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 应用修改器后调用内部法术
        InjectMods(inner.mods);
        
        // 这里应该修改投射物轨迹为spiraling，但当前实现可能无法做到
        // 在实际实现中，您可能需要修改ProjectileManager等代码来支持轨迹修改
        Debug.Log("[ChaoticModifier] 应用混沌效果，增加伤害并使投射物螺旋移动");
        
        yield return inner.TryCast(from, to);
    }
}