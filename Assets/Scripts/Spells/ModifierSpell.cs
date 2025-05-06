using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public abstract class ModifierSpell : Spell
{
    protected readonly Spell inner;
    protected ModifierSpell(Spell inner) : base(inner.Owner)
    {
        this.inner = inner;
    }

    public override string DisplayName => $"{inner.DisplayName} {Suffix}";
    public override int    IconIndex   => inner.IconIndex;
    protected abstract string Suffix { get; }

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        // 基类实现，可以由子类扩展
        base.LoadAttributes(j, vars);
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 清空内部法术的修饰符，避免累积
        inner.mods = new StatBlock();
        
        // 注入新的修饰符
        InjectMods(inner.mods);
        
        Debug.Log($"[ModifierSpell] {DisplayName} 开始施放内部法术 {inner.DisplayName}");
        
        // 调用内部法术
        yield return inner.TryCast(from, to);
        
        Debug.Log($"[ModifierSpell] {DisplayName} 完成施放内部法术 {inner.DisplayName}");
        
        // 施法完成后清空修饰符，确保不会影响后续施法
        inner.mods = new StatBlock();
    }

    protected abstract void InjectMods(StatBlock mods);
}