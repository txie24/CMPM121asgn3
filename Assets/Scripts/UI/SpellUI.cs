using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellUI : MonoBehaviour
{
    public GameObject          icon;
    public RectTransform       cooldown;
    public TextMeshProUGUI     manacost;
    public TextMeshProUGUI     damage;
    public Spell               spell;

    float lastText;
    const float UPDATE_DELAY = 1f;

    void Awake()
    {
        // Auto‑find if left unassigned in Inspector
        if (cooldown == null)
            cooldown = transform.Find("Cooldown")?.GetComponent<RectTransform>();
        if (manacost == null)
            manacost = transform.Find("Manacost")?.GetComponent<TextMeshProUGUI>();
        if (damage == null)
            damage   = transform.Find("Damage")?.GetComponent<TextMeshProUGUI>();
        if (icon == null)
            icon     = transform.Find("Icon")?.gameObject;
    }

    void Update()
    {
        if (spell == null) return;

        // update text once per second
        if (Time.time > lastText + UPDATE_DELAY)
        {
            if (manacost != null)
                manacost.text = Mathf.RoundToInt(spell.Mana).ToString();
            if (damage != null)
                damage.text   = Mathf.RoundToInt(spell.Damage).ToString();
            lastText = Time.time;
        }

        // update cooldown bar
        if (cooldown != null)
        {
            float elapsed = Time.time - spell.lastCast;
            float pct     = elapsed >= spell.Cooldown ? 0f : 1f - (elapsed / spell.Cooldown);
            cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48f * pct);
        }

        // update icon
        if (icon != null)
        {
            Image img = icon.GetComponent<Image>();
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, img);
        }
    }

    public void SetSpell(Spell s)
    {
        spell = s;
        // immediately draw icon
        if (spell != null && icon != null)
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex,
                icon.GetComponent<Image>());
    }
}
