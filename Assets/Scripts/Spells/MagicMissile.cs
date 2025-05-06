using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class MagicMissile : Spell
{
    // 从JSON加载的属性
    private string displayName;
    private string description;
    private int iconIndex;
    private float baseDamage;
    private float baseMana;
    private float baseCooldown;
    private float baseSpeed;
    private string trajectory;
    private int projectileSprite;
    //private float turnRate = 0.25f;

    public MagicMissile(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int    IconIndex    => iconIndex;

    protected override float BaseDamage   => baseDamage;
    protected override float BaseMana     => baseMana;
    protected override float BaseCooldown => baseCooldown;
    protected override float BaseSpeed    => baseSpeed;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        // 基本属性
        displayName = j["name"].Value<string>();
        description = j["description"]?.Value<string>() ?? "";
        iconIndex   = j["icon"].Value<int>();

        // 基础数值
        baseDamage   = RPNEvaluator.EvaluateFloat(j["damage"]["amount"].Value<string>(), vars);
        baseMana     = RPNEvaluator.EvaluateFloat(j["mana_cost"].Value<string>(), vars);
        baseCooldown = RPNEvaluator.EvaluateFloat(j["cooldown"].Value<string>(), vars);
        baseSpeed    = RPNEvaluator.EvaluateFloat(j["projectile"]["speed"].Value<string>(), vars);

        // 投射物属性
        trajectory       = j["projectile"]["trajectory"].Value<string>();
        projectileSprite = j["projectile"]["sprite"]?.Value<int>() ?? 0;
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Cast() from {from} to {to} | speed={Speed}, damage={Damage}");

        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetDirection = closestEnemy != null 
            ? (closestEnemy.transform.position - from).normalized 
            : (to - from).normalized;

        // 使用固定索引0避免越界
        GameManager.Instance.projectileManager.CreateProjectile(
            0, // 固定使用索引0
            trajectory, // 使用从JSON加载的轨迹类型
            from,
            targetDirection,
            Speed,
            (hit, impactPos) =>
            {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(this.Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[{displayName}] Hit {hit.owner.name} for {amount} damage");
                }
            }
        );

        yield return null;
    }
}