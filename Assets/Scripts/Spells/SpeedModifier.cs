using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class SpeedModifier : ModifierSpell
{
    private float speedMultiplier = 1.75f;
    private string modifierName = "speed-amplified";
    
    public SpeedModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[SpeedModifier] Loading attributes from JSON");
        
        // Load name
        modifierName = j["name"]?.Value<string>() ?? "speed-amplified";
        
        // Load speed multiplier using RPN
        if (j["speed_multiplier"] != null)
        {
            string expr = j["speed_multiplier"].Value<string>();
            speedMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.75f);
            Debug.Log($"[SpeedModifier] Loaded speed_multiplier={speedMultiplier} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[SpeedModifier] Injecting mods: speed×{speedMultiplier}");
        mods.speed.Add(new ValueMod(ModOp.Mul, speedMultiplier));
    }
    
    // Override CastWithModifiers instead of Cast
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        // Get reference to ProjectileManager
        var pm = GameManager.Instance.projectileManager;
        
        // Store original speed multiplier value
        float originalSpeedMultiplier = pm.speedMultiplier;
        
        try
        {
            // Apply our speed multiplier to the ProjectileManager
            // Use multiplication to allow stacking with other speed modifiers
            pm.speedMultiplier *= speedMultiplier;
            
            Debug.Log($"[SpeedModifier] Enhancing {inner.DisplayName} with increased projectile speed: {Speed:F1} (multiplier: {speedMultiplier:F2}x)");
            
            // Call inner spell's cast with our overrides in effect
            yield return base.Cast(from, to);
        }
        finally
        {
            // Always restore original values to avoid side effects
            pm.speedMultiplier = originalSpeedMultiplier;
        }
    }
}