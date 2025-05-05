using UnityEngine;
using System.Collections;
using System.Collections.Generic;    // ← add this
using Newtonsoft.Json.Linq;  

public abstract class Spell
{
    protected SpellCaster owner;
    public SpellCaster Owner => owner;
    public float lastCast;
    public StatBlock mods = new();

    protected Spell(SpellCaster owner) { this.owner = owner; }

    public abstract string DisplayName { get; }
    public abstract int    IconIndex   { get; }

    protected virtual float BaseDamage   => 10;
    protected virtual float BaseMana     => 10;
    protected virtual float BaseCooldown => 1;
    protected virtual float BaseSpeed    => 8;

    public float Damage   => StatBlock.Apply(BaseDamage,   mods.damage);
    public float Mana     => StatBlock.Apply(BaseMana,     mods.mana);
    public float Cooldown => StatBlock.Apply(BaseCooldown, mods.cd);
    public float Speed    => StatBlock.Apply(BaseSpeed,    mods.speed);
    public bool  IsReady  => Time.time >= lastCast + Cooldown;

    public IEnumerator TryCast(Vector3 from, Vector3 to)
    {
        Debug.Log($"[Spell] TryCast {DisplayName}: IsReady={IsReady}, owner.mana={owner.mana}, cost={Mana}");
        if (!IsReady || owner.mana < Mana)
        {
            Debug.Log($"[Spell] TryCast aborted ({(!IsReady ? "cooldown" : "no mana")})");
            yield break;
        }
        owner.mana -= Mathf.RoundToInt(Mana);
        lastCast = Time.time;
        Debug.Log($"[Spell] TryCast proceeding: spent {(int)Mana} mana, new mana={owner.mana}");
        yield return Cast(from, to);
    }

    protected abstract IEnumerator Cast(Vector3 from, Vector3 to);

    public virtual void LoadAttributes(Newtonsoft.Json.Linq.JObject j, Dictionary<string,float> vars) { }
}
