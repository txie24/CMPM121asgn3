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
        // Initialize modifiers on construction
        mods = new StatBlock();
        InjectMods(mods);
        Debug.Log($"[ModifierSpell] Created {this.GetType().Name} wrapping {inner.DisplayName}");
    }

    public override string DisplayName => $"{inner.DisplayName} {Suffix}";
    public override int IconIndex => inner.IconIndex;
    protected abstract string Suffix { get; }

    // Override to return the inner spell's calculated values
    protected override float BaseDamage => inner.Damage;
    protected override float BaseMana => inner.Mana;
    protected override float BaseCooldown => inner.Cooldown;
    protected override float BaseSpeed => inner.Speed;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        // After loading attributes, reapply modifiers
        mods = new StatBlock();
        InjectMods(mods);

        // Log calculated values for debugging
        Debug.Log($"[ModifierSpell] {GetType().Name} final values - Damage: {Damage:F2}, Mana: {Mana:F2}, Cooldown: {Cooldown:F2}, Speed: {Speed:F2}");
    }

    // Default Cast implementation that calls the inner spell's Cast with modified properties
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // By default, apply modifier's behavior first, then call inner spell
        yield return ApplyModifierEffect(from, to);
    }

    // New method to apply modifier-specific effects
    protected virtual IEnumerator ApplyModifierEffect(Vector3 from, Vector3 to)
    {
        // Default implementation just passes through to inner spell
        yield return inner.TryCast(from, to);
    }

    protected abstract void InjectMods(StatBlock mods);
}