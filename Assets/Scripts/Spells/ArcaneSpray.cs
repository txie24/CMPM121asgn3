// File: Assets/Scripts/Spells/ArcaneSpray.cs

using UnityEngine;
using System.Collections;                // for IEnumerator
using System.Collections.Generic;       // for Dictionary<,>
using Newtonsoft.Json.Linq;             // for JObject

public sealed class ArcaneSpray : Spell
{
    // JSON‐loaded fields
    private string displayName;
    private string description;
    private int iconIndex;
    private float baseMana;
    private float baseCooldown;
    private string trajectory;
    private int projectileSprite;

    private string damageExpr;
    private string speedExpr;
    private string lifetimeExpr;
    private string projectileCountExpr;

    private float sprayAngle;

    public ArcaneSpray(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int IconIndex => iconIndex;

    protected override float BaseDamage => RPNEvaluator.SafeEvaluateFloat(
        damageExpr, GetVars(), 3f);
    protected override float BaseSpeed => RPNEvaluator.SafeEvaluateFloat(
        speedExpr, GetVars(), 8f);
    protected override float BaseMana => baseMana;
    protected override float BaseCooldown => baseCooldown;

    private Dictionary<string, float> GetVars() => new()
    {
        { "power", owner.spellPower },
        { "wave",  GetCurrentWave()    }
    };

    private float GetCurrentWave()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        return spawner != null ? spawner.currentWave : 1;
    }

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        // 1) Identity
        displayName = j["name"].Value<string>();
        description = j["description"]?.Value<string>() ?? "";
        iconIndex = j["icon"].Value<int>();

        // 2) Stat expressions
        damageExpr = j["damage"]["amount"].Value<string>();
        speedExpr = j["projectile"]["speed"].Value<string>();
        lifetimeExpr = j["projectile"]["lifetime"].Value<string>();
        projectileCountExpr = j["N"]?.Value<string>() ?? "7";

        // 3) Mana & cooldown
        baseMana = RPNEvaluator.SafeEvaluateFloat(
                                   j["mana_cost"].Value<string>(),
                                   vars,
                                   1f);
        baseCooldown = RPNEvaluator.SafeEvaluateFloat(
                                   j["cooldown"].Value<string>(),
                                   vars,
                                   0.5f);

        // 4) Projectile visuals & behavior
        trajectory = j["projectile"]["trajectory"].Value<string>();
        projectileSprite = j["projectile"]["sprite"]?.Value<int>() ?? 0;
        sprayAngle = j["spray"] != null
                                 ? float.Parse(j["spray"].Value<string>()) * 180f
                                 : 60f;
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // evaluate count & lifetime under current mods
        int projectileCount = Mathf.RoundToInt(
                                   RPNEvaluator.SafeEvaluateFloat(
                                     projectileCountExpr,
                                     GetVars(),
                                     7f));
        float lifetime = RPNEvaluator.SafeEvaluateFloat(
                                   lifetimeExpr,
                                   GetVars(),
                                   0.5f);

        // 1) capture stats once so modifiers stick
        float dmg = Damage;
        float spd = Speed;

        Debug.Log(
          $"[{displayName}] Cast ▶ dmg={dmg:F1}, spd={spd:F1}, " +
          $"lifetime={lifetime:F2}, count={projectileCount}"
        );

        // 2) spray loop
        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float angleStep = sprayAngle / (projectileCount - 1);
        float startAngle = baseAngle - sprayAngle / 2;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector3 projectileDir = new Vector3(
                                         Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                                         Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                                         0f
                                       );

            GameManager.Instance.projectileManager.CreateProjectile(
                projectileSprite,
                trajectory,
                from,
                projectileDir,
                spd,
                (hit, impactPos) =>
                {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(dmg);
                        var dmgObj = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmgObj);
                        Debug.Log($"[{displayName}] Hit {hit.owner.name} for {amount} dmg");
                    }
                },
                lifetime
            );

            yield return new WaitForSeconds(0.02f);
        }

        yield return null;
    }
}
