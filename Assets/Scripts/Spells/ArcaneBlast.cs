// File: Assets/Scripts/Spells/ArcaneBlast.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneBlast : Spell
{
    private string displayName;
    private string description;
    private int iconIndex;
    private float baseMana;
    private float baseCooldown;
    private string trajectory;
    private int projectileSprite;

    private string damageExpr;
    private string speedExpr;
    private string secondaryDamageExpr;
    private string secondaryCountExpr;

    private float secondarySpeed;
    private float secondaryLifetime;
    private string secondaryTrajectory;
    private int secondaryProjectileSprite;

    public ArcaneBlast(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int IconIndex => iconIndex;

    protected override float BaseDamage => RPNEvaluator.SafeEvaluateFloat(
        damageExpr,
        GetVars(),
        20f);

    protected override float BaseSpeed => RPNEvaluator.SafeEvaluateFloat(
        speedExpr,
        GetVars(),
        12f);

    protected override float BaseMana => baseMana;
    protected override float BaseCooldown => baseCooldown;

    private Dictionary<string, float> GetVars() => new() {
        { "power", owner.spellPower },
        { "wave", GetCurrentWave() }
    };

    private float GetCurrentWave()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        return spawner != null ? spawner.currentWave : 1;
    }

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        displayName = j["name"].Value<string>();
        description = j["description"]?.Value<string>() ?? "";
        iconIndex = j["icon"].Value<int>();

        damageExpr = j["damage"]["amount"].Value<string>();
        speedExpr = j["projectile"]["speed"].Value<string>();
        baseMana = RPNEvaluator.SafeEvaluateFloat(j["mana_cost"].Value<string>(), vars, 1f);
        baseCooldown = RPNEvaluator.SafeEvaluateFloat(j["cooldown"].Value<string>(), vars, 0f);

        trajectory = j["projectile"]["trajectory"].Value<string>();
        projectileSprite = j["projectile"]["sprite"]?.Value<int>() ?? 0;

        secondaryCountExpr = j["N"]?.Value<string>();
        secondaryDamageExpr = j["secondary_damage"]?.Value<string>();

        if (j["secondary_projectile"] != null)
        {
            secondaryTrajectory = j["secondary_projectile"]["trajectory"]?.Value<string>() ?? "straight";
            secondarySpeed = j["secondary_projectile"]["speed"] != null
                ? RPNEvaluator.SafeEvaluateFloat(j["secondary_projectile"]["speed"].Value<string>(), vars, 8f)
                : BaseSpeed * 0.8f;

            secondaryLifetime = j["secondary_projectile"]["lifetime"] != null
                ? float.Parse(j["secondary_projectile"]["lifetime"].Value<string>())
                : 0.3f;

            secondaryProjectileSprite = j["secondary_projectile"]["sprite"]?.Value<int>() ?? projectileSprite;
        }
        else
        {
            secondaryTrajectory = "straight";
            secondarySpeed = BaseSpeed * 0.8f;
            secondaryLifetime = 0.3f;
            secondaryProjectileSprite = projectileSprite;
        }
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Casting ▶ dmg={Damage:F1}, spd={Speed:F1}, secondary={SecondaryDamage:F1}x{SecondaryCount}");

        GameManager.Instance.projectileManager.CreateProjectile(
            projectileSprite,
            trajectory,
            from,
            to - from,
            Speed,
            (hit, impactPos) =>
            {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[{displayName}] Primary hit on {hit.owner.name} for {amount} dmg");
                    SpawnSecondaryProjectiles(impactPos);
                }
            });

        yield return null;
    }

    private float SecondaryDamage => secondaryDamageExpr != null
        ? RPNEvaluator.SafeEvaluateFloat(secondaryDamageExpr, GetVars(), Damage * 0.25f)
        : Damage * 0.25f;

    private int SecondaryCount => secondaryCountExpr != null
        ? Mathf.RoundToInt(RPNEvaluator.SafeEvaluateFloat(secondaryCountExpr, GetVars(), 8))
        : 8;

    private void SpawnSecondaryProjectiles(Vector3 position)
    {
        float angleStep = 360f / SecondaryCount;

        for (int i = 0; i < SecondaryCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0).normalized;

            GameManager.Instance.projectileManager.CreateProjectile(
                secondaryProjectileSprite,
                secondaryTrajectory,
                position,
                direction,
                secondarySpeed * Speed / BaseSpeed,
                (hit, impactPos) =>
                {
                    if (hit.team != owner.team)
                    {
                        int amt = Mathf.RoundToInt(SecondaryDamage);
                        var dmg = new global::Damage(amt, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[{displayName}] Secondary hit on {hit.owner.name} for {amt} dmg");
                    }
                },
                secondaryLifetime);
        }
    }
}
