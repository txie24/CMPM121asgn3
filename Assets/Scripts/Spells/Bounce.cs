// Bounce.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public sealed class BounceModifier : ModifierSpell
{
    private int bounceCount = 2;
    private float bounceRange = 15f;
    private string modifierName = "bounce";
    private string modifierDescription = "Bounces to another enemy or in a random direction.";

    public BounceModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[BounceModifier] Loading attributes from JSON");

        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        // read bounce count from json (fallback to existing value)
        if (j["bounceCount"] != null)
        {
            bounceCount = Mathf.RoundToInt(
                RPNEvaluator.SafeEvaluateFloat(j["bounceCount"].Value<string>(), vars, bounceCount));
            Debug.Log($"[BounceModifier] Loaded bounceCount={bounceCount}");
        }

        // read bounce range from json (fallback to existing value)
        if (j["bounceRange"] != null)
        {
            bounceRange = RPNEvaluator.SafeEvaluateFloat(
                j["bounceRange"].Value<string>(), vars, bounceRange);
            Debug.Log($"[BounceModifier] Loaded bounceRange={bounceRange}");
        }

        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods) { }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // snapshot existing projectiles
        var before = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).ToList();
        // cast the wrapped spell
        yield return inner.TryCast(from, to);
        // find the new ones
        var after = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None);
        var newProjs = after.Except(before);

        foreach (var ctrl in newProjs)
        {
            ctrl.OnHit += (hit, impactPos) =>
                owner.StartCoroutine(PerformBounce(impactPos, hit.owner, bounceCount));
        }
        yield return null;
    }

    private IEnumerator PerformBounce(Vector3 origin, GameObject previousTarget, int remaining)
    {
        if (remaining <= 0)
            yield break;

        // gather enemies except the one we just hit, within bounceRange
        var others = Object
            .FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
            .Select(ec => ec.gameObject)
            .Where(go => go != previousTarget)
            .Where(go => Vector3.Distance(origin, go.transform.position) <= bounceRange)
            .ToList();

        Vector3 dir;
        if (others.Count > 0)
        {
            // home in on the closest valid target
            var next = others
                .OrderBy(go => Vector3.Distance(origin, go.transform.position))
                .First();
            dir = (next.transform.position - origin).normalized;
        }
        else
        {
            // no enemy in range? still bounce off in a random direction
            dir = Random.insideUnitCircle.normalized;
        }

        // spawn the next projectile
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "homing",        // will track if an enemy comes into view
            origin,
            dir,
            inner.Speed,
            (hit2, impactPos2) =>
            {
                if (hit2.team != owner.team)
                {
                    int dmg = Mathf.RoundToInt(inner.Damage);
                    hit2.Damage(new global::Damage(dmg, global::Damage.Type.ARCANE));
                    // chain the next bounce
                    owner.StartCoroutine(PerformBounce(impactPos2, hit2.owner, remaining - 1));
                }
            }
        );

        yield return null;
    }
}
