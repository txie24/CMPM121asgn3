using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ChaoticModifier : ModifierSpell
{
    private float damageMultiplier = 1.5f;
    private string modifierName = "chaotic";

    public ChaoticModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[ChaoticModifier] Loading attributes from JSON");

        // Load name
        modifierName = j["name"]?.Value<string>() ?? "chaotic";

        // Load damage multiplier using RPN
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
            Debug.Log($"[ChaoticModifier] Loaded damage_multiplier={damageMultiplier} from expression '{expr}'");
        }

        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[ChaoticModifier] Injecting mods: damage×{damageMultiplier}");
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }

    protected override IEnumerator ApplyModifierEffect(Vector3 from, Vector3 to)
    {
        Debug.Log($"[ChaoticModifier] Adding spiraling effect to {inner.DisplayName}");

        // Handle ArcaneSpray specifically
        if (inner is ArcaneSpray spraySpell)
        {
            // Create a wrapper for the hit callback to apply spiraling trajectory to each projectile
            yield return CreateSpiralingSpray(from, to);
        }
        else if (inner is ArcaneBlast blastSpell)
        {
            // Handle ArcaneBlast by adding spiraling to primary and secondary projectiles
            yield return CreateSpiralingBlast(from, to);
        }
        else
        {
            // For other spells, create a spiraling projectile that preserves the inner spell's damage
            yield return CreateGenericSpiraling(from, to);
        }
    }

    private IEnumerator CreateSpiralingSpray(Vector3 from, Vector3 to)
    {
        // Extract needed parameters from ArcaneSpray (approximate)
        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Estimate number of projectiles from damage (assuming original design)
        int projectileCount = Mathf.RoundToInt(inner.Damage) + 5; // Rough approximation
        float sprayAngle = 60f; // Default spray angle
        float angleStep = sprayAngle / (projectileCount - 1);
        float startAngle = baseAngle - sprayAngle / 2;

        // Create spiraling projectiles in a spray pattern
        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + i * angleStep;
            Vector3 projectileDirection = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                0);

            GameManager.Instance.projectileManager.CreateProjectile(
                inner.IconIndex,
                "spiraling", // Use spiraling trajectory for each projectile
                from,
                projectileDirection,
                inner.Speed,
                (hit, impactPos) => {
                    if (hit.team != owner.team)
                    {
                        int amount = Mathf.RoundToInt(Damage);
                        var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                        hit.Damage(dmg);
                        Debug.Log($"[ChaoticModifier] Spray hit {hit.owner.name} for {amount} damage");
                    }
                },
                0.1f + inner.Speed / 40f // Approximate lifetime based on speed
            );

            yield return new WaitForSeconds(0.02f);
        }
    }

    private IEnumerator CreateSpiralingBlast(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;

        // Create primary projectile with spiraling
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "spiraling",
            from,
            direction,
            inner.Speed,
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);

                    // Create secondary explosion with spiraling projectiles
                    CreateSpiralingSecondaryExplosion(impactPos, amount / 4);
                }
            }
        );

        yield return null;
    }

    private void CreateSpiralingSecondaryExplosion(Vector3 position, int damage)
    {
        // Create secondary projectiles in a circular pattern
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
                "spiraling", // Use spiraling for secondary projectiles too
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

    private IEnumerator CreateGenericSpiraling(Vector3 from, Vector3 to)
    {
        // For other spell types, create a basic spiraling projectile
        GameManager.Instance.projectileManager.CreateProjectile(
            inner.IconIndex,
            "spiraling",
            from,
            (to - from).normalized,
            inner.Speed,
            (hit, impactPos) => {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[ChaoticModifier] Hit {hit.owner.name} for {amount} damage");
                }
            }
        );

        yield return null;
    }
}