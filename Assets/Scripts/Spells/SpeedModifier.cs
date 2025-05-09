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
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["speed_multiplier"] != null)
        {
            string expr = j["speed_multiplier"].Value<string>();
            speedMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, speedMultiplier);
            Debug.Log($"[SpeedModifier] Loaded speed_multiplier={speedMultiplier} from '{expr}'");
        }
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[SpeedModifier] Injecting mods: speed×{speedMultiplier}");
        mods.speed.Add(new ValueMod(ModOp.Mul, speedMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 1) Drill down through any ModifierSpell wrappers to the true “leaf” spell
        Spell leaf = inner;
        while (leaf is ModifierSpell ms)
            leaf = ms.InnerSpell;

        // 2) Swap in our StatBlock on that leaf so its Speed is boosted
        var originalLeafMods = leaf.mods;
        leaf.mods = this.mods;

        // 3) Fire the entire wrapper chain; when it hits the leaf, Speed uses our boosted mods
        yield return inner.TryCast(from, to);

        // 4) Restore the leaf’s original mods
        leaf.mods = originalLeafMods;
    }
}
