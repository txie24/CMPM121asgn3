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

    private Dictionary<string, float> GetVars() => new()
    {
        { "power", owner.spellPower },
        { "wave",  GetCurrentWave()       }
    };

    private float GetCurrentWave()
    {
        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
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
        // 1) Capture the amplified values up‑front
        float primaryDamage = Damage;
        float secondaryDamage = secondaryDamageExpr != null
            ? RPNEvaluator.SafeEvaluateFloat(secondaryDamageExpr, GetVars(), primaryDamage * 0.25f)
            : primaryDamage * 0.25f;
        int secondaryCount = secondaryCountExpr != null
            ? Mathf.RoundToInt(RPNEvaluator.SafeEvaluateFloat(secondaryCountExpr, GetVars(), 8))
            : 8;

        Debug.Log($"[{displayName}] Casting ▶ dmg={primaryDamage:F1}, spd={Speed:F1}, secondary={secondaryDamage:F1}x{secondaryCount}");

        // 2) Fire primary projectile
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
                    int amt = Mathf.RoundToInt(primaryDamage);
                    var dmg = new global::Damage(amt, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[{displayName}] Primary hit on {hit.owner.name} for {amt} dmg");

                    // 3) Spawn secondaries exactly at the real impact point
                    SpawnSecondaryProjectiles(impactPos, secondaryDamage, secondaryCount);
                }
            });

        yield return null;
    }

    private void SpawnSecondaryProjectiles(Vector3 origin, float secondaryDamage, int count)
    {
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0).normalized;

            GameManager.Instance.projectileManager.CreateProjectile(
                secondaryProjectileSprite,
                secondaryTrajectory,
                origin,
                dir,
                secondarySpeed * Speed / BaseSpeed,
                (hit, impactPos) =>
                {
                    if (hit.team != owner.team)
                    {
                        int amt = Mathf.RoundToInt(secondaryDamage);
                        var dmg = new global::Damage(amt, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[{displayName}] Secondary hit on {hit.owner.name} for {amt} dmg");
                    }
                },
                secondaryLifetime);
        }
    }
}
