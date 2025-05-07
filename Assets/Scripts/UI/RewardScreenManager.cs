using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class RewardScreenManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject rewardUI;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI currentWaveText;
    public TextMeshProUGUI nextWaveText;
    public TextMeshProUGUI enemiesKilledText;

    [Header("Spell Reward UI")]
    public Image spellIcon;
    public TextMeshProUGUI spellNameText;
    public TextMeshProUGUI spellDescriptionText;
    public TextMeshProUGUI damageValueText;
    public TextMeshProUGUI manaValueText;

    [Header("Buttons")]
    public Button acceptSpellButton;
    public Button nextWaveButton;

    private EnemySpawner spawner;
    private SpellCaster playerSpellCaster;
    private GameManager.GameState prevState;
    private Coroutine rewardCoroutine;
    private Spell offeredSpell;
    private Dictionary<string, JObject> spellCatalog;

    void Start()
    {
        spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (rewardUI != null) rewardUI.SetActive(false);
        if (acceptSpellButton != null) acceptSpellButton.onClick.AddListener(AcceptSpell);
        if (nextWaveButton != null) nextWaveButton.onClick.AddListener(OnNextWaveClicked);
        prevState = GameManager.Instance.state;

        var ta = Resources.Load<TextAsset>("spells");
        spellCatalog = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(ta.text);
    }

    void Update()
    {
        var state = GameManager.Instance.state;
        if (state == prevState) return;

        if (state == GameManager.GameState.WAVEEND && spawner.currentWave <= spawner.currentLevel.waves)
        {
            if (rewardCoroutine != null) StopCoroutine(rewardCoroutine);
            rewardCoroutine = StartCoroutine(ShowRewardScreen());
        }
        else
        {
            if (rewardUI != null) rewardUI.SetActive(false);
        }

        prevState = state;
    }

    IEnumerator ShowRewardScreen()
    {
        yield return new WaitForSeconds(0.25f);

        if (titleText != null) titleText.text = "You Survived!";
        if (currentWaveText != null) currentWaveText.text = $"Current Wave: {spawner.currentWave - 1}";
        if (nextWaveText != null) nextWaveText.text = $"Next Wave: {spawner.currentWave}";
        if (enemiesKilledText != null) enemiesKilledText.text = $"Enemies Killed: {spawner.lastWaveEnemyCount}";

        GenerateSpellReward();

        if (rewardUI != null) rewardUI.SetActive(true);
        if (acceptSpellButton != null) acceptSpellButton.interactable = true;
        if (nextWaveButton != null) nextWaveButton.interactable = true;
    }

    void GenerateSpellReward()
    {
        if (playerSpellCaster == null && GameManager.Instance.player != null)
            playerSpellCaster = GameManager.Instance.player.GetComponent<SpellCaster>();

        if (playerSpellCaster == null)
        {
            Debug.LogError("Cannot find player's SpellCaster component");
            return;
        }

        SpellBuilder builder = new SpellBuilder();
        offeredSpell = builder.Build(playerSpellCaster);
        UpdateSpellRewardUI(offeredSpell);
    }

    void UpdateSpellRewardUI(Spell spell)
    {
        if (spell == null) return;

        if (spellIcon != null && GameManager.Instance.spellIconManager != null)
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, spellIcon);

        if (spellNameText != null)
            spellNameText.text = spell.DisplayName;

        if (spellDescriptionText != null)
        {
            string id = spell.GetType().Name.ToLower();
            if (id.Contains("arcane")) id = id.Replace("arcane", "arcane_");
            if (spellCatalog.TryGetValue(id, out var obj))
                spellDescriptionText.text = obj["description"].Value<string>();
            else
                spellDescriptionText.text = "A mysterious spell";
        }

        if (damageValueText != null)
            damageValueText.text = Mathf.RoundToInt(spell.Damage).ToString();

        if (manaValueText != null)
            manaValueText.text = Mathf.RoundToInt(spell.Mana).ToString();
    }

    void AcceptSpell()
    {
        if (offeredSpell == null || playerSpellCaster == null)
        {
            Debug.LogWarning("Cannot accept spell: spell or player's spell caster is null");
            return;
        }

        Debug.Log($"Accepting spell: {offeredSpell.DisplayName}");

        bool isDuplicate = false;
        for (int i = 0; i < playerSpellCaster.spells.Count; i++)
        {
            if (playerSpellCaster.spells[i] != null && playerSpellCaster.spells[i].DisplayName == offeredSpell.DisplayName)
            {
                isDuplicate = true;
                Debug.Log($"Spell {offeredSpell.DisplayName} already exists in slot {i}. Not adding duplicate.");
                break;
            }
        }

        if (isDuplicate)
        {
            Debug.Log("Duplicate spell not added.");
            OnNextWaveClicked();
            return;
        }

        int availableSlot = -1;
        for (int i = 0; i < 4; i++)
        {
            if (i >= playerSpellCaster.spells.Count)
                playerSpellCaster.spells.Add(null);

            if (playerSpellCaster.spells[i] == null)
            {
                availableSlot = i;
                break;
            }
        }

        if (availableSlot == -1)
        {
            Debug.Log("All spell slots are full. Player needs to drop a spell first.");
            return;
        }

        playerSpellCaster.spells[availableSlot] = offeredSpell;
        Debug.Log($"Added {offeredSpell.DisplayName} to slot {availableSlot}");
        UpdatePlayerSpellUI();
        OnNextWaveClicked();
    }

    void UpdatePlayerSpellUI()
    {
        SpellUIContainer container = Object.FindFirstObjectByType<SpellUIContainer>();
        if (container != null)
        {
            container.UpdateSpellUIs();
        }
        else
        {
            Debug.LogWarning("Could not find SpellUIContainer");
            PlayerController playerController = GameManager.Instance.player.GetComponent<PlayerController>();
            if (playerController != null)
                playerController.UpdateSpellUI();
        }
    }

    void OnNextWaveClicked()
    {
        if (rewardUI != null) rewardUI.SetActive(false);
        if (acceptSpellButton != null) acceptSpellButton.interactable = false;
        if (nextWaveButton != null) nextWaveButton.interactable = false;
        if (spawner != null) spawner.NextWave();
    }
}
