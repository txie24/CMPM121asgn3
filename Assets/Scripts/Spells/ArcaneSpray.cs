using UnityEngine;
using System.Collections;

public sealed class ArcaneSpray : Spell
{
    public ArcaneSpray(SpellCaster owner) : base(owner) { }

    public override string DisplayName => "Arcane Spray";
    public override int    IconIndex    => 1;

    protected override float BaseDamage   => 5f;
    protected override float BaseMana     => 15f; 
    protected override float BaseCooldown => 2f;
    protected override float BaseSpeed    => 15f; 

    private int ProjectileCount => 8;
    private float SpreadAngle => 60f;
    private float ProjectileLifetime => 0.5f; 

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[ArcaneSpray] Cast() from {from} to {to} | speed={Speed}, damage={Damage}");

        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float angleStep = SpreadAngle / (ProjectileCount - 1);
        float startAngle = baseAngle - SpreadAngle / 2;

        for (int i = 0; i < ProjectileCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            
            Vector3 projectileDirection = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                0
            );

            GameManager.Instance.projectileManager.CreateProjectile(
                IconIndex,
                "straight",
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
                        Debug.Log($"[ArcaneSpray] Hit {hit.owner.name} for {amount} damage");
                    }
                },
                ProjectileLifetime  
            );

            yield return new WaitForSeconds(0.02f);
        }

        yield return null;
    }
}