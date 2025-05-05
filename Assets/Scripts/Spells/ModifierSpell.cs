using UnityEngine;
using System.Collections;

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

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        InjectMods(inner.mods);
        yield return inner.TryCast(from, to);
    }

    protected abstract void InjectMods(StatBlock mods);
}
