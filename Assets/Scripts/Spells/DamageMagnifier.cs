using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class DamageMagnifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "damage-amplified";
    private string modifierDescription = "Increased damage and increased mana cost.";
    
    public DamageMagnifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "damage-amplified";
        modifierDescription = j["description"]?.Value<string>() ?? "Increased damage and increased mana cost.";
        
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
        }
        
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Use the values loaded from JSON to modify damage and mana
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // Store the original mods
        StatBlock originalMods = inner.mods;
        
        // Create a new StatBlock and apply our damage and mana modifiers
        StatBlock ourMods = new StatBlock();
        InjectMods(ourMods);
        
        // Merge the StatBlocks
        inner.mods = MergeStatBlocks(originalMods, ourMods);
        
        Debug.Log($"[DamageMagnifier] Casting damage-amplified spell with damage multiplier={damageMultiplier:F2}x, mana multiplier={manaMultiplier:F2}x");
        
        // Get the direction from the from position to the to position
        Vector3 direction = (to - from).normalized;
        
        // Compute the modified damage using the inner spell's Damage property after applying mods
        float modifiedDamage = inner.Damage; // This should reflect the multiplied damage
        
        // Use the inner spell's projectile sprite and trajectory
        int projectileSprite = 0; // Default sprite, should be loaded from inner spell
        string trajectory = "straight"; // Default trajectory, should be loaded from inner spell

        // Attempt to get sprite and trajectory from inner spell (similar to ArcaneBolt)
        if (inner is ArcaneBolt arcaneBolt)
        {
            // Use reflection or cast to access fields if necessary (since they're private in ArcaneBolt)
            var jObject = JObject.Parse(JsonUtility.ToJson(inner));
            projectileSprite = jObject["projectileSprite"]?.Value<int>() ?? 0;
            trajectory = jObject["trajectory"]?.Value<string>() ?? "straight";
        }

        // Create the projectile using ProjectileManager
        GameManager.Instance.projectileManager.CreateProjectile(
            projectileSprite,
            trajectory,
            from,
            direction,
            inner.Speed, // Use the modified speed (if any)
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    // Apply the modified damage directly
                    int amount = Mathf.RoundToInt(modifiedDamage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[DamageMagnifier] Hit {hit.owner.name} for {amount} damage");
                }
            }
        );
        
        // Restore the original mods
        inner.mods = originalMods;
        
        yield return null;
    }

    // Helper method to merge StatBlocks
    private StatBlock MergeStatBlocks(StatBlock a, StatBlock b)
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
}