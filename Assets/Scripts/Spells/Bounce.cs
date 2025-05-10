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
    private float damageMultiplier = 0.5f;
    private float speedMultiplier = 1.5f;      // default, will be overwritten by JSON
    private string modifierName = "bounce";
    private string modifierDescription = "Bounces to another enemy or in a random direction.";

    public BounceModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[BounceModifier] Loading attributes from JSON");

        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        if (j["bounceCount"] != null)
        {
            bounceCount = Mathf.RoundToInt(
                RPNEvaluator.SafeEvaluateFloat(
                    j["bounceCount"].Value<string>(), vars, bounceCount));
            Debug.Log($"[BounceModifier] Loaded bounceCount={bounceCount}");
        }

        if (j["bounceRange"] != null)
        {
            bounceRange = RPNEvaluator.SafeEvaluateFloat(
                j["bounceRange"].Value<string>(), vars, bounceRange);
            Debug.Log($"[BounceModifier] Loaded bounceRange={bounceRange}");
        }

        if (j["damage_multiplier"] != null)
        {
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(), vars, damageMultiplier);
            Debug.Log($"[BounceModifier] Loaded damageMultiplier={damageMultiplier}");
        }

        if (j["speedMultiplier"] != null)
        {
            speedMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["speedMultiplier"].Value<string>(), vars, speedMultiplier);
            Debug.Log($"[BounceModifier] Loaded speedMultiplier={speedMultiplier}");
        }

        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods) { }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        var before = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).ToList();
        yield return inner.TryCast(from, to);
        var after = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None);
        var newProjs = after.Except(before);

        foreach (var ctrl in newProjs)
            ctrl.OnHit += (hit, impactPos) =>
                owner.StartCoroutine(PerformBounce(impactPos, hit.owner, bounceCount));

        yield return null;
    }

    private IEnumerator PerformBounce(Vector3 origin, GameObject previousTarget, int remaining)
    {
        if (remaining <= 0)
            yield break;

        var others = Object
            .FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
            .Select(ec => ec.gameObject)
            .Where(go => go != previousTarget && Vector3.Distance(origin, go.transform.position) <= bounceRange)
            .ToList();

        Vector3 dir;
        if (others.Count > 0)
        {
            var next = others.OrderBy(go => Vector3.Distance(origin, go.transform.position)).First();
            dir = (next.transform.position - origin).normalized;
        }
        else
        {
            dir = Random.insideUnitCircle.normalized;
        }

        float spawnSpeed = inner.Speed * speedMultiplier;

        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "homing",
            origin,
            dir,
            spawnSpeed,
            (hit2, impactPos2) =>
            {
                if (hit2.team != owner.team)
                {
                    int baseDmg = Mathf.RoundToInt(inner.Damage);
                    int bounceDmg = Mathf.RoundToInt(baseDmg * damageMultiplier);
                    hit2.Damage(new global::Damage(bounceDmg, global::Damage.Type.ARCANE));
                    owner.StartCoroutine(PerformBounce(impactPos2, hit2.owner, remaining - 1));
                }
            }
        );

        yield return null;
    }
}
