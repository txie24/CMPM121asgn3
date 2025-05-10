using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public sealed class KnockbackModifier : ModifierSpell
{
    private float damageMultiplier = 0.5f;
    private float knockbackForce = 20f;
    private string modifierName = "knockback";
    private string modifierDescription = "Adds a knockback effect.";

    public KnockbackModifier(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[KnockbackModifier] Loading attributes from JSON");

        // Load name and description
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        // Load damage multiplier
        if (j["damage_multiplier"] != null)
        {
            string expr = j["damage_multiplier"].Value<string>();
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, damageMultiplier);
            Debug.Log($"[KnockbackModifier] Loaded damage_multiplier={damageMultiplier} from expression '{expr}'");
        }

        // Load knockback force
        if (j["force"] != null)
        {
            string expr = j["force"].Value<string>();
            knockbackForce = RPNEvaluator.SafeEvaluateFloat(expr, vars, knockbackForce);
            Debug.Log($"[KnockbackModifier] Loaded force={knockbackForce} from expression '{expr}'");
        }

        // Register our mods
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[KnockbackModifier] Injecting damage×{damageMultiplier}");
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[KnockbackModifier] Casting spell with scaled damage={Damage:F1} and force={knockbackForce:F1}");

        // 1) Snapshot existing projectiles
        var before = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).ToList();

        // 2) Save and swap in our modified stats so Damage uses the multiplier
        var originalInnerMods = inner.mods;
        inner.mods = this.mods;

        // 3) Cast the inner spell (base + other modifiers) with scaled damage
        yield return inner.TryCast(from, to);

        // 4) Restore original stats
        inner.mods = originalInnerMods;

        // 5) Find only the new projectiles and attach knockback
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
