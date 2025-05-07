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
    
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // Store the original mods
        StatBlock originalMods = inner.mods;
        
        // Create and apply our mods
        StatBlock ourMods = new StatBlock();
        InjectMods(ourMods);
        inner.mods = MergeStatBlocks(originalMods, ourMods);
        
        Debug.Log($"[HomingModifier] Casting homing spell with trajectory 'homing'");
        
        // Find the closest enemy for better targeting
        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetPos = closestEnemy != null ? closestEnemy.transform.position : to;
        Vector3 direction = (targetPos - from).normalized;
        
        // Create the projectile with homing trajectory
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
        
        // Restore original mods
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