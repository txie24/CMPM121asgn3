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

    private int slotIndex = -1;
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

        // Auto-find manacost and damage with improved logging:
        if (manacost == null)
        {
            manacost = GetComponentsInChildren<TextMeshProUGUI>(true) // Include inactive objects
                .FirstOrDefault(t => t.name.ToLower().Contains("mana"));
            if (manacost == null)
                Debug.LogError($"[{name}] SpellUI: no child with 'mana' in its name! Please check the naming of the mana cost UI element.");
            else
                Debug.Log($"[{name}] SpellUI bound manacost to '{manacost.name}'");
        }
        if (damage == null)
        {
            damage = GetComponentsInChildren<TextMeshProUGUI>(true) // Include inactive objects
                .FirstOrDefault(t => t.name.ToLower().Contains("dmg") || t.name.ToLower().Contains("damage"));
            if (damage == null)
                Debug.LogError($"[{name}] SpellUI: no child with 'dmg' or 'damage' in its name! Please check the naming of the damage UI element.");
            else
                Debug.Log($"[{name}] SpellUI bound damage to '{damage.name}'");
        }
        if (icon == null)
        {
            var imgGO = transform
                .GetComponentsInChildren<Image>(true) // Include inactive objects
                .Select(i => i.gameObject)
                .FirstOrDefault(go => go.name.ToLower().Contains("icon"));
            if (imgGO != null) icon = imgGO;
            else Debug.LogWarning($"[{name}] SpellUI: could not auto-find Icon");
        }
    }

    public void Initialize(int index)
    {
        slotIndex = index;
    }

    void Update()
    {
        if (spell == null) return;

        // Update text once per second
        if (Time.time > lastText + UPDATE_DELAY)
        {
            if (manacost != null)
            {
                manacost.text = Mathf.RoundToInt(spell.Mana).ToString();
            }
            else
            {
                Debug.LogWarning($"[{name}] SpellUI: manacost is null, cannot update mana cost for slot {slotIndex}");
            }

            if (damage != null)
            {
                damage.text = Mathf.RoundToInt(spell.Damage).ToString();
            }
            else
            {
                Debug.LogWarning($"[{name}] SpellUI: damage is null, cannot update damage for slot {slotIndex}");
            }

            lastText = Time.time;
        }

        // Update cooldown bar
        if (cooldown != null)
        {
            float elapsed = Time.time - spell.lastCast;
            float pct     = elapsed >= spell.Cooldown ? 0f : 1f - (elapsed / spell.Cooldown);
            cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48f * pct);
        }

        // Update icon image
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
    
    public void DropSpell()
    {
        PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();
        if (playerController != null && playerController.spellcaster != null && slotIndex >= 0)
        {
            if (slotIndex < playerController.spellcaster.spells.Count)
            {
                Debug.Log($"Dropping spell from slot {slotIndex}: {playerController.spellcaster.spells[slotIndex]?.DisplayName}");
                playerController.spellcaster.spells[slotIndex] = null;
                
                // Clear this UI
                spell = null;
                
                // Immediately deactivate this UI element
                gameObject.SetActive(false);

                // Update all of the UI slots
                playerController.UpdateSpellUI();
            }
        }
    }
}