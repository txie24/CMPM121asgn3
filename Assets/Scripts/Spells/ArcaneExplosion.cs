using UnityEngine;
using System.Collections;

public sealed class ArcaneExplosion : Spell
{
    public ArcaneExplosion(SpellCaster owner) : base(owner) { }

    public override string DisplayName => "Arcane Explosion";
    public override int    IconIndex    => 3;

    protected override float BaseDamage   => 20f;
    protected override float BaseMana     => 15f;
    protected override float BaseCooldown => 1.5f;
    protected override float BaseSpeed    => 12f;

    private int SecondaryProjectileCount => 8; 
    private float SecondaryDamage => BaseDamage * 0.25f;  
    private float SecondarySpeed => BaseSpeed * 0.8f;  
    private float SecondaryLifetime => 0.3f;  

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[ArcaneExplosion] Cast() from {from} to {to} | speed={Speed}, damage={Damage}");

        // 创建主投射物
        GameManager.Instance.projectileManager.CreateProjectile(
            IconIndex,
            "straight",
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
                    Debug.Log($"[ArcaneExplosion] Primary hit on {hit.owner.name} for {amount} damage");

                    SpawnSecondaryProjectiles(impactPos);
                }
            }
        );

        yield return null;
    }

    private void SpawnSecondaryProjectiles(Vector3 position)
    {

        float angleStep = 360f / SecondaryProjectileCount;

        for (int i = 0; i < SecondaryProjectileCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0
            ).normalized;

            GameManager.Instance.projectileManager.CreateProjectile(
                IconIndex,
                "straight",
                position,
                direction,
                SecondarySpeed * Speed / BaseSpeed,
                (hit, impactPos) =>
                {
                    if (hit.team != owner.team)
                    {
                        int secondaryAmount = Mathf.RoundToInt(SecondaryDamage * Damage / BaseDamage); 
                        var dmg = new global::Damage(secondaryAmount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[ArcaneExplosion] Secondary hit on {hit.owner.name} for {secondaryAmount} damage");
                    }
                },
                SecondaryLifetime
            );
        }
    }
}