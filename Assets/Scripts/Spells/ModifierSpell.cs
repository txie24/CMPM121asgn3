using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public abstract class ModifierSpell : Spell
{
    // The spell we’re wrapping
    protected readonly Spell inner;

    // Expose it publicly so subclasses can walk the chain
    public Spell InnerSpell => inner;

    protected ModifierSpell(Spell inner) : base(inner.Owner)
    {
        this.inner = inner;
        // Initialize modifiers on construction
        mods = new StatBlock();
        InjectMods(mods);
        Debug.Log($"[ModifierSpell] Created {this.GetType().Name} wrapping {inner.DisplayName}");
    }

    public override string DisplayName => $"{inner.DisplayName} {Suffix}";
    public override int IconIndex => inner.IconIndex;
    protected abstract string Suffix { get; }

    // Delegate all base stats to our inner, then let our StatBlock mods apply
    protected override float BaseDamage => inner.Damage;
    protected override float BaseMana => inner.Mana;
    protected override float BaseCooldown => inner.Cooldown;
    protected override float BaseSpeed => inner.Speed;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        // If you need the JSON for this modifier itself, parse it here
        // Then reapply your InjectMods to rebuild mods from scratch
        mods = new StatBlock();
        InjectMods(mods);

        Debug.Log($"[ModifierSpell] {GetType().Name} final values - " +
                  $"Damage: {Damage:F2}, Mana: {Mana:F2}, " +
                  $"Cooldown: {Cooldown:F2}, Speed: {Speed:F2}");
    }

    // By default, modifiers just pass through
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        yield return ApplyModifierEffect(from, to);
    }

    // Override this in modifiers that need special behavior
    protected virtual IEnumerator ApplyModifierEffect(Vector3 from, Vector3 to)
    {
        yield return inner.TryCast(from, to);
    }

    // Each modifier injects its own ValueMods here
    protected abstract void InjectMods(StatBlock mods);
}
