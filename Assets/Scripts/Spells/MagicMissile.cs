using UnityEngine;
using System.Collections;

public sealed class MagicMissile : Spell
{
    public MagicMissile(SpellCaster owner) : base(owner) { }

    public override string DisplayName => "Magic Missile";
    public override int    IconIndex    => 2;

    protected override float BaseDamage   => 8f;
    protected override float BaseMana     => 10f;
    protected override float BaseCooldown => 1.2f; 
    protected override float BaseSpeed    => 8f; 
    
    private float TurnRate => 0.25f; 

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[MagicMissile] Cast() from {from} to {to} | speed={Speed}, damage={Damage}");

        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetDirection = closestEnemy != null 
            ? (closestEnemy.transform.position - from).normalized 
            : (to - from).normalized;

        GameManager.Instance.projectileManager.CreateProjectile(
            IconIndex,
            "homing", 
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
                    Debug.Log($"[MagicMissile] Hit {hit.owner.name} for {amount} damage");
                }
            }
        );

        yield return null;
    }
}