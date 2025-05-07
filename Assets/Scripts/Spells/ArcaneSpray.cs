// File: Assets/Scripts/Spells/ArcaneSpray.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneSpray : Spell
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
    private string projectileCountExpr;

    private float sprayAngle;

    public ArcaneSpray(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int IconIndex => iconIndex;

    protected override float BaseDamage => RPNEvaluator.SafeEvaluateFloat(
        damageExpr,
        GetVars(),
        3f);

    protected override float BaseSpeed => RPNEvaluator.SafeEvaluateFloat(
        speedExpr,
        GetVars(),
        8f);

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
        lifetimeExpr = j["projectile"]["lifetime"].Value<string>();
        projectileCountExpr = j["N"]?.Value<string>();

        baseMana = RPNEvaluator.SafeEvaluateFloat(j["mana_cost"].Value<string>(), vars, 1f);
        baseCooldown = RPNEvaluator.SafeEvaluateFloat(j["cooldown"].Value<string>(), vars, 0.5f);

        trajectory = j["projectile"]["trajectory"].Value<string>();
        projectileSprite = j["projectile"]["sprite"]?.Value<int>() ?? 0;

        sprayAngle = j["spray"] != null 
            ? float.Parse(j["spray"].Value<string>()) * 180f 
            : 60f;
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        int projectileCount = Mathf.RoundToInt(RPNEvaluator.SafeEvaluateFloat(projectileCountExpr, GetVars(), 7));
        float lifetime = RPNEvaluator.SafeEvaluateFloat(lifetimeExpr, GetVars(), 0.5f);

        Debug.Log($"[{displayName}] Cast() ▶ dmg={Damage:F1}, spd={Speed:F1}, lifetime={lifetime}, count={projectileCount}");

        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float angleStep = sprayAngle / (projectileCount - 1);
        float startAngle = baseAngle - sprayAngle / 2;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector3 projectileDirection = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                0);

            GameManager.Instance.projectileManager.CreateProjectile(
                projectileSprite,
                trajectory,
                from,
                projectileDirection,
                Speed,
                (hit, impactPos) =>
                {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[{displayName}] Hit {hit.owner.name} for {amount} dmg");
                    }
                },
                lifetime);

            yield return new WaitForSeconds(0.02f);
        }

        yield return null;
    }
}
