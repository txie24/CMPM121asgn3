// File: ChaoticModifier.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ChaoticModifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private string modifierName = "chaotic";

    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[ChaoticModifier] Loading attributes from JSON");
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(j["damage_multiplier"].Value<string>(), vars, damageMultiplier);
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Scale damage
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }

    // After the main cast, spawn a spiraling burst
    protected override IEnumerator PostCast(Vector3 from, Vector3 to)
    {
        Debug.Log("[ChaoticModifier] Applying chaotic spiraling effect");

        // Approximate number of petals by final Damage
        int count = Mathf.RoundToInt(Damage) + 5;
        float sprayArc = 60f;
        float step = sprayArc / (count - 1);
        float startAngle = Mathf.Atan2((to - from).y, (to - from).x) * Mathf.Rad2Deg - sprayArc / 2;

        for (int i = 0; i < count; i++)
        {
            float ang = startAngle + step * i;
            Vector3 d = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad), 0).normalized;

            GameManager.Instance.projectileManager.CreateProjectile(
                inner.IconIndex,
                "spiraling",
                from,
                d,
                inner.Speed,
                (hit, pos) =>
                {
                    if (hit.team != owner.team)
                        hit.Damage(new global::Damage(Mathf.RoundToInt(Damage), global::Damage.Type.ARCANE));
                }
            );
            yield return new WaitForSeconds(0.02f);
        }
    }
}
