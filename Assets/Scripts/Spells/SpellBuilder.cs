using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

/// <summary>
/// Builds random Spell chains by instantiating from the global namespace
/// (currently only ArcaneBolt is supported)
/// </summary>
public class SpellBuilder
{
    private readonly Dictionary<string, JObject> catalog;
    private readonly System.Random rng = new System.Random();

    public SpellBuilder()
    {
        var ta = Resources.Load<TextAsset>("spells");
        if (ta == null)
        {
            Debug.LogError("SpellBuilder: spells.json not found in Resources!");
            catalog = new Dictionary<string, JObject>();
        }
        else
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(ta.text);
            Debug.Log($"SpellBuilder: Loaded {catalog.Count} spell definitions.");
        }
    }

    /// <summary>
    /// Always returns at least an ArcaneBolt.
    /// </summary>
    public Spell Build(SpellCaster owner)
    {
        // instantiate the base spell
        Spell s = new global::ArcaneBolt(owner);

        // TODO: once you have more modifiers/base spells, you can extend this.
        return s;
    }
}
