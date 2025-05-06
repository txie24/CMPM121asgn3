using UnityEngine;
using System.Collections;

public sealed class Splitter : ModifierSpell
{
    public Splitter(Spell inner) : base(inner) { }

    protected override string Suffix => "Splitter";

    protected override void InjectMods(StatBlock mods)
    {
        // 分裂器不修改属性，只改变行为
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 计算两个略微不同的方向
        Vector3 direction = (to - from).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 随机角度偏移
        float offset = Random.Range(10f, 20f);
        
        // 计算两个新方向
        Vector3 dir1 = new Vector3(
            Mathf.Cos((angle + offset) * Mathf.Deg2Rad),
            Mathf.Sin((angle + offset) * Mathf.Deg2Rad),
            0
        );
            
        Vector3 dir2 = new Vector3(
            Mathf.Cos((angle - offset) * Mathf.Deg2Rad),
            Mathf.Sin((angle - offset) * Mathf.Deg2Rad),
            0
        );
            
        // 向两个方向施放法术
        Vector3 target1 = from + dir1 * 10f;
        Vector3 target2 = from + dir2 * 10f;
        
        yield return inner.TryCast(from, target1);
        yield return inner.TryCast(from, target2);
    }
}