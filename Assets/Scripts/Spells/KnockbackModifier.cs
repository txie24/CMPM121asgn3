using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public sealed class KnockbackModifier : ModifierSpell
{
    private float knockbackForce = 20f;
    private string modifierName = "knockback";
    private string modifierDescription = "Adds a knockback effect to the spell.";

    public KnockbackModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[KnockbackModifier] Loading attributes from JSON");

        // Load name and description
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        // Load knockback force
        if (j["force"] != null)
        {
            string expr = j["force"].Value<string>();
            knockbackForce = RPNEvaluator.SafeEvaluateFloat(expr, vars, knockbackForce);
            Debug.Log($"[KnockbackModifier] Loaded force={knockbackForce} from expression '{expr}'");
        }

        // Register our mods (none for damage)
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // No stat modifications for knockback
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[KnockbackModifier] Casting spell with force={knockbackForce:F1}");

        // Snapshot existing projectiles
        var before = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).ToList();

        // Cast the inner spell normally
        yield return inner.TryCast(from, to);

        // Attach knockback to each new projectile's OnHit
        var after = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None);
        foreach (var ctrl in after.Except(before))
        {
            ctrl.OnHit += (hit, impactPos) =>
            {
                if (hit.team != owner.team)
                {
                    var rb = hit.owner.GetComponent<Rigidbody2D>();
                    if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                    {
                        Vector2 dir = (hit.owner.transform.position - impactPos).normalized;
                        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                        Debug.Log($"[KnockbackModifier] Knocked back {hit.owner.name} with strength {knockbackForce:F1}");
                    }
                }
            };
        }
    }
}
