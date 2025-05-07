// File: Assets/Scripts/Spells/Modifiers/Doubler.cs

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
    private string modifierDescription = "Spell is cast a second time after a small delay; increased mana cost and cooldown.";

    public Doubler(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        base.LoadAttributes(j, vars);

        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        if (j["delay"] != null)
        {
            string expr = j["delay"].Value<string>();
            delay = RPNEvaluator.SafeEvaluateFloat(expr, vars, 0.5f);
        }

        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
        }

        if (j["cooldown_multiplier"] != null)
        {
            string expr = j["cooldown_multiplier"].Value<string>();
            cooldownMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
        mods.cd.Add(new ValueMod(ModOp.Mul, cooldownMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        StatBlock originalMods = inner.mods;
        StatBlock ourMods = new StatBlock();
        InjectMods(ourMods);
        inner.mods = MergeStatBlocks(originalMods, ourMods);

        Debug.Log($"[Doubler] First cast of {inner.DisplayName}");
        yield return inner.TryCast(from, to);

        yield return new WaitForSeconds(delay);
        Debug.Log($"[Doubler] Second cast of {inner.DisplayName} after {delay}s delay");

        // Ensure second cast uses same direction and position reference
        Vector3 secondFrom = owner.transform.position;
        Vector3 secondTo = secondFrom + (to - from); // keep original direction
        yield return inner.TryCast(secondFrom, secondTo);

        inner.mods = originalMods;
    }

    private StatBlock MergeStatBlocks(StatBlock a, StatBlock b)
    {
        StatBlock result = new StatBlock();
        result.damage.AddRange(a.damage); result.damage.AddRange(b.damage);
        result.mana.AddRange(a.mana); result.mana.AddRange(b.mana);
        result.speed.AddRange(a.speed); result.speed.AddRange(b.speed);
        result.cd.AddRange(a.cd); result.cd.AddRange(b.cd);
        return result;
    }
}
