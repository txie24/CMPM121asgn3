using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SpellUI : MonoBehaviour
{
    public GameObject      icon;
    public RectTransform   cooldown;
    public TextMeshProUGUI manacost;
    public TextMeshProUGUI damage;
    public Spell           spell;

    float lastText;
    const float UPDATE_DELAY = 1f;

    void Awake()
    {
        // Auto-find the cooldown bar if left unassigned:
        if (cooldown == null)
        {
            var allRects = GetComponentsInChildren<RectTransform>();
            cooldown = allRects
                .FirstOrDefault(rt => rt.name.ToLower().Contains("cool"));
            if (cooldown == null)
                Debug.LogError($"[{name}] SpellUI: no child with 'cool' in its name!");
            else
                Debug.Log($"[{name}] SpellUI bound cooldown to '{cooldown.name}'");
        }

        // You can do the same for manacost and damage if you like:
        if (manacost == null)
        {
            manacost = GetComponentsInChildren<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name.ToLower().Contains("mana"));
            if (manacost == null)
                Debug.LogError($"[{name}] SpellUI: no child with 'mana' in its name!");
        }
        if (damage == null)
        {
            damage = GetComponentsInChildren<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name.ToLower().Contains("dmg") 
                                   || t.name.ToLower().Contains("damage"));
            if (damage == null)
                Debug.LogError($"[{name}] SpellUI: no child with 'damage' in its name!");
        }
        if (icon == null)
        {
            var imgGO = transform
                .GetComponentsInChildren<Image>()
                .Select(i => i.gameObject)
                .FirstOrDefault(go => go.name.ToLower().Contains("icon"));
            if (imgGO != null) icon = imgGO;
            else Debug.LogWarning($"[{name}] SpellUI: could not auto‑find Icon");
        }
    }

    void Update()
    {
        if (spell == null) return;

        // update text once per second
        if (Time.time > lastText + UPDATE_DELAY)
        {
            if (manacost != null) manacost.text = Mathf.RoundToInt(spell.Mana).ToString();
            if (damage   != null) damage.text   = Mathf.RoundToInt(spell.Damage).ToString();
            lastText = Time.time;
        }

        // update cooldown bar
        if (cooldown != null)
        {
            float elapsed = Time.time - spell.lastCast;
            float pct     = elapsed >= spell.Cooldown ? 0f : 1f - (elapsed / spell.Cooldown);
            cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48f * pct);
        }

        // update icon image
        if (icon != null)
        {
            var img = icon.GetComponent<Image>();
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, img);
        }
    }

    public void SetSpell(Spell s)
    {
        spell = s;
        if (spell != null && icon != null)
        {
            var img = icon.GetComponent<Image>();
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, img);
        }
    }
    
    public void DropSpell() {
        // Find the index of this spell in the spellcaster
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null && playerController.spellcaster != null)
        {
            // Find which slot this UI represents
            int slotIndex = -1;
            if (this == playerController.spellui) slotIndex = 0;
            else if (this == playerController.spellui2) slotIndex = 1;
            else if (this == playerController.spellui3) slotIndex = 2;
            else if (this == playerController.spellui4) slotIndex = 3;
            
            if (slotIndex >= 0 && slotIndex < playerController.spellcaster.spells.Count)
            {
                Debug.Log($"Dropping spell from slot {slotIndex}: {playerController.spellcaster.spells[slotIndex]?.DisplayName}");
                playerController.spellcaster.spells[slotIndex] = null;
                
                // Clear this UI
                spell = null;
                
                // Update all of the UI slots
                playerController.UpdateSpellUI();
            }
        }
    }
}