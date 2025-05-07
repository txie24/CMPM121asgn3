// File: Assets/Scripts/Spells/Railgun.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class Railgun : Spell
{
    private string displayName;
    private string description;
    private int iconIndex;
    private float baseMana;
    private float baseCooldown;
    private string trajectory;
    private int projectileSprite;

    private string damageExpr;
    private string speedExpr;

    public Railgun(SpellCaster owner) : base(owner) { }

    public override string DisplayName => displayName;
    public override int IconIndex => iconIndex;

    protected override float BaseDamage => RPNEvaluator.SafeEvaluateFloat(
        damageExpr,
        GetVars(),
        50f);

    protected override float BaseSpeed => RPNEvaluator.SafeEvaluateFloat(
        speedExpr,
        GetVars(),
        25f);

    protected override float BaseMana => baseMana;
    protected override float BaseCooldown => baseCooldown;

    private Dictionary<string, float> GetVars() => new()
    {
        { "power", owner.spellPower },
        { "wave", GetCurrentWave() }
    };

    private float GetCurrentWave()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        return spawner != null ? spawner.currentWave : 1;
    }

    public override void LoadAttributes(JObject j, Dictionary<string, float> vars)
    {
        displayName = j["name"].Value<string>();
        description = j["description"]?.Value<string>() ?? "";
        iconIndex = j["icon"].Value<int>();

        damageExpr = j["damage"]["amount"].Value<string>();
        speedExpr = j["projectile"]["speed"].Value<string>();
        baseMana = RPNEvaluator.SafeEvaluateFloat(j["mana_cost"].Value<string>(), vars, 10f);
        baseCooldown = RPNEvaluator.SafeEvaluateFloat(j["cooldown"].Value<string>(), vars, 3f);

        trajectory = j["projectile"]["trajectory"].Value<string>();
        projectileSprite = j["projectile"]["sprite"]?.Value<int>() ?? 0;
    }

    protected override IEnumerator Cast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[{displayName}] Casting ▶ dmg={Damage:F1}, spd={Speed:F1}, cooldown={Cooldown:F1}");

        Vector3 direction = (to - from).normalized;

        GameManager.Instance.projectileManager.CreatePiercingProjectile(
            projectileSprite,
            trajectory,
            from,
            direction,
            Speed,
            (hit, impactPos) =>
            {
                if (hit.team != owner.team)
                {
                    int amount = Mathf.RoundToInt(Damage);
                    var dmg = new global::Damage(amount, global::Damage.Type.ARCANE);
                    hit.Damage(dmg);
                    Debug.Log($"[{displayName}] Pierced {hit.owner.name} for {amount} dmg");
                }
            });

        yield return null;
    }
}
