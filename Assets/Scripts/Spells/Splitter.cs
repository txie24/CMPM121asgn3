// File: Splitter.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Splitter : ModifierSpell
{
    private float angle = 10f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "split";

    public Splitter(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[Splitter] Loading attributes from JSON");
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["angle"] != null)
            angle = RPNEvaluator.SafeEvaluateFloat(j["angle"].Value<string>(), vars, angle);
        if (j["mana_multiplier"] != null)
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(j["mana_multiplier"].Value<string>(), vars, manaMultiplier);
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Scale mana cost
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    // After the main cast, fire two angled shots
    protected override IEnumerator PostCast(Vector3 from, Vector3 to)
    {
        Debug.Log("[Splitter] Applying split effect");

        Vector3 dir = (to - from).normalized;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float v1 = Random.Range(-2f, 2f), v2 = Random.Range(-2f, 2f);
        float leftA = baseAngle + angle + v1;
        float rightA = baseAngle - angle + v2;

        Vector3 leftDir = new Vector3(Mathf.Cos(leftA * Mathf.Deg2Rad), Mathf.Sin(leftA * Mathf.Deg2Rad), 0).normalized;
        Vector3 rightDir = new Vector3(Mathf.Cos(rightA * Mathf.Deg2Rad), Mathf.Sin(rightA * Mathf.Deg2Rad), 0).normalized;

        // First extra shot
        yield return inner.TryCast(from, from + leftDir * 10f);
        // Second extra shot
        yield return inner.TryCast(from, from + rightDir * 10f);
    }
}
