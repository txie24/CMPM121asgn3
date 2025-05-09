using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class SpeedModifier : ModifierSpell
{
    private float speedMultiplier = 1.75f;
    private string modifierName = "speed-amplified";

    public SpeedModifier(Spell inner) : base(inner) { }

    // This suffix gets appended to the wrapped spell’s DisplayName
    protected override string Suffix => modifierName;

    // Load name & multiplier from JSON via RPN, then register the mods
    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[SpeedModifier] Loading attributes from JSON");

        // Override name if provided
        modifierName = j["name"]?.Value<string>() ?? modifierName;

        // Parse speed_multiplier expression
        if (j["speed_multiplier"] != null)
        {
            string expr = j["speed_multiplier"].Value<string>();
            speedMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, speedMultiplier);
            Debug.Log($"[SpeedModifier] Loaded speed_multiplier={speedMultiplier} from expression '{expr}'");
        }

        // Now let base register our InjectMods() call
        base.LoadAttributes(j, vars);
    }

    // Actually inject the multiplier into the StatBlock
    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[SpeedModifier] Injecting mods: speed×{speedMultiplier}");
        mods.speed.Add(new ValueMod(ModOp.Mul, speedMultiplier));
    }

    // Swap the inner spell’s mods to include our speed boost, then restore
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[SpeedModifier] Casting with speed={Speed}");

        // 1) Save original inner.mods
        var originalInnerMods = inner.mods;
        // 2) Use our wrapper’s mods so inner.Speed is amplified
        inner.mods = this.mods;
        // 3) Cast the inner spell (now with boosted speed)
        yield return inner.TryCast(from, to);
        // 4) Restore the inner spell’s original mods
        inner.mods = originalInnerMods;
    }
}
