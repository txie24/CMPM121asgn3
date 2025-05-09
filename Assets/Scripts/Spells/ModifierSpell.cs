// File: ModifierSpell.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public abstract class ModifierSpell : Spell
{
    // The wrapped spell
    protected readonly Spell inner;
    // Expose it so we can walk the chain
    public Spell InnerSpell => inner;

    protected ModifierSpell(Spell inner) : base(inner.Owner)
    {
        this.inner = inner;
    }

    public override string DisplayName => $"{inner.DisplayName} {Suffix}";
    public override int IconIndex => inner.IconIndex;
    protected abstract string Suffix { get; }

    // Delegate base stats to inner, then our StatBlock mods will apply
    protected override float BaseDamage => inner.Damage;
    protected override float BaseMana => inner.Mana;
    protected override float BaseCooldown => inner.Cooldown;
    protected override float BaseSpeed => inner.Speed;

    // Rebuild our StatBlock whenever JSON is reloaded
    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        mods = new StatBlock();
        InjectMods(mods);
    }

    // The single Cast pipeline that handles ALL modifiers
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 1) Find the true leaf spell (the one that actually spawns the projectile)
        Spell leaf = inner;
        while (leaf is ModifierSpell ms)
            leaf = ms.InnerSpell;

        // 2) Aggregate *all* StatBlock mods from this → leaf
        var aggregated = new StatBlock();
        var stack = new Stack<ModifierSpell>();
        for (Spell s = this; s is ModifierSpell m; s = m.InnerSpell)
            stack.Push(m);
        while (stack.Count > 0)
            stack.Pop().InjectMods(aggregated);

        // 3) Swap that merged StatBlock into the leaf
        var originalLeafMods = leaf.mods;
        leaf.mods = aggregated;

        // 4) Run every wrapper’s PreCast (top→down)
        for (Spell s = this; s is ModifierSpell m; s = m.InnerSpell)
            yield return m.PreCast(from, to);

        // 5) Fire the whole chain—down to the leaf
        yield return inner.TryCast(from, to);

        // 6) Run every wrapper’s PostCast (down→up)
        var postStack = new Stack<ModifierSpell>();
        for (Spell s = this; s is ModifierSpell m; s = m.InnerSpell)
            postStack.Push(m);
        while (postStack.Count > 0)
            yield return postStack.Pop().PostCast(from, to);

        // 7) Restore the leaf’s original mods
        leaf.mods = originalLeafMods;
    }

    // Hooks for one‑off behavior; override if needed
    protected virtual IEnumerator PreCast(Vector3 from, Vector3 to) { yield break; }
    protected virtual IEnumerator PostCast(Vector3 from, Vector3 to) { yield break; }

    // Each modifier just adds its own stats here
    protected abstract void InjectMods(StatBlock mods);
}
