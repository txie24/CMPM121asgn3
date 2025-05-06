using UnityEngine;
using System.Collections;

public sealed class Doubler : ModifierSpell
{
    public Doubler(Spell inner) : base(inner) { }

    protected override string Suffix => "Doubler";

    protected override void InjectMods(StatBlock mods)
    {
        // 加倍器不修改属性，只改变行为
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 先施放一次法术
        yield return inner.TryCast(from, to);
        
        // 等待短暂时间
        yield return new WaitForSeconds(0.25f);
        
        // 再施放一次法术
        yield return inner.TryCast(from, to);
    }
}