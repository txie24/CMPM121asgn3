// File: Assets/Scripts/Spells/ArcaneBolt.cs

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneBolt : Spell
{
    string displayName;
    Damage.Type damageType;
    float baseMana;
    float baseCooldown;
    int iconIndex;
    string trajectory;
    int projectileSprite;

    // RPN expressions for dynamic scaling
    string damageExpr;
    string speedExpr;

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

    float GetCurrentWave()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        return spawner != null ? spawner.currentWave : 1;
    }

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        displayName = j["name"].Value<string>();
        iconIndex = j["icon"].Value<int>();

        // Save damage expression for dynamic evaluation
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
        Debug.Log($"[{displayName}] Casting ▶ dmg={Damage:F1} ({damageType}), mana={Mana:F1}, spd={Speed:F1}");

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
                    int amt = Mathf.RoundToInt(this.Damage);
                    var dmg = new global::Damage(amt, damageType);
                    hit.Damage(dmg);
                    Debug.Log($"[{displayName}] Hit {hit.owner.name} for {amt} ({damageType})");
                }
            });

        yield return null;
    }
}
