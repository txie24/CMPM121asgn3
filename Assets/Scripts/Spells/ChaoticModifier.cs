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
    
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[ChaoticModifier] Casting with damage={Damage}, using spiraling trajectory");
        
        // Create spiraling projectile
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "spiraling",  // Force spiraling trajectory
            from,
            (to - from).normalized,
            Speed,
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[ChaoticModifier] Hit {hit.owner.name} for {amount} damage");
                }
            }
        );
        
        yield return null;
    }
}