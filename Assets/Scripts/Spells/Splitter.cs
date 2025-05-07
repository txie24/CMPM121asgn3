// File: Assets/Scripts/Spells/Modifiers/Splitter.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Splitter : ModifierSpell
{
    private float angle = 10f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "split";
    private string modifierDescription = "Spell is cast twice in slightly different directions; increased mana cost.";

    public Splitter(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        base.LoadAttributes(j, vars);

        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        if (j["angle"] != null)
        {
            string expr = j["angle"].Value<string>();
            angle = RPNEvaluator.SafeEvaluateFloat(expr, vars, 10f);
        }

        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        StatBlock originalMods = inner.mods;
        StatBlock ourMods = new StatBlock();
        InjectMods(ourMods);
        inner.mods = MergeStatBlocks(originalMods, ourMods);

        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float randomVariation1 = Random.Range(-2f, 2f);
        float randomVariation2 = Random.Range(-2f, 2f);

        Vector3 dir1 = new Vector3(
            Mathf.Cos((baseAngle + angle + randomVariation1) * Mathf.Deg2Rad),
            Mathf.Sin((baseAngle + angle + randomVariation1) * Mathf.Deg2Rad),
            0).normalized;

        Vector3 dir2 = new Vector3(
            Mathf.Cos((baseAngle - angle + randomVariation2) * Mathf.Deg2Rad),
            Mathf.Sin((baseAngle - angle + randomVariation2) * Mathf.Deg2Rad),
            0).normalized;

        Vector3 target1 = from + dir1 * 10f;
        Vector3 target2 = from + dir2 * 10f;

        Debug.Log($"[Splitter] Casting {inner.DisplayName} in two directions (±{angle}°)");

        yield return inner.TryCast(from, target1);
        yield return inner.TryCast(from, target2);

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