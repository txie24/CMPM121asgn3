using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RewardScreenManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject rewardUI;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI currentWaveText;
    public TextMeshProUGUI nextWaveText;
    public TextMeshProUGUI enemiesKilledText;
    
    [Header("Spell Reward UI")]
    public Image spellIcon;                  // 法术图标
    public TextMeshProUGUI spellNameText;    // 法术名称文本
    public TextMeshProUGUI spellDescriptionText; // 法术描述文本
    public TextMeshProUGUI damageValueText;  // 伤害值文本
    public TextMeshProUGUI manaValueText;    // 魔法消耗文本
    
    [Header("Buttons")]
    public Button acceptSpellButton;         // 接受法术按钮
    public Button nextWaveButton;            // 下一波按钮
    
    private EnemySpawner spawner;
    private SpellCaster playerSpellCaster;
    private GameManager.GameState prevState;
    private Coroutine rewardCoroutine;
    private Spell offeredSpell;              // 当前提供的法术

    void Start()
    {
        spawner = Object.FindFirstObjectByType<EnemySpawner>();
        
        // Hide reward UI initially
        if (rewardUI != null) rewardUI.SetActive(false);
        
        // Hook up button events
        if (acceptSpellButton != null) acceptSpellButton.onClick.AddListener(AcceptSpell);
        if (nextWaveButton != null) nextWaveButton.onClick.AddListener(OnNextWaveClicked);
        
        prevState = GameManager.Instance.state;
    }

    void Update()
    {
        var state = GameManager.Instance.state;

        if (state == prevState) return;
        if (state == GameManager.GameState.WAVEEND &&
            spawner.currentWave <= spawner.currentLevel.waves)
        {
            if (rewardCoroutine != null)
                StopCoroutine(rewardCoroutine);

            rewardCoroutine = StartCoroutine(ShowRewardScreen());
        }
        else
        {
            if (rewardUI != null)
                rewardUI.SetActive(false);
        }

        prevState = state;
    }

    IEnumerator ShowRewardScreen()
    {
        yield return new WaitForSeconds(0.25f);

        // Set basic reward information
        if (titleText != null)
            titleText.text = "You Survived!";

        if (currentWaveText != null)
            currentWaveText.text = $"Current Wave: {spawner.currentWave - 1}";

        if (nextWaveText != null)
            nextWaveText.text = $"Next Wave: {spawner.currentWave}";

        if (enemiesKilledText != null)
            enemiesKilledText.text = $"Enemies Killed: {spawner.lastWaveEnemyCount}";
            
        // Generate random spell reward
        GenerateSpellReward();
            
        // Show reward UI
        if (rewardUI != null)
            rewardUI.SetActive(true);

        // Enable buttons
        if (acceptSpellButton != null)
            acceptSpellButton.interactable = true;
        if (nextWaveButton != null)
            nextWaveButton.interactable = true;
    }
    
    void GenerateSpellReward()
    {
        // Get player's SpellCaster component
        if (playerSpellCaster == null && GameManager.Instance.player != null)
            playerSpellCaster = GameManager.Instance.player.GetComponent<SpellCaster>();
            
        if (playerSpellCaster == null)
        {
            Debug.LogError("Cannot find player's SpellCaster component");
            return;
        }
        
        // Use SpellBuilder to generate a random spell
        SpellBuilder builder = new SpellBuilder();
        offeredSpell = builder.Build(playerSpellCaster);
        
        // Update UI display
        UpdateSpellRewardUI(offeredSpell);
    }
    
    void UpdateSpellRewardUI(Spell spell)
    {
        if (spell == null) return;
        
        // Set spell icon
        if (spellIcon != null && GameManager.Instance.spellIconManager != null)
        {
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, spellIcon);
        }
        
        // Set spell name
        if (spellNameText != null)
        {
            spellNameText.text = spell.DisplayName;
        }
        
        // Set spell description
        if (spellDescriptionText != null)
        {
            string description = GetSpellDescription(spell);
            spellDescriptionText.text = description;
        }
        
        // Set damage value
        if (damageValueText != null)
        {
            damageValueText.text = Mathf.RoundToInt(spell.Damage).ToString();
        }
        
        // Set mana cost
        if (manaValueText != null)
        {
            manaValueText.text = Mathf.RoundToInt(spell.Mana).ToString();
        }
    }
    
    string GetSpellDescription(Spell spell)
    {
        // Return appropriate description based on spell type
        if (spell is ModifierSpell)
        {
            return "Modified spell effect";
        }
        else if (spell is ArcaneBolt)
        {
            return "An arcane energy bolt that deals medium damage";
        }
        else if (spell is ArcaneSpray)
        {
            return "Fires multiple fast but short-lived projectiles, each dealing little damage";
        }
        else if (spell is MagicMissile)
        {
            return "A homing magic missile";
        }
        else if (spell is ArcaneBlast)
        {
            return "Fires an arcane projectile that explodes on impact, generating smaller fragments";
        }
        
        // Add more descriptions for other spell types
        
        return "A mysterious spell";
    }
    
    void AcceptSpell()
    {
        if (offeredSpell == null || playerSpellCaster == null) 
        {
            Debug.LogWarning("Cannot accept spell: spell or player's spell caster is null");
            return;
        }
        
        Debug.Log($"Accepting spell: {offeredSpell.DisplayName}");
        
        // Check if the spell already exists in any slot
        bool isDuplicate = false;
        for (int i = 0; i < playerSpellCaster.spells.Count; i++)
        {
            if (playerSpellCaster.spells[i] != null && 
                playerSpellCaster.spells[i].DisplayName == offeredSpell.DisplayName)
            {
                isDuplicate = true;
                Debug.Log($"Spell {offeredSpell.DisplayName} already exists in slot {i}. Not adding duplicate.");
                break;
            }
        }
        
        if (isDuplicate)
        {
            // Optional: Show a message to the player that this is a duplicate spell
            Debug.Log("Duplicate spell not added.");
            // Proceed to next wave anyway since the player accepted the reward
            OnNextWaveClicked();
            return;
        }
        
        // Find the first available slot
        int availableSlot = -1;
        for (int i = 0; i < 4; i++)
        {
            // Make sure we have enough slots in the list
            if (i >= playerSpellCaster.spells.Count)
            {
                playerSpellCaster.spells.Add(null);
            }
            
            if (playerSpellCaster.spells[i] == null)
            {
                availableSlot = i;
                break;
            }
        }
        
        if (availableSlot == -1)
        {
            Debug.Log("All spell slots are full. Player needs to drop a spell first.");
            // Could show a message to the player here
            return;
        }
        
        // Add the new spell to the available slot
        playerSpellCaster.spells[availableSlot] = offeredSpell;
        Debug.Log($"Added {offeredSpell.DisplayName} to slot {availableSlot}");
        
        // Update all the spell UI slots
        UpdatePlayerSpellUI();
        
        // Proceed to next wave
        OnNextWaveClicked();
    }
    
    void UpdatePlayerSpellUI()
    {
        // Find the SpellUIContainer
        SpellUIContainer container = FindObjectOfType<SpellUIContainer>();
        if (container != null)
        {
            // Update the container's UI
            container.UpdateSpellUIs();
        }
        else
        {
            Debug.LogWarning("Could not find SpellUIContainer");
            
            // Fallback: use the PlayerController directly
            PlayerController playerController = GameManager.Instance.player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.UpdateSpellUI();
            }
        }
    }

    void OnNextWaveClicked()
    {
        // Hide reward UI
        if (rewardUI != null)
            rewardUI.SetActive(false);

        // Disable buttons to prevent multiple clicks
        if (acceptSpellButton != null)
            acceptSpellButton.interactable = false;
        if (nextWaveButton != null)
            nextWaveButton.interactable = false;

        // Start next wave
        if (spawner != null)
            spawner.NextWave();
    }
}