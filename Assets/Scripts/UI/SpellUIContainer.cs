using UnityEngine;

public class SpellUIContainer : MonoBehaviour
{
    public GameObject[] spellUIs;
    public PlayerController player;

    void Start()
    {
        // Initially, only show the first slot that has the initial spell
        for(int i = 0; i < spellUIs.Length; ++i)
        {
            if (player != null && player.spellcaster != null && 
                i < player.spellcaster.spells.Count && player.spellcaster.spells[i] != null)
            {
                spellUIs[i].SetActive(true);
            }
            else
            {
                spellUIs[i].SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // No need for constant updates
    }
    
    // Method to update all SpellUIs based on current spells
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
                    spellUI.SetSpell(spellcaster.spells[i]);
                }
            }
            else
            {
                spellUIs[i].SetActive(false);
            }
        }
    }
}