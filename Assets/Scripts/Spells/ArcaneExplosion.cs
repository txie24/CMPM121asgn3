using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ArcaneExplosion : Spell
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
    
    // 次级投射物属性
    private int secondaryProjectileCount;
    private float secondaryDamage;
    private float secondarySpeed;
    private float secondaryLifetime;
    private string secondaryTrajectory;
    private int secondaryProjectileSprite;

    public ArcaneExplosion(SpellCaster owner) : base(owner) { }

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
        
        // 次级投射物属性
        secondaryProjectileCount = j["N"] != null 
            ? Mathf.RoundToInt(RPNEvaluator.EvaluateFloat(j["N"].Value<string>(), vars)) 
            : 8;
            
        secondaryDamage = j["secondary_damage"] != null 
            ? RPNEvaluator.EvaluateFloat(j["secondary_damage"].Value<string>(), vars)
            : baseDamage * 0.25f;
            
        if (j["secondary_projectile"] != null)
        {
            secondaryTrajectory = j["secondary_projectile"]["trajectory"]?.Value<string>() ?? "straight";
            secondarySpeed = j["secondary_projectile"]["speed"] != null
                ? RPNEvaluator.EvaluateFloat(j["secondary_projectile"]["speed"].Value<string>(), vars)
                : baseSpeed * 0.8f;
                
            secondaryLifetime = j["secondary_projectile"]["lifetime"] != null
                ? float.Parse(j["secondary_projectile"]["lifetime"].Value<string>())
                : 0.3f;
                
            secondaryProjectileSprite = j["secondary_projectile"]["sprite"]?.Value<int>() ?? projectileSprite;
        }
        else
        {
            secondaryTrajectory = "straight";
            secondarySpeed = baseSpeed * 0.8f;
            secondaryLifetime = 0.3f;
            secondaryProjectileSprite = projectileSprite;
        }
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Cast() from {from} to {to} | speed={Speed}, damage={Damage}");

        // 创建主投射物，使用固定索引0避免越界
        GameManager.Instance.projectileManager.CreateProjectile(
            0, // 固定使用索引0
            trajectory,
            from,
            to - from,
            Speed,
            (hit, impactPos) =>
            {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(this.Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[{displayName}] Primary hit on {hit.owner.name} for {amount} damage");

                    SpawnSecondaryProjectiles(impactPos);
                }
            }
        );

        yield return null;
    }

    private void SpawnSecondaryProjectiles(Vector3 position)
    {
        float angleStep = 360f / secondaryProjectileCount;

        for (int i = 0; i < secondaryProjectileCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0
            ).normalized;

            // 使用固定索引0避免越界
            GameManager.Instance.projectileManager.CreateProjectile(
                0, // 固定使用索引0
                secondaryTrajectory,
                position,
                direction,
                secondarySpeed * Speed / BaseSpeed,
                (hit, impactPos) =>
                {
                    if (hit.team != owner.team)
                    {
                        int secondaryAmount = Mathf.RoundToInt(secondaryDamage * Damage / BaseDamage); 
                        var dmg = new global::Damage(secondaryAmount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[{displayName}] Secondary hit on {hit.owner.name} for {secondaryAmount} damage");
                    }
                },
                secondaryLifetime
            );
        }
    }
}