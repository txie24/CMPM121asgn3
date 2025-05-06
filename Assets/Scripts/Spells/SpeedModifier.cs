using UnityEngine;
using System.Collections;

public sealed class SpeedModifier : ModifierSpell
{
    public SpeedModifier(Spell inner) : base(inner) { }

    protected override string Suffix => "Speed Booster";

    protected override void InjectMods(StatBlock mods)
    {
        // 增加速度(乘法修改)
        mods.speed.Add(new ValueMod(ModOp.Mul, 1.5f));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 应用修改器后调用内部法术
        InjectMods(inner.mods);
        yield return inner.TryCast(from, to);
    }
}