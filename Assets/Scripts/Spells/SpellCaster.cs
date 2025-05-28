using UnityEngine;
using System;              // ← added for Action
using System.Collections;
using System.Collections.Generic;

public class SpellCaster : MonoBehaviour
{
    [Header("Mana Settings")]
    public int max_mana;
    public int mana;
    public int mana_reg;

    [Header("Team")]
    public Hittable.Team team;

    [Header("Spells")]
    public int spellPower;
    public List<Spell> spells = new(4);

    // ← new: fired after any spell finishes casting
    public static event Action OnSpellCast;

    void Awake()
    {
        StartCoroutine(ManaRegeneration());
        var builder = new SpellBuilder();
        spells.Add(builder.Build(this));
        while (spells.Count < 4)
            spells.Add(null);
    }

    IEnumerator ManaRegeneration()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            mana = Mathf.Min(max_mana, mana + mana_reg);
        }
    }

    public IEnumerator CastSlot(int slot, Vector3 from, Vector3 to)
    {
        if (slot < 0 || slot >= spells.Count) yield break;

        Spell s = spells[slot];
        if (s == null)
        {
            Debug.LogWarning($"[SpellCaster] No spell in slot {slot}");
            yield break;
        }

        if (!s.IsReady || mana < s.Mana)
            yield break;

        Debug.Log($"[SpellCaster] Slot {slot} -> Casting \"{s.DisplayName}\" (mana={mana}, cost={s.Mana})");

        mana -= Mathf.RoundToInt(s.Mana);
        s.lastCast = Time.time;

        yield return s.TryCast(from, to);

        // ← new: notify one‐shot relics
        OnSpellCast?.Invoke();
    }
}
