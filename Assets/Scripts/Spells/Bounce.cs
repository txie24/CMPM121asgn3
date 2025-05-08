// File: Assets/Scripts/Spells/Modifiers/Bounce.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class BounceModifier : ModifierSpell
{
    private int bounceCount = 1;
    private float bounceRange = 5f;
    private string modifierName = "bounce";
    private string modifierDescription = "Bounces to another enemy nearby.";

    public BounceModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        base.LoadAttributes(j, vars);
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;
        if (j["count"] != null)
            bounceCount = Mathf.RoundToInt(RPNEvaluator.SafeEvaluateFloat(j["count"].Value<string>(), vars, 1f));
        if (j["range"] != null)
            bounceRange = RPNEvaluator.SafeEvaluateFloat(j["range"].Value<string>(), vars, 5f);
    }

    protected override void InjectMods(StatBlock mods) { }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;

        GameManager.Instance.projectileManager.CreateProjectile(
            0, // base sprite index
            "straight",
            from,
            direction,
            inner.Speed,
            (hit, impactPos) =>
            {
                inner.Owner.StartCoroutine(DoBounce(hit, impactPos));
            });

        yield return null;
    }

    private IEnumerator DoBounce(Hittable initialTarget, Vector3 origin)
    {
        GameObject lastTarget = initialTarget.owner;
        Vector3 bounceFrom = origin;

        for (int i = 0; i < bounceCount; i++)
        {
            GameObject next = GameManager.Instance.GetClosestEnemy(bounceFrom);
            if (next == null || next == lastTarget) yield break;

            Vector3 toNext = next.transform.position - bounceFrom;
            yield return inner.Owner.StartCoroutine(inner.TryCast(bounceFrom, bounceFrom + toNext));

            lastTarget = next;
            bounceFrom = next.transform.position;
        }
    }
}