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
        Debug.Log($"[Spell] TryCast {DisplayName}");
        
        // The issue is related to double-checking conditions, which we'll fix
        // We already check these conditions in SpellCaster.CastSlot, so we can just cast directly here
        // This ensures that after mana regenerates, we can cast spells again
        yield return Cast(from, to);
    }

    protected abstract IEnumerator Cast(Vector3 from, Vector3 to);

    public virtual void LoadAttributes(Newtonsoft.Json.Linq.JObject j, Dictionary<string,float> vars) { }
}