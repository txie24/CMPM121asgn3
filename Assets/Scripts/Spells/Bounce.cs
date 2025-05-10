using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public sealed class BounceModifier : ModifierSpell
{
    private int bounceCount = 2;
    private float bounceRange = 5f;
    private string modifierName = "bounce";
    private string modifierDescription = "Bounces to another enemy nearby.";

    public BounceModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[BounceModifier] Loading attributes from JSON");
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        if (j["count"] != null)
        {
            bounceCount = Mathf.RoundToInt(
                RPNEvaluator.SafeEvaluateFloat(j["count"].Value<string>(), vars, bounceCount));
            Debug.Log($"[BounceModifier] Loaded count={bounceCount}");
        }
        if (j["range"] != null)
        {
            bounceRange = RPNEvaluator.SafeEvaluateFloat(
                j["range"].Value<string>(), vars, bounceRange);
            Debug.Log($"[BounceModifier] Loaded range={bounceRange}");
        }

        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // No stat modifications for Bounce
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // Snapshot existing projectiles
        var before = Object
            .FindObjectsByType<ProjectileController>(FindObjectsSortMode.None)
            .ToList();

        // Cast the wrapped spell chain
        yield return inner.TryCast(from, to);

        // Identify new projectiles
        var after = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None);
        var newProjs = after.Except(before);

        // Attach bounce logic: spawn new projectile on every hit
        foreach (var ctrl in newProjs)
        {
            ctrl.OnHit += (hit, impactPos) =>
            {
                // Always trigger bounce regardless of kill
                owner.StartCoroutine(PerformBounce(impactPos, bounceCount));
            };
        }

        yield return null;
    }

    private IEnumerator PerformBounce(Vector3 origin, int remaining)
    {
        if (remaining <= 0)
            yield break;

        // Find the closest enemy to bounce towards
        GameObject next = GameManager.Instance.GetClosestEnemy(origin);
        if (next == null)
            yield break;

        Vector3 dir = (next.transform.position - origin).normalized;

        // Spawn homing projectile at the bounce point
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "homing",
            origin,
            dir,
            inner.Speed,
            (hit2, impactPos2) =>
            {
                if (hit2.team != owner.team)
                {
                    // Deal damage using the inner spell's value
                    int dmg = Mathf.RoundToInt(inner.Damage);
                    hit2.Damage(new global::Damage(dmg, global::Damage.Type.ARCANE));

                    // Continue chaining bounces
                    owner.StartCoroutine(PerformBounce(impactPos2, remaining - 1));
                }
            }
        );

        yield return null;
    }
}
