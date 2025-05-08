using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Doubler : ModifierSpell
{
    private float delay = 0.5f;
    private float manaMultiplier = 1.5f;
    private float cooldownMultiplier = 1.5f;
    private string modifierName = "doubled";
    
    public Doubler(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[Doubler] Loading attributes from JSON");
        
        // Load name
        modifierName = j["name"]?.Value<string>() ?? "doubled";
        
        // Load delay using RPN
        if (j["delay"] != null)
        {
            string expr = j["delay"].Value<string>();
            delay = RPNEvaluator.SafeEvaluateFloat(expr, vars, 0.5f);
            Debug.Log($"[Doubler] Loaded delay={delay} from expression '{expr}'");
        }
        
        // Load mana multiplier using RPN
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
            Debug.Log($"[Doubler] Loaded mana_multiplier={manaMultiplier} from expression '{expr}'");
        }
        
        // Load cooldown multiplier using RPN
        if (j["cooldown_multiplier"] != null)
        {
            string expr = j["cooldown_multiplier"].Value<string>();
            cooldownMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
            Debug.Log($"[Doubler] Loaded cooldown_multiplier={cooldownMultiplier} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[Doubler] Injecting mods: mana×{manaMultiplier}, cooldown×{cooldownMultiplier}");
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
        mods.cd.Add(new ValueMod(ModOp.Mul, cooldownMultiplier));
    }
    
    // Override CastWithModifiers to implement the double-casting behavior
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        Debug.Log($"[Doubler] Casting {inner.DisplayName} twice with delay={delay:F1}s");
        
        // Cast first instance normally
        yield return base.Cast(from, to);
        
        // Wait for specified delay between casts
        Debug.Log($"[Doubler] Waiting {delay}s before second cast");
        yield return new WaitForSeconds(delay);
        
        // Get updated position for second cast (in case the player moved)
        Vector3 secondFrom = owner.transform.position;
        
        // Keep same direction but adjust for new starting position
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(to, from);
        Vector3 secondTo = secondFrom + direction * distance;
        
        Debug.Log($"[Doubler] Casting second instance of {inner.DisplayName} from updated position");
        
        // Cast second instance from new position
        yield return inner.TryCast(secondFrom, secondTo);
    }
}