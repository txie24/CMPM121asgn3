using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Splitter : ModifierSpell
{
    private float angle = 10f;
    private float manaMultiplier = 1.5f;
    private string modifierName = "split";

    public Splitter(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        Debug.Log("[Splitter] Loading attributes from JSON");

        // Load name
        modifierName = j["name"]?.Value<string>() ?? "split";

        // Load angle using RPN
        if (j["angle"] != null)
        {
            string expr = j["angle"].Value<string>();
            angle = RPNEvaluator.SafeEvaluateFloat(expr, vars, 10f);
            Debug.Log($"[Splitter] Loaded angle={angle} from expression '{expr}'");
        }

        // Load mana multiplier using RPN
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.SafeEvaluateFloat(expr, vars, 1.5f);
            Debug.Log($"[Splitter] Loaded mana_multiplier={manaMultiplier} from expression '{expr}'");
        }

        // Call base class to update modifiers
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        Debug.Log($"[Splitter] Injecting mods: mana×{manaMultiplier}");
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
    }

    protected override IEnumerator ApplyModifierEffect(Vector3 from, Vector3 to)
    {
        Debug.Log($"[Splitter] Applying split effect to {inner.DisplayName}");

        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Add small random variation to angles
        float randomVariation1 = Random.Range(-2f, 2f);
        float randomVariation2 = Random.Range(-2f, 2f);

        // Calculate angles for split directions
        float leftAngle = baseAngle + angle + randomVariation1;
        float rightAngle = baseAngle - angle + randomVariation2;

        // Calculate direction vectors
        Vector3 leftDirection = new Vector3(
            Mathf.Cos(leftAngle * Mathf.Deg2Rad),
            Mathf.Sin(leftAngle * Mathf.Deg2Rad),
            0).normalized;

        Vector3 rightDirection = new Vector3(
            Mathf.Cos(rightAngle * Mathf.Deg2Rad),
            Mathf.Sin(rightAngle * Mathf.Deg2Rad),
            0).normalized;

        // Calculate target positions
        Vector3 leftTarget = from + leftDirection * 10f;
        Vector3 rightTarget = from + rightDirection * 10f;

        // Cast in split directions - preserving inner spell's behavior in each direction
        Debug.Log($"[Splitter] Casting first split direction at angle {leftAngle}°");
        yield return CastInDirection(from, leftTarget);

        Debug.Log($"[Splitter] Casting second split direction at angle {rightAngle}°");
        yield return CastInDirection(from, rightTarget);
    }

    private IEnumerator CastInDirection(Vector3 from, Vector3 direction)
    {
        if (inner is ArcaneSpray)
        {
            // For spray, calculate the offset spray cone direction
            yield return inner.TryCast(from, direction);
        }
        else if (inner is ArcaneBlast)
        {
            // For blast, we just need to change the primary projectile direction
            yield return inner.TryCast(from, direction);
        }
        else
        {
            // For other spells, just use the direction
            yield return inner.TryCast(from, direction);
        }
    }
}