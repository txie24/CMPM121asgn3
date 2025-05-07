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
        // Base implementation, can be extended by subclasses
        base.LoadAttributes(j, vars);
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // Create a fresh StatBlock for this specific modifier
        StatBlock ourMods = new StatBlock();
        
        // Apply this modifier's specific changes
        InjectMods(ourMods);
        
        // Store the original mods to restore later
        StatBlock originalMods = inner.mods;
        
        // Combine existing mods with our mods - important for nested modifiers
        StatBlock combinedMods = MergeStatBlocks(originalMods, ourMods);
        
        // Apply the combined modifications
        inner.mods = combinedMods;
        
        Debug.Log($"[ModifierSpell] {DisplayName} casting inner spell {inner.DisplayName}");
        
        // Let the modifier-specific implementation handle the actual casting
        yield return ModifierCast(from, to);
        
        Debug.Log($"[ModifierSpell] {DisplayName} finished casting inner spell {inner.DisplayName}");
        
        // Restore the original modifiers
        inner.mods = originalMods;
    }
    
    // Default implementation of ModifierCast - can be overridden by specific modifiers
    protected virtual IEnumerator ModifierCast(Vector3 from, Vector3 to)
    {
        // Default behavior is to just call the inner spell's TryCast
        yield return inner.TryCast(from, to);
    }

    // Helper method to merge StatBlocks
    protected StatBlock MergeStatBlocks(StatBlock a, StatBlock b)
    {
        StatBlock result = new StatBlock();
        
        // Copy all modifiers from both blocks
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

    protected abstract void InjectMods(StatBlock mods);
}