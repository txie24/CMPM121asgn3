using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Splitter : ModifierSpell
{
    private float angle = 10f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "split";
    private string modifierDescription = "Spell is cast twice in slightly different directions; increased mana cost.";
    
    public Splitter(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "split";
        modifierDescription = j["description"]?.Value<string>() ?? "Spell is cast twice in slightly different directions; increased mana cost.";
        
        if (j["angle"] != null)
        {
            string expr = j["angle"].Value<string>();
            angle = float.Parse(expr);
        }
        
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Increase mana cost
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    protected override IEnumerator ModifierCast(Vector3 from, Vector3 to)
    {
        // Calculate two different directions
        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Add slight randomization to make it look more natural
        float randomVariation1 = Random.Range(-2f, 2f);
        float randomVariation2 = Random.Range(-2f, 2f);
        
        Vector3 dir1 = new Vector3(
            Mathf.Cos((baseAngle + angle + randomVariation1) * Mathf.Deg2Rad),
            Mathf.Sin((baseAngle + angle + randomVariation1) * Mathf.Deg2Rad),
            0
        ).normalized;
        
        Vector3 dir2 = new Vector3(
            Mathf.Cos((baseAngle - angle + randomVariation2) * Mathf.Deg2Rad),
            Mathf.Sin((baseAngle - angle + randomVariation2) * Mathf.Deg2Rad),
            0
        ).normalized;
        
        // Use these directions to calculate new target positions
        Vector3 target1 = from + dir1 * 10f;
        Vector3 target2 = from + dir2 * 10f;
        
        Debug.Log($"[Splitter] Casting {inner.DisplayName} in two directions");
        
        // Handle special case for nested with ChaoticModifier
        if (inner is ChaoticModifier)
        {
            // Create one spiraling projectile for each direction
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // Fixed projectile sprite index
                "spiraling", // Force spiraling trajectory
                from,
                dir1,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(inner.Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[Splitter+Chaotic] Hit {hit.owner.name} for {amount} damage (dir1)");
                    }
                }
            );
            
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // Fixed projectile sprite index
                "spiraling", // Force spiraling trajectory
                from,
                dir2,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(inner.Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[Splitter+Chaotic] Hit {hit.owner.name} for {amount} damage (dir2)");
                    }
                }
            );
            
            yield return null;
        }
        // Handle special case for nested with HomingModifier
        else if (inner is HomingModifier)
        {
            // Create one homing projectile for each direction (they'll find targets)
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // Fixed projectile sprite index
                "homing", // Force homing trajectory
                from,
                dir1,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(inner.Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[Splitter+Homing] Hit {hit.owner.name} for {amount} damage (dir1)");
                    }
                }
            );
            
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // Fixed projectile sprite index
                "homing", // Force homing trajectory
                from,
                dir2,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(inner.Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[Splitter+Homing] Hit {hit.owner.name} for {amount} damage (dir2)");
                    }
                }
            );
            
            yield return null;
        }
        else
        {
            // Cast inner spell in both directions
            yield return inner.TryCast(from, target1);
            yield return inner.TryCast(from, target2);
        }
    }
}