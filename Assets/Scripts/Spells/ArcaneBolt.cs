// File: Assets/Scripts/Spells/ArcaneBolt.cs

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneBolt : Spell
{
    private string displayName;
    private Damage.Type damageType;
    private float baseMana;
    private float baseCooldown;
    private int iconIndex;
    private string trajectory;
    private int projectileSprite;

    // RPN expressions for dynamic scaling
    private string damageExpr;
    private string speedExpr;

    public ArcaneBolt(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int IconIndex => iconIndex;

    protected override float BaseDamage => RPNEvaluator.SafeEvaluateFloat(
        damageExpr,
        new Dictionary<string, float> {
            { "power", owner.spellPower },
            { "wave", GetCurrentWave() }
        },
        10f);

    protected override float BaseSpeed => RPNEvaluator.SafeEvaluateFloat(
        speedExpr,
        new Dictionary<string, float> {
            { "power", owner.spellPower },
            { "wave", GetCurrentWave() }
        },
        8f);

    protected override float BaseMana => baseMana;
    protected override float BaseCooldown => baseCooldown;

    private float GetCurrentWave()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        return spawner != null ? spawner.currentWave : 1;
    }

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        displayName = j["name"].Value<string>();
        iconIndex = j["icon"].Value<int>();

        // Save RPN expressions
        damageExpr = j["damage"]["amount"].Value<string>();
        var dt = j["damage"]["type"].Value<string>();
        damageType = (Damage.Type)Enum.Parse(typeof(Damage.Type), dt, true);

        baseMana = RPNEvaluator.SafeEvaluateFloat(j["mana_cost"].Value<string>(), vars, 1f);
        baseCooldown = RPNEvaluator.SafeEvaluateFloat(j["cooldown"].Value<string>(), vars, 0f);

        trajectory = j["projectile"]["trajectory"].Value<string>();
        speedExpr = j["projectile"]["speed"].Value<string>();
        projectileSprite = j["projectile"]["sprite"].Value<int>();
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        // 1) Capture the *final* damage & speed at cast time
        float dmg = Damage;
        float spd = Speed;

        //Debug.Log($"[{displayName}] Casting ▶ dmg={dmg:F1} ({damageType}), mana={Mana:F1}, spd={spd:F1}");

        // 2) Fire the projectile using captured values
        GameManager.Instance.projectileManager.CreateProjectile(
            projectileSprite,
            trajectory,
            from,
            to - from,
            spd,
            (hit, impactPos) =>
            {
                if (hit.team != owner.team)
                {
                    int amt = Mathf.RoundToInt(dmg);
                    var dmgObj = new global::Damage(amt, damageType);
                    hit.Damage(dmgObj);
                }
            });

        yield return null;
    }
}
