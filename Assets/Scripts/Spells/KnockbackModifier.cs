using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class KnockbackModifier : ModifierSpell
{
    private float knockbackStrength = 5f;
    private string modifierName = "knockback";
    private string modifierDescription = "Adds a knockback effect to the spell.";
    
    public KnockbackModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "knockback";
        modifierDescription = j["description"]?.Value<string>() ?? "Adds a knockback effect to the spell.";
        
        if (j["knockback_strength"] != null)
        {
            string expr = j["knockback_strength"].Value<string>();
            knockbackStrength = RPNEvaluator.SafeEvaluateFloat(expr, vars, 5f);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // No stat modifications needed for knockback, handled in Cast
    }
    
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // Store the original mods
        StatBlock originalMods = inner.mods;
        
        Debug.Log($"[KnockbackModifier] Casting knockback spell with strength={knockbackStrength:F1}");
        
        // Get the direction from the from position to the to position
        Vector3 direction = (to - from).normalized;
        
        // Use the inner spell's projectile sprite and trajectory
        int projectileSprite = 0; // Default sprite, should be loaded from inner spell
        string trajectory = "straight"; // Default trajectory, should be loaded from inner spell
        if (inner is ArcaneBolt arcaneBolt)
        {
            var jObject = JObject.Parse(JsonUtility.ToJson(inner));
            projectileSprite = jObject["projectileSprite"]?.Value<int>() ?? 0;
            trajectory = jObject["trajectory"]?.Value<string>() ?? "straight";
        }

        // Create the projectile with the base spell's properties
        GameManager.Instance.projectileManager.CreateProjectile(
            projectileSprite,
            trajectory,
            from,
            direction,
            inner.Speed,
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(inner.Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[KnockbackModifier] Hit {hit.owner.name} for {amount} damage");

                    // Apply knockback using the hit.owner (GameObject)
                    if (hit.owner != null)
                    {
                        Rigidbody2D enemyRb = hit.owner.GetComponent<Rigidbody2D>();
                        if (enemyRb != null && enemyRb.bodyType == RigidbodyType2D.Dynamic)
                        {
                            Vector2 knockbackDirection = (hit.owner.transform.position - impactPos).normalized;
                            enemyRb.AddForce(knockbackDirection * knockbackStrength, ForceMode2D.Impulse);
                            Debug.Log($"[KnockbackModifier] Applied knockback to {hit.owner.name} with strength {knockbackStrength:F1}");
                        }
                    }
                }
            }
        );
        
        // Restore original mods
        inner.mods = originalMods;
        
        yield return null;
    }
    
    // Helper method to merge StatBlocks (optional, included for consistency)
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