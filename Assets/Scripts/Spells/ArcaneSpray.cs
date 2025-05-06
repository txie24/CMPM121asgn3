using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneSpray : Spell
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
    private int projectileCount;
    private float sprayAngle;
    private float projectileLifetime;

    public ArcaneSpray(SpellCaster owner) : base(owner) { }

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
        
        // 特有属性
        projectileCount = j["N"] != null 
            ? Mathf.RoundToInt(RPNEvaluator.EvaluateFloat(j["N"].Value<string>(), vars)) 
            : 7;
        
        sprayAngle = j["spray"] != null 
            ? float.Parse(j["spray"].Value<string>()) * 180f  // 将值转换为角度
            : 60f;
        
        projectileLifetime = j["projectile"]["lifetime"] != null 
            ? RPNEvaluator.EvaluateFloat(j["projectile"]["lifetime"].Value<string>(), vars) 
            : 0.5f;
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Cast() from {from} to {to} | speed={Speed}, damage={Damage}");

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
                0
            );

            // 使用固定索引0避免越界
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // 固定使用索引0
                trajectory,
                from,
                projectileDirection,
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
                },
                projectileLifetime
            );

            yield return new WaitForSeconds(0.02f);
        }

        yield return null;
    }
}