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
    
    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[Splitter] Casting in split directions with angle={angle}° and mana={Mana}");
        
        Vector3 direction = (to - from).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Add small random variation to each angle
        float randomVariation1 = Random.Range(-2f, 2f);
        float randomVariation2 = Random.Range(-2f, 2f);
        
        // Calculate direction vectors for the two split projectiles
        Vector3 dir1 = new Vector3(
            Mathf.Cos((baseAngle + angle + randomVariation1) * Mathf.Deg2Rad),
            Mathf.Sin((baseAngle + angle + randomVariation1) * Mathf.Deg2Rad),
            0).normalized;
        
        Vector3 dir2 = new Vector3(
            Mathf.Cos((baseAngle - angle + randomVariation2) * Mathf.Deg2Rad),
            Mathf.Sin((baseAngle - angle + randomVariation2) * Mathf.Deg2Rad),
            0).normalized;
        
        // Calculate target positions
        Vector3 target1 = from + dir1 * 10f;
        Vector3 target2 = from + dir2 * 10f;
        
        // Cast in both directions
        Debug.Log($"[Splitter] Casting first split direction at angle {baseAngle + angle + randomVariation1}°");
        yield return inner.TryCast(from, target1);
        
        Debug.Log($"[Splitter] Casting second split direction at angle {baseAngle - angle + randomVariation2}°");
        yield return inner.TryCast(from, target2);
    }
}