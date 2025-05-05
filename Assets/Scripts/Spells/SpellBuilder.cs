using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

/// <summary>
/// Builds random Spell chains by instantiating from the global namespace
/// </summary>
public class SpellBuilder
{
    private readonly Dictionary<string,JObject> catalog;
    private readonly System.Random rng = new System.Random();

    public SpellBuilder()
    {
        var ta = Resources.Load<TextAsset>("spells");
        if (ta == null)
        {
            Debug.LogError("SpellBuilder: spells.json not found in Resources!");
            catalog = new Dictionary<string,JObject>();
        }
        else
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string,JObject>>(ta.text);
            Debug.Log($"SpellBuilder: Loaded {catalog.Count} spell definitions.");
        }
    }

    /// <summary>
    /// Always returns at least an ArcaneBolt, possibly wrapped in a DamageAmp.
    /// Fully qualifies the class names so the compiler can find them.
    /// </summary>
    public Spell Build(SpellCaster owner)
    {
        // force lookup in the global namespace
        Spell s = new global::ArcaneBolt(owner);

        // 30% chance to wrap in a DamageAmp
        if (rng.NextDouble() < 0.3)
            s = new global::DamageAmp(s);

        return s;
    }
}
