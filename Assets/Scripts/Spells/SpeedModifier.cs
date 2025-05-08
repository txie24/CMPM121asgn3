using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class SpeedModifier : ModifierSpell
{
    private float speedMultiplier = 1.75f;
    private string modifierName = "speed-amplified";
    
    public SpeedModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[SpeedModifier] Loading attributes from JSON");
        
        // Load name
        modifierName = j["name"]?.Value<string>() ?? "speed-amplified";
        
        // Load speed multiplier using RPN
        if (j["speed_multiplier"] != null)
        {
            string expr = j["speed_multiplier"].Value<string>();
            speedMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.75f);
            Debug.Log($"[SpeedModifier] Loaded speed_multiplier={speedMultiplier} from expression '{expr}'");
        }
        
        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[SpeedModifier] Injecting mods: speed×{speedMultiplier}");
        mods.speed.Add(new ValueMod(ModOp.Mul, speedMultiplier));
    }
    
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[SpeedModifier] Casting with speed={Speed}");
        // Simply pass through to inner spell with our modifiers applied
        yield return inner.TryCast(from, to);
    }
}