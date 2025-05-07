using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class HomingModifier : ModifierSpell
{
    private float damageMultiplier = 0.75f;
    private float manaAdder = 10f;
    private string modifierName = "homing";
    private string modifierDescription = "Homing projectile, with decreased damage and increased mana cost.";
    
    public HomingModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "homing";
        modifierDescription = j["description"]?.Value<string>() ?? "Homing projectile, with decreased damage and increased mana cost.";
        
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        if (j["mana_adder"] != null)
        {
            string expr = j["mana_adder"].Value<string>();
            manaAdder = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Decrease damage
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        
        // Increase mana cost (addition)
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
    }
    
    protected override IEnumerator ModifierCast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[HomingModifier] ModifierCast() with homing projectile");
        
        // Find the closest enemy for better targeting
        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetPos = closestEnemy != null ? closestEnemy.transform.position : to;
        Vector3 direction = (targetPos - from).normalized;
        
        // For Splitter, let it handle its own logic first
        if (inner is Splitter)
        {
            // Let Splitter handle the casting with our mods applied
            yield return inner.TryCast(from, to);
        }
        else if (inner is ChaoticModifier)
        {
            // We need to prioritize one behavior - choosing homing since it's the outer modifier
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // Fixed projectile sprite index
                "homing", // Force homing trajectory
                from,
                direction,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(inner.Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[HomingModifier+Chaotic] Hit {hit.owner.name} for {amount} damage");
                    }
                }
            );
            
            yield return null;
        }
        else
        {
            // Create a standard homing projectile
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // Fixed projectile sprite index 
                "homing", // Force homing trajectory
                from,
                direction,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(inner.Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[HomingModifier] Hit {hit.owner.name} for {amount} damage");
                    }
                }
            );
            
            yield return null;
        }
    }
}