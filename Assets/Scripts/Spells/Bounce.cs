using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class BounceModifier : ModifierSpell
{
    private int bounceCount = 2;
    private float bounceRange = 5f;
    private string modifierName = "bounce";
    private string modifierDescription = "Bounces to another enemy nearby.";

    public BounceModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[BounceModifier] Loading attributes from JSON");
        
        // Load name and description
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;
        
        // Load bounce count using RPN
        if (j["count"] != null)
        {
            string expr = j["count"].Value<string>();
            bounceCount = Mathf.RoundToInt(RPNEvaluator.SafeEvaluateFloat(expr, vars, 2f));
            Debug.Log($"[BounceModifier] Loaded bounceCount={bounceCount} from expression '{expr}'");
        }
        
        // Load bounce range using RPN
        if (j["range"] != null)
        {
            string expr = j["range"].Value<string>();
            bounceRange = RPNEvaluator.SafeEvaluateFloat(expr, vars, 5f);
            Debug.Log($"[BounceModifier] Loaded bounceRange={bounceRange} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Bounce doesn't modify any stats, just adds behavior
        Debug.Log($"[BounceModifier] No stat modifications needed for bounce effect (count={bounceCount}, range={bounceRange})");
    }
    
    // Override CastWithModifiers to implement the bouncing behavior
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        // Get reference to ProjectileManager
        var pm = GameManager.Instance.projectileManager;
        
        // Store original onHit wrapper
        var originalOnHitWrapper = pm.onHitWrapper;
        
        try
        {
            // Set our wrapper to add bounce effect to projectile impacts
            pm.onHitWrapper = (hit, impactPos) => {
                if (hit.team != owner.team && hit.owner != null)
                {
                    // Start the bounce chain from this hit
                    owner.StartCoroutine(DoBounce(hit, impactPos, bounceCount));
                }
            };
            
            Debug.Log($"[BounceModifier] Enhancing {inner.DisplayName} with bounce effect (count={bounceCount}, range={bounceRange})");
            
            // Call inner spell's cast with our wrapper in effect
            yield return base.Cast(from, to);
        }
        finally
        {
            // Always restore original wrapper to avoid side effects
            pm.onHitWrapper = originalOnHitWrapper;
        }
    }
    
    // Helper method to handle bounce chain
    private IEnumerator DoBounce(Hittable initialTarget, Vector3 origin, int remainingBounces)
    {
        // If no more bounces left, stop
        if (remainingBounces <= 0)
            yield break;
            
        // Remember the initial target to avoid bouncing back to it
        GameObject lastHitTarget = initialTarget.owner;
        
        // Wait a small amount to make bounces more visible
        yield return new WaitForSeconds(0.1f);
        
        // Find the next target
        GameObject nextTarget = FindNextTarget(origin, lastHitTarget);
        if (nextTarget == null)
        {
            Debug.Log("[BounceModifier] No valid targets found for bounce");
            yield break;
        }
        
        // Calculate direction to next target
        Vector3 bounceFrom = origin;
        Vector3 bounceTo = nextTarget.transform.position;
        Vector3 direction = (bounceTo - bounceFrom).normalized;
        
        Debug.Log($"[BounceModifier] Bouncing from {lastHitTarget.name} to {nextTarget.name} ({remainingBounces} bounces left)");
        
        // Cast the inner spell toward the new target
        yield return inner.TryCast(bounceFrom, bounceTo);
        
        // The onHitWrapper will handle the next bounce in the chain
    }
    
    // Helper method to find next target for bouncing
    private GameObject FindNextTarget(Vector3 origin, GameObject excludeTarget)
    {
        // Get all enemies
        var enemies = new List<GameObject>();
        foreach (var enemy in Object.FindObjectsOfType<EnemyController>())
        {
            if (enemy.gameObject != excludeTarget && !enemy.dead && 
                Vector3.Distance(origin, enemy.transform.position) <= bounceRange)
            {
                enemies.Add(enemy.gameObject);
            }
        }
        
        // If no valid targets, return null
        if (enemies.Count == 0)
            return null;
            
        // Find the closest valid target
        GameObject closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(origin, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        
        return closest;
    }
}