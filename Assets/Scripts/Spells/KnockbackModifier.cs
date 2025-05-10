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
        // 1) Parse JSON fields first
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        modifierDescription = j["description"]?.Value<string>() ?? modifierDescription;

        if (j["damage_multiplier"] != null)
            damageMultiplier = RPNEvaluator.SafeEvaluateFloat(
                j["damage_multiplier"].Value<string>(), vars, damageMultiplier);

        if (j["force"] != null)
            knockbackForce = RPNEvaluator.SafeEvaluateFloat(
                j["force"].Value<string>(), vars, knockbackForce);

        // 2) Then rebuild StatBlock.mods with updated multiplier
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Inject damage multiplier so inner damage is scaled
        mods.damage.Add(new ValueMod(ModOp.Mul, damageMultiplier));
    }

    protected override IEnumerator ApplyModifierEffect(Vector3 from, Vector3 to)
    {
        // Snapshot existing projectiles
        var before = Object
            .FindObjectsByType<ProjectileController>(FindObjectsSortMode.None)
            .ToList();

        // Cast the base spell + inner modifiers normally
        yield return inner.TryCast(from, to);

        // Find only the new projectiles spawned
        var after = Object.FindObjectsByType<ProjectileController>(FindObjectsSortMode.None);
        var newOnes = after.Except(before);

        // Attach knockback to each new projectile's OnHit
        foreach (var ctrl in newOnes)
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
                    }
                }
            };
        }
    }
}