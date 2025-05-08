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
    
    // Override CastWithModifiers to implement the splitting behavior
    protected override IEnumerator CastWithModifiers(Vector3 from, Vector3 to)
    {
        Debug.Log($"[Splitter] Splitting {inner.DisplayName} into two directions with angle={angle}°");
        
        // Calculate the base direction vector and angle
        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Add small random variation to each angle to make it look more natural
        float randomVariation1 = Random.Range(-2f, 2f);
        float randomVariation2 = Random.Range(-2f, 2f);
        
        // Calculate the modified angles for the two split directions
        float angle1 = baseAngle + angle + randomVariation1;
        float angle2 = baseAngle - angle + randomVariation2;
        
        // Convert angles to direction vectors
        Vector3 dir1 = new Vector3(
            Mathf.Cos(angle1 * Mathf.Deg2Rad),
            Mathf.Sin(angle1 * Mathf.Deg2Rad),
            0).normalized;
        
        Vector3 dir2 = new Vector3(
            Mathf.Cos(angle2 * Mathf.Deg2Rad),
            Mathf.Sin(angle2 * Mathf.Deg2Rad),
            0).normalized;
        
        // Calculate target positions at same distance as original
        float distance = Vector3.Distance(from, to);
        Vector3 target1 = from + dir1 * distance;
        Vector3 target2 = from + dir2 * distance;
        
        // Log what we're doing
        Debug.Log($"[Splitter] Casting {inner.DisplayName} in first direction (angle: {angle1:F1}°)");
        
        // Cast in first direction
        yield return inner.TryCast(from, target1);
        
        // Small delay to ensure the first cast has started
        yield return new WaitForSeconds(0.05f);
        
        // Log second cast
        Debug.Log($"[Splitter] Casting {inner.DisplayName} in second direction (angle: {angle2:F1}°)");
        
        // Cast in second direction
        yield return inner.TryCast(from, target2);
    }
}