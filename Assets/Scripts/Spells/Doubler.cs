using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Doubler : ModifierSpell
{
    private float delay = 0.5f;
    private float manaMultiplier = 1.5f;
    private float cooldownMultiplier = 1.5f;
    private string modifierName = "doubled";
    private string modifierDescription = "Spell is cast a second time after a small delay; increased mana cost and cooldown.";
    
    public Doubler(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string,float> vars)
    {
        base.LoadAttributes(j, vars);
        
        modifierName = j["name"]?.Value<string>() ?? "doubled";
        modifierDescription = j["description"]?.Value<string>() ?? "Spell is cast a second time after a small delay; increased mana cost and cooldown.";
        
        if (j["delay"] != null)
        {
            string expr = j["delay"].Value<string>();
            delay = float.Parse(expr);
        }
        
        if (j["mana_multiplier"] != null)
        {
            string expr = j["mana_multiplier"].Value<string>();
            manaMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
        
        if (j["cooldown_multiplier"] != null)
        {
            string expr = j["cooldown_multiplier"].Value<string>();
            cooldownMultiplier = RPNEvaluator.EvaluateFloat(expr, vars);
        }
    }

    protected override void InjectMods(StatBlock mods)
    {
        // Increase mana cost and cooldown
        mods.mana.Add(new ValueMod(ModOp.Mul, manaMultiplier));
        mods.cd.Add(new ValueMod(ModOp.Mul, cooldownMultiplier));
    }

    protected override IEnumerator ModifierCast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[Doubler] First cast of {inner.DisplayName}");
        
        // First cast - respect the inner spell's behavior
        if (inner is ChaoticModifier || inner is HomingModifier || inner is Splitter)
        {
            // Let the inner modifier handle the first cast with its special behavior
            yield return inner.TryCast(from, to);
        }
        else
        {
            // For regular spells
            yield return inner.TryCast(from, to);
        }
        
        // Small delay between casts
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"[Doubler] Second cast of {inner.DisplayName} after {delay}s delay");
        
        // Second cast - repeat with the same handling
        if (inner is ChaoticModifier || inner is HomingModifier || inner is Splitter)
        {
            // Let the inner modifier handle the second cast with its special behavior
            yield return inner.TryCast(from, to);
        }
        else
        {
            // For regular spells
            yield return inner.TryCast(from, to);
        }
    }
}