// File: Assets/Scripts/Spells/ArcaneBolt.cs

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneBolt : Spell
{
    // all fields are populated from JSON
    string displayName;
    float baseDamage;
    Damage.Type damageType;
    float baseMana;
    float baseCooldown;
    float baseSpeed;
    int iconIndex;
    string trajectory;
    int projectileSprite;

    public ArcaneBolt(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int    IconIndex    => iconIndex;

    protected override float BaseDamage   => baseDamage;
    protected override float BaseMana     => baseMana;
    protected override float BaseCooldown => baseCooldown;
    protected override float BaseSpeed    => baseSpeed;

    /// <summary>
    /// Expects the JObject for "arcane_bolt" (i.e. root["arcane_bolt"]).
    /// </summary>
    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        // identity
        displayName = j["name"].Value<string>();
        iconIndex   = j["icon"].Value<int>();

        // damage & type
        baseDamage = RPNEvaluator.EvaluateFloat(
            j["damage"]["amount"].Value<string>(), vars);
        var dt = j["damage"]["type"].Value<string>();
        // parse enum (case‑insensitive)
        damageType = (Damage.Type)Enum.Parse(
            typeof(Damage.Type), dt, true);

        // mana + cooldown
        baseMana     = RPNEvaluator.EvaluateFloat(
            j["mana_cost"].Value<string>(), vars);
        baseCooldown = RPNEvaluator.EvaluateFloat(
            j["cooldown"].Value<string>(), vars);

        // projectile
        trajectory       = j["projectile"]["trajectory"].Value<string>();
        baseSpeed        = RPNEvaluator.EvaluateFloat(
            j["projectile"]["speed"].Value<string>(), vars);
        projectileSprite = j["projectile"]["sprite"].Value<int>();
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Casting ▶ dmg={Damage:F1} ({damageType}), mana={Mana:F1}, spd={Speed:F1}");

        GameManager.Instance.projectileManager.CreateProjectile(
            projectileSprite, // from JSON
            trajectory,       // from JSON
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
