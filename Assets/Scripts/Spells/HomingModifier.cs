using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class HomingModifier : ModifierSpell
{
    private float damageMultiplier = 0.75f;
    private float manaAdder = 10f;
    private string modifierName = "homing";

    public HomingModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[HomingModifier] Loading attributes from JSON");

        // Load name
        modifierName = j["name"]?.Value<string>() ?? "homing";

        // Load damage multiplier using RPN
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 0.75f);
            Debug.Log($"[HomingModifier] Loaded damage_multiplier={damageMultiplier} from expression '{expr}'");
        }

        // Load mana adder using RPN
        if (j["mana_adder"] != null)
        {
            string expr = j["mana_adder"].Value<string>();
            manaAdder = RPNEvaluator.SafeEvaluateFloat(expr, vars, 10f);
            Debug.Log($"[HomingModifier] Loaded mana_adder={manaAdder} from expression '{expr}'");
        }

        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[HomingModifier] Injecting mods: damage×{damageMultiplier}, mana+{manaAdder}");
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
        mods.mana.Add(new ValueMod(ModOp.Add, manaAdder));
    }

    protected override IEnumerator ApplyModifierEffect(Vector3 from, Vector3 to)
    {
        Debug.Log($"[HomingModifier] Adding homing effect to {inner.DisplayName}");

        // Handle different spell types appropriately
        if (inner is ArcaneSpray spraySpell)
        {
            yield return CreateHomingSpray(from, to);
        }
        else if (inner is ArcaneBlast blastSpell)
        {
            yield return CreateHomingBlast(from, to);
        }
        else
        {
            // For simpler spells, just create a homing projectile
            yield return CreateGenericHoming(from, to);
        }
    }

    private IEnumerator CreateHomingSpray(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Estimate spray parameters
        int projectileCount = Mathf.RoundToInt(inner.Damage) + 5; // Approximate based on damage
        float sprayAngle = 60f;
        float angleStep = sprayAngle / (projectileCount - 1);
        float startAngle = baseAngle - sprayAngle / 2;

        // Create homing projectiles in a spray pattern
        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector3 projectileDirection = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                0);

            // Find nearby enemy for better initial targeting
            GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
            Vector3 targetDirection = closestEnemy != null
                ? (closestEnemy.transform.position - from).normalized
                : projectileDirection;

            GameManager.Instance.projectileManager.CreateProjectile(
                inner.IconIndex,
                "homing", // Use homing trajectory
                from,
                targetDirection,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                    }
                },
                0.1f + inner.Speed / 40f // Approximate lifetime
            );

            yield return new WaitForSeconds(0.02f);
        }
    }

    private IEnumerator CreateHomingBlast(Vector3 from, Vector3 to)
    {
        // Find closest enemy for initial targeting
        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetDirection = closestEnemy != null
            ? (closestEnemy.transform.position - from).normalized
            : (to - from).normalized;

        // Create primary homing projectile
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "homing",
            from,
            targetDirection,
            inner.Speed,
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);

                    // Create secondary explosion with homing projectiles
                    CreateHomingSecondaryExplosion(impactPos, amount / 4);
                }
            }
        );

        yield return null;
    }

    private void CreateHomingSecondaryExplosion(Vector3 position, int damage)
    {
        int projectileCount = 8; // Default from ArcaneBlast
        float angleStep = 360f / projectileCount;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad),
                0).normalized;

            GameManager.Instance.projectileManager.CreateProjectile(
                inner.IconIndex,
                "homing", // Use homing for secondary projectiles
                position,
                direction,
                inner.Speed * 0.8f,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        var dmg = new global::Damage(damage, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                    }
                },
                0.3f // Default secondary lifetime
            );
        }
    }

    private IEnumerator CreateGenericHoming(Vector3 from, Vector3 to)
    {
        // Find closest enemy for better targeting
        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(from);
        Vector3 targetPos = closestEnemy != null ? closestEnemy.transform.position : to;
        Vector3 direction = (targetPos - from).normalized;

        // Create a single homing projectile
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "homing",
            from,
            direction,
            inner.Speed,
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[HomingModifier] Hit {hit.owner.name} for {amount} damage");
                }
            }
        );

        yield return null;
    }
}