using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ChaoticModifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private string modifierName = "chaotic";
    private string modifierDescription = "Significantly increased damage, but projectile is spiraling.";
    
    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "chaotic";
        modifierDescription = j["description"]?.Value<string>() ?? "Significantly increased damage, but projectile is spiraling.";
        
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Increase damage
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }
    
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // Store the original mods
        StatBlock originalMods = inner.mods;
        
        // Create a new StatBlock and apply our damage modifier
        StatBlock ourMods = new StatBlock();
        InjectMods(ourMods);
        
        // Merge the StatBlocks
        inner.mods = MergeStatBlocks(originalMods, ourMods);
        
        Debug.Log($"[ChaoticModifier] Casting chaotic spell with trajectory 'spiraling'");
        
        // Get the direction from the from position to the to position
        Vector3 direction = (to - from).normalized;
        
        // Create the projectile directly using ProjectileManager with spiraling trajectory
        GameManager.Instance.projectileManager.CreateProjectile(
            0, // Fixed projectile sprite index (you might want to preserve the original)
            "spiraling", // Force spiraling trajectory
            from,
            direction,
            inner.Speed, // Use the modified speed
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    // Apply damage using the inner spell's damage value which includes our modifier
                    int amount = Mathf.RoundToInt(inner.Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[ChaoticModifier] Hit {hit.owner.name} for {amount} damage");
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