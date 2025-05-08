using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class HomingModifier : ModifierSpell
{
    private float damageMultiplier = 0.75f;
    private float manaAdder = 10f;
    private string modifierName = "homing";
    
    public HomingModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[HomingModifier] Loading attributes from JSON");
        
        // Load name
        modifierName = j["name"]?.Value<string>() ?? "homing";
        
        // Load damage multiplier using RPN
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 0.75f);
            Debug.Log($"[HomingModifier] Loaded damage_multiplier={damageMultiplier} from expression '{expr}'");
        }
        
        // Load mana adder using RPN
        if (j["mana_adder"] != null)
        {
            string expr = j["mana_adder"].Value<string>();
            manaAdder = RPNEvaluator.SafeEvaluateFloat(expr, vars, 10f);
            Debug.Log($"[HomingModifier] Loaded mana_adder={manaAdder} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[HomingModifier] Injecting mods: damage×{damageMultiplier}, mana+{manaAdder}");
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
    }
    
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[HomingModifier] Casting with damage={Damage}, mana={Mana}");
        
        // Find closest enemy for better targeting
        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetPos = closestEnemy != null ? closestEnemy.transform.position : to;
        Vector3 direction = (targetPos - from).normalized;
        
        // Create the homing projectile
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "homing",
            from,
            direction,
            Speed,
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[HomingModifier] Hit {hit.owner.name} for {amount} damage");
                }
            }
        );
        
        yield return null;
    }
}