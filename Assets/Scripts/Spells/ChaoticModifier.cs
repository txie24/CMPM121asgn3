using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ChaoticModifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private string modifierName = "chaotic";
    
    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[ChaoticModifier] Loading attributes from JSON");
        
        // Load name
        modifierName = j["name"]?.Value<string>() ?? "chaotic";
        
        // Load damage multiplier using RPN
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
            Debug.Log($"[ChaoticModifier] Loaded damage_multiplier={damageMultiplier} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[ChaoticModifier] Injecting mods: damage×{damageMultiplier}");
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }
    
    // Override CastWithModifiers instead of Cast
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        // Get reference to ProjectileManager
        var pm = GameManager.Instance.projectileManager;
        
        // Store original values
        string originalTrajectoryOverride = pm.trajectoryOverride;
        
        try
        {
            // Set our trajectory override to spiraling
            pm.trajectoryOverride = "spiraling";
            
            Debug.Log($"[ChaoticModifier] Enhancing {inner.DisplayName} with chaotic spiraling trajectories (damage={Damage:F1})");
            
            // Call inner spell's cast with our overrides in effect
            yield return base.Cast(from, to);
        }
        finally
        {
            // Always restore original values to avoid side effects
            pm.trajectoryOverride = originalTrajectoryOverride;
        }
    }
}