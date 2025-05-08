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
    
    // Override CastWithModifiers instead of Cast
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        // Get reference to ProjectileManager
        var pm = GameManager.Instance.projectileManager;
        
        // Store original values
        string originalTrajectoryOverride = pm.trajectoryOverride;
        
        try
        {
            // Set our trajectory override to homing
            pm.trajectoryOverride = "homing";
            
            Debug.Log($"[HomingModifier] Enhancing {inner.DisplayName} with homing projectiles (damage={Damage:F1}, mana={Mana:F1})");
            
            // Call inner spell's cast with our overrides in effect
            yield return base.Cast(from, to);
        }
        finally
        {
            // Always restore original values to avoid side effects
            pm.trajectoryOverride = originalTrajectoryOverride;
        }
    }
}