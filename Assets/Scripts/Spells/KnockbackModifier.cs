using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class KnockbackModifier : ModifierSpell
{
    private float knockbackStrength = 10f;
    private string modifierName = "knockback";
    private string modifierDescription = "Applies knockback to the enemy hit.";
    
    public KnockbackModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        Debug.Log("[KnockbackModifier] Loading attributes from JSON");
        
        // Load name and description
        modifierName = j["name"]?.Value<string>() ?? "knockback";
        modifierDescription = j["description"]?.Value<string>() ?? "Applies knockback to the enemy hit.";
        
        // Load knockback strength using RPN
        if (j["force"] != null)
        {
            string expr = j["force"].Value<string>();
            knockbackStrength = RPNEvaluator.SafeEvaluateFloat(expr, vars, 10f);
            Debug.Log($"[KnockbackModifier] Loaded knockbackStrength={knockbackStrength} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Knockback doesn't modify any stats, just adds behavior
        Debug.Log("[KnockbackModifier] No stat modifications needed for knockback effect");
    }
    
    // Override CastWithModifiers to implement the knockback behavior
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        // Get reference to ProjectileManager
        var pm = GameManager.Instance.projectileManager;
        
        // Store original onHit wrapper
        var originalOnHitWrapper = pm.onHitWrapper;
        
        try
        {
            // Set our wrapper to add knockback effect to projectile impacts
            pm.onHitWrapper = (hit, impactPos) => {
                if (hit.team != owner.team && hit.owner != null)
                {
                    // Get the Rigidbody2D from the hit target
                    Rigidbody2D enemyRb = hit.owner.GetComponent<Rigidbody2D>();
                    if (enemyRb != null && enemyRb.bodyType == RigidbodyType2D.Dynamic)
                    {
                        // Calculate knockback direction away from impact point
                        Vector2 knockbackDirection = (hit.owner.transform.position - impactPos).normalized;
                        
                        // Apply force to the enemy
                        enemyRb.AddForce(knockbackDirection * knockbackStrength, ForceMode2D.Impulse);
                        
                        Debug.Log($"[KnockbackModifier] Applied knockback to {hit.owner.name} with strength {knockbackStrength:F1} in direction {knockbackDirection}");
                    }
                }
            };
            
            Debug.Log($"[KnockbackModifier] Enhancing {inner.DisplayName} with knockback effect (strength={knockbackStrength:F1})");
            
            // Call inner spell's cast with our wrapper in effect
            yield return base.Cast(from, to);
        }
        finally
        {
            // Always restore original wrapper to avoid side effects
            pm.onHitWrapper = originalOnHitWrapper;
        }
    }
}