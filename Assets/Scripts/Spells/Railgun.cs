// File: Assets/Scripts/Spells/Railgun.cs
using UnityEngine;
using System.Collections;                // for IEnumerator
using System.Collections.Generic;       // for Dictionary<,>
using System.Linq;                      // for Except()
using Newtonsoft.Json.Linq;             // for JObject

public sealed class Railgun : Spell
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
    private string lifetimeExpr;

    public Railgun(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int IconIndex => iconIndex;

    protected override float BaseDamage => RPNEvaluator.SafeEvaluateFloat(
        damageExpr,
        new Dictionary<string, float> {
            { "power", owner.spellPower },
            { "wave",  GetCurrentWave()    }
        },
        50f);

    protected override float BaseSpeed => RPNEvaluator.SafeEvaluateFloat(
        speedExpr,
        new Dictionary<string, float> {
            { "power", owner.spellPower },
            { "wave",  GetCurrentWave()    }
        },
        25f);

    protected override float BaseMana => baseMana;
    protected override float BaseCooldown => baseCooldown;

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
        baseMana = RPNEvaluator.SafeEvaluateFloat(
                               j["mana_cost"].Value<string>(),
                               vars,
                               10f);
        baseCooldown = RPNEvaluator.SafeEvaluateFloat(
                               j["cooldown"].Value<string>(),
                               vars,
                               3f);

        trajectory = j["projectile"]["trajectory"].Value<string>();
        projectileSprite = j["projectile"]["sprite"]?.Value<int>() ?? 0;
        lifetimeExpr = j["projectile"]["lifetime"]?.Value<string>() ?? "1";
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 1) Capture final damage, speed, and lifetime
        float dmg = Damage;
        float spd = Speed;
        float lifetime = RPNEvaluator.SafeEvaluateFloat(
            lifetimeExpr,
            new Dictionary<string, float> {
                { "power", owner.spellPower },
                { "wave",  GetCurrentWave()    }
            },
            1f
        );

        Debug.Log(
            $"[{displayName}] Casting ▶ dmg={dmg:F1}, spd={spd:F1}, " +
            $"lifetime={lifetime:F1}, cooldown={Cooldown:F1}"
        );

        Vector3 direction = (to - from).normalized;

        // 2) Snapshot existing projectiles
        var before = Object.FindObjectsByType<ProjectileController>(
            FindObjectsSortMode.None
        ).ToList();

        // 3) Fire a piercing shot
        GameManager.Instance.projectileManager.CreatePiercingProjectile(
            projectileSprite,
            trajectory,
            from,
            direction,
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
            }
        );

        // 4) Apply lifetime to the new projectile
        var after = Object.FindObjectsByType<ProjectileController>(
            FindObjectsSortMode.None
        );
        foreach (var ctrl in after.Except(before))
        {
            ctrl.SetLifetime(lifetime);
        }

        yield return null;
    }
}
