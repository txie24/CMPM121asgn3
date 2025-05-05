using UnityEngine;
using System.Collections;

public sealed class ArcaneBolt : Spell
{
    public ArcaneBolt(SpellCaster owner) : base(owner) { }

    public override string DisplayName => "Arcane Bolt";
    public override int    IconIndex    => 0;

    protected override float BaseDamage   => 20f;
    protected override float BaseMana     => 8f;
    protected override float BaseCooldown => 1.5f;
    protected override float BaseSpeed    => 12f;

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[ArcaneBolt] Cast() from {from} to {to} | speed={Speed}, damage={Damage}");

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
                    Debug.Log($"[ArcaneBolt] Hit {hit.owner.name} for {amount} damage");
                }
            });

        yield return null;
    }
}
