// File: Assets/Scripts/Spells/MagicMissile.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class MagicMissile : Spell
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

    public MagicMissile(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int IconIndex => iconIndex;

    protected override float BaseDamage {
        get {
            float pw = owner.spellPower;
            float wv = GetCurrentWave();
            float dmg = RPNEvaluator.SafeEvaluateFloat(
                damageExpr,
                new Dictionary<string, float> {
                    { "power", pw },
                    { "wave", wv }
                },
                10f);
            Debug.Log($"[MagicMissile] Damage Scaling ▶ power={pw}, wave={wv}, damage={dmg:F2}");
            return dmg;
        }
    }

    protected override float BaseSpeed => RPNEvaluator.SafeEvaluateFloat(
        speedExpr,
        new Dictionary<string, float> {
            { "power", owner.spellPower },
            { "wave", GetCurrentWave() }
        },
        10f);

    protected override float BaseMana => baseMana;
    protected override float BaseCooldown => baseCooldown;

    float GetCurrentWave()
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
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Casting ▶ dmg={Damage:F1}, spd={Speed:F1}");

        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetDirection = closestEnemy != null 
            ? (closestEnemy.transform.position - from).normalized 
            : (to - from).normalized;

        GameManager.Instance.projectileManager.CreateProjectile(
            projectileSprite,
            trajectory,
            from,
            targetDirection,
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
            });

        yield return null;
    }
}
