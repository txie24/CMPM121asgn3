using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Doubler : ModifierSpell
{
    private float delay = 0.5f;
    private float manaMultiplier = 1.5f;
    private float cooldownMultiplier = 1.5f;
    private string modifierName = "doubled";
    private string modifierDescription = "Spell is cast a second time after a small delay; increased mana cost and cooldown.";
    
    public Doubler(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "doubled";
        modifierDescription = j["description"]?.Value<string>() ?? "Spell is cast a second time after a small delay; increased mana cost and cooldown.";
        
        if (j["delay"] != null)
        {
            string expr = j["delay"].Value<string>();
            delay = float.Parse(expr);
        }
        
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        if (j["cooldown_multiplier"] != null)
        {
            string expr = j["cooldown_multiplier"].Value<string>();
            cooldownMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Increase mana cost and cooldown
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
        mods.cd.Add(new ValueMod(ModOp.Mul, cooldownMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // Store original mods
        StatBlock originalMods = inner.mods;
        
        // Create our mods
        StatBlock ourMods = new StatBlock();
        InjectMods(ourMods);
        
        // Apply our mods to inner spell
        inner.mods = MergeStatBlocks(originalMods, ourMods);
        
        Debug.Log($"[Doubler] First cast of {inner.DisplayName}");
        
        // First cast - allow inner spell to handle it properly
        // This is crucial for handling nested modifiers
        yield return inner.TryCast(from, to);
        
        // Small delay between casts
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[Doubler] Second cast of {inner.DisplayName} after {delay}s delay");
        
        // Second cast - the inner spell's mods are still applied
        // This is important for handling nested modifiers properly
        yield return inner.TryCast(from, to);
        
        // Restore original mods
        inner.mods = originalMods;
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