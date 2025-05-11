// File: Assets/Scripts/Spells/Modifiers/DamageMagnifier.cs

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

        modifierName = j["name"]?.Value<string>() ?? modifierName;

        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, damageMultiplier);
            Debug.Log($"[DamageMagnifier] Loaded damage_multiplier={damageMultiplier}");
        }

        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, manaMultiplier);
            Debug.Log($"[DamageMagnifier] Loaded mana_multiplier={manaMultiplier}");
        }

        // this will rebuild this.mods via InjectMods
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[DamageMagnifier] Injecting mods: damage×{damageMultiplier}, mana×{manaMultiplier}");
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[DamageMagnifier] Casting spell with amplified damage={Damage:F1}");

        // 1) Find the deepest wrapped spell (the leaf)
        Spell leaf = inner;
        while (leaf is ModifierSpell ms)
            leaf = ms.InnerSpell;

        // 2) Swap in our damage/mana mods on that leaf
        var originalLeafMods = leaf.mods;
        leaf.mods = this.mods;

        // 3) Fire the entire chain (including Splitter → ArcaneSpray)
        yield return inner.TryCast(from, to);

        // 4) Restore the leaf’s original mods
        leaf.mods = originalLeafMods;
    }
}
