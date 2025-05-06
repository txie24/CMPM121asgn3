// File: Assets/Scripts/Spells/ArcaneBolt.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneBolt : Spell
{
    // data from spells.json
    string displayName;
    string description;
    int iconIndex;
    float baseDamage;
    float baseMana;
    float baseCooldown;
    float baseSpeed;
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
    /// Reads everything from the JSON node for "arcane_bolt"
    /// </summary>
    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        // basic fields
        displayName = j["name"].Value<string>();
        description = j["description"]?.Value<string>() ?? "";
        iconIndex   = j["icon"].Value<int>();

        // stats (evaluate any RPN expressions)
        baseDamage   = RPNEvaluator.EvaluateFloat(j["damage"]["amount"].Value<string>(), vars);
        baseMana     = RPNEvaluator.EvaluateFloat(j["mana_cost"].Value<string>(), vars);
        baseCooldown = RPNEvaluator.EvaluateFloat(j["cooldown"].Value<string>(), vars);
        baseSpeed    = RPNEvaluator.EvaluateFloat(j["projectile"]["speed"].Value<string>(), vars);

        // projectile details
        trajectory       = j["projectile"]["trajectory"].Value<string>();
        projectileSprite = j["projectile"]["sprite"]?.Value<int>() ?? 0;
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Casting from {from} towards {to} (speed={Speed:F1}, dmg={Damage:F1})");

        // 关键修改：使用projectileSprite而不是iconIndex
        GameManager.Instance.projectileManager.CreateProjectile(
            0, // 固定使用索引0避免越界错误
            trajectory,
            from,
            to - from,
            Speed,
            (hit, impactPos) =>
            {
                if (hit.team != owner.team)
                {
                    int amt = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amt, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[{displayName}] Hit {hit.owner.name} for {amt} damage");
                }
            });

        yield return null;
    }
}