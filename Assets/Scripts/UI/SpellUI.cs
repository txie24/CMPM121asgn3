using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellUI : MonoBehaviour
{
    public Image icon;
    public RectTransform cooldownBar;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI dmgText;
    public Spell spell;

    float lastTextTime;
    const float TEXT_INTERVAL = 1f;

    void Update()
    {
        if (spell == null) return;

        if (Time.time > lastTextTime + TEXT_INTERVAL)
        {
            manaText.text = Mathf.RoundToInt(spell.Mana).ToString();
            dmgText.text  = Mathf.RoundToInt(spell.Damage).ToString();
            lastTextTime = Time.time;
        }

        float elapsed = Time.time - spell.lastCast;
        float pct     = elapsed >= spell.Cooldown ? 0f : 1f - (elapsed / spell.Cooldown);
        cooldownBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48f * pct);

        GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, icon);
    }

    public void SetSpell(Spell s)
    {
        spell = s;
        if (s != null)
            GameManager.Instance.spellIconManager.PlaceSprite(s.IconIndex, icon);
    }
}
