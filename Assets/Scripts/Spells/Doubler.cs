// File: Doubler.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Doubler : ModifierSpell
{
    private string modifierName = "doubled";

    public Doubler(Spell inner) : base(inner) { }

    protected override string Suffix => modifierName;

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        modifierName = j["name"]?.Value<string>() ?? modifierName;
        base.LoadAttributes(j, vars);
    }

    protected override void InjectMods(StatBlock mods)
    {
        // no stat changes here
    }

    // After the normal cast, fire one extra time
    protected override IEnumerator PostCast(Vector3 from, Vector3 to)
    {
        yield return inner.TryCast(from, to);
    }
}
