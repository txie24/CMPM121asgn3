using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class PlayerClass
{
	private static readonly Dictionary<string, JObject> defs;

	static PlayerClass()
	{
		var ta = Resources.Load<TextAsset>("classes");
		if (ta == null)
		{
			Debug.LogError("PlayerClass: classes.json not found!");
			defs = new Dictionary<string, JObject>();
		}
		else
		{
			defs = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(ta.text);
		}
	}

	// returns the sprite index from classes.json (0=mage,1=warlock,2=battlemage)
	public static int GetSpriteIndex(string className)
	{
		if (defs.TryGetValue(className, out var d) && d["sprite"] != null)
			return d["sprite"].Value<int>();

		Debug.LogWarning($"PlayerClass: sprite index for '{className}' missing, defaulting to 0");
		return 0;
	}

	// evaluates each stat’s RPN formula at the given wave
	public static Dictionary<string, float> GetStatsForWave(string className, int wave)
	{
		var result = new Dictionary<string, float>();
		var vars = new Dictionary<string, float> { ["wave"] = wave };

		if (!defs.TryGetValue(className, out var d))
		{
			Debug.LogWarning($"PlayerClass: no definition for '{className}', using defaults");
			result["health"] = RPNEvaluator.SafeEvaluateFloat("95 wave 5 * +", vars, 95f);
			result["mana"] = RPNEvaluator.SafeEvaluateFloat("90 wave 10 * +", vars, 90f);
			result["mana_regeneration"] = RPNEvaluator.SafeEvaluateFloat("10 wave +", vars, 10f);
			result["spellpower"] = RPNEvaluator.SafeEvaluateFloat("wave 10 *", vars, 0f);
			result["speed"] = RPNEvaluator.SafeEvaluateFloat("5", vars, 5f);
			return result;
		}

		// helper to read each field
		float Eval(string key)
			=> d[key] != null
			   ? RPNEvaluator.SafeEvaluateFloat(d[key].ToString(), vars, 0f)
			   : 0f;

		result["health"] = Eval("health");
		result["mana"] = Eval("mana");
		result["mana_regeneration"] = Eval("mana_regeneration");
		result["spellpower"] = Eval("spellpower");
		result["speed"] = Eval("speed");

		return result;
	}
}
