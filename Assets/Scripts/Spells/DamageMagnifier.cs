using UnityEngine;
using System.Collections;

public sealed class DamageMagnifier : ModifierSpell
{
    public DamageMagnifier(Spell inner) : base(inner) { }

    protected override string Suffix => "Damage Magnifier";

    protected override void InjectMods(StatBlock mods)
    {
        // 增加伤害(乘法修改)
        mods.damage.Add(new ValueMod(ModOp.Mul, 1.5f));
        
        // 增加魔法消耗(乘法修改)
        mods.mana.Add(new ValueMod(ModOp.Mul, 1.25f));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 应用修改器后调用内部法术
        InjectMods(inner.mods);
        yield return inner.TryCast(from, to);
    }
}