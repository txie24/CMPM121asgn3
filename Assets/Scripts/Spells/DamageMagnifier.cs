using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class DamageMagnifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "damage-amplified";
    
    public DamageMagnifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[DamageMagnifier] Loading attributes from JSON");
        
        // Load name
        modifierName = j["name"]?.Value<string>() ?? "damage-amplified";
        
        // Load damage multiplier using RPN
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
            Debug.Log($"[DamageMagnifier] Loaded damage_multiplier={damageMultiplier} from expression '{expr}'");
        }
        
        // Load mana multiplier using RPN
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
            Debug.Log($"[DamageMagnifier] Loaded mana_multiplier={manaMultiplier} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[DamageMagnifier] Injecting mods: damage×{damageMultiplier}, mana×{manaMultiplier}");
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }
    
    // Override CastWithModifiers to add custom behavior if needed
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        // For DamageMagnifier, we don't need to override projectile creation
        // We only need to apply stat modifications which is done in InjectMods
        // Those modifications affect the damage calculated for all projectiles
        
        Debug.Log($"[DamageMagnifier] Enhancing {inner.DisplayName} with increased damage: {Damage:F1} (multiplier: {damageMultiplier:F2}x)");
        
        // Get a reference to the ProjectileManager for potential future enhancements
        var pm = GameManager.Instance.projectileManager;
        
        // Simply delegate to the inner spell with our stat modifiers applied
        yield return base.Cast(from, to);
    }
}