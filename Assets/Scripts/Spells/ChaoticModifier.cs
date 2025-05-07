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
    
    protected override IEnumerator ModifierCast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[ChaoticModifier] ModifierCast() with spiraling projectile");
        
        // Get the direction from start to target
        Vector3 direction = (to - from).normalized;
        
        // Create a spiraling projectile
        GameManager.Instance.projectileManager.CreateProjectile(
            0, // Fixed projectile sprite index
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
        
        yield return null;
    }
}