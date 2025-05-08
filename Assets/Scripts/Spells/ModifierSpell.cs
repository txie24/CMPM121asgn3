using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public abstract class ModifierSpell : Spell
{
    protected readonly Spell inner;
    
    // Add property to access the inner spell
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

    // This is the key method each modifier will implement to customize behavior
    protected abstract void InjectMods(StatBlock mods);
    
    // Use the ModifyAndCast method instead of trying to override TryCast
    // This avoids the compiler error since we're not trying to override TryCast
    public new IEnumerator TryCast(Vector3 from, Vector3 to)
    {
        // Temporarily store original modifiers
        StatBlock originalMods = inner.mods;
        
        try
        {
            // Merge our mods with inner's existing mods
            StatBlock combinedMods = MergeStatBlocks(originalMods, this.mods);
            inner.mods = combinedMods;
            
            // Actually cast the spell with modifiers applied
            yield return CastWithModifiers(from, to);
        }
        finally
        {
            // Always restore original mods
            inner.mods = originalMods;
        }
    }
    
    // A new method that can be overridden by subclasses to modify casting behavior
    protected virtual IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        // Default implementation just delegates to inner spell's Cast method
        yield return Cast(from, to);
    }
    
    // The base Cast implementation just calls inner.Cast since we've already applied modifiers
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{GetType().Name}] Base Cast delegating to inner spell {inner.DisplayName}");
        yield return inner.TryCast(from, to);
    }
    
    // Helper method to merge StatBlocks
    private StatBlock MergeStatBlocks(StatBlock a, StatBlock b)
    {
        StatBlock result = new StatBlock();
        
        result.damage.AddRange(a.damage);
        result.damage.AddRange(b.damage);
        
        result.mana.AddRange(a.mana);
        result.mana.AddRange(b.mana);
        
        result.speed.AddRange(a.speed);
        result.speed.AddRange(b.speed);
        
        result.cd.AddRange(a.cd);
        result.cd.AddRange(b.cd);
        
        return result;
    }
}