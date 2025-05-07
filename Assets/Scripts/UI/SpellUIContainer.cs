using UnityEngine;

public class SpellUIContainer : MonoBehaviour
{
    public GameObject[] spellUIs;
    public PlayerController player;

    void Start()
    {
        for (int i = 0; i < spellUIs.Length; ++i)
        {
            if (player != null && player.spellcaster != null && 
                i < player.spellcaster.spells.Count && player.spellcaster.spells[i] != null)
            {
                spellUIs[i].SetActive(true);
                SpellUI spellUI = spellUIs[i].GetComponent<SpellUI>();
                if (spellUI != null)
                {
                    spellUI.Initialize(i);
                    spellUI.SetSpell(player.spellcaster.spells[i]);
                }
            }
            else
            {
                spellUIs[i].SetActive(false);
            }
        }
    }

    void Update()
    {
        // No need for constant updates
    }
    
    public void UpdateSpellUIs()
    {
        if (player == null || player.spellcaster == null)
            return;
        
        var spellcaster = player.spellcaster;
        
        for (int i = 0; i < spellUIs.Length && i < spellcaster.spells.Count; i++)
        {
            if (spellcaster.spells[i] != null)
            {
                spellUIs[i].SetActive(true);
                SpellUI spellUI = spellUIs[i].GetComponent<SpellUI>();
                if (spellUI != null)
                {
                    spellUI.Initialize(i);
                    spellUI.SetSpell(spellcaster.spells[i]);
                }
            }
            else
            {
                spellUIs[i].SetActive(false);
            }
        }
    }

    public void HideSpellUISlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < spellUIs.Length)
        {
            spellUIs[slotIndex].SetActive(false);
            Debug.Log($"Hiding spell UI slot {slotIndex}");
        }
    }
}