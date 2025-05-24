// File: Assets/Scripts/UI/RewardScreenManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class RewardScreenManager : MonoBehaviour
{
    // ←— singleton
    public static RewardScreenManager Instance { get; private set; }

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

    [Header("Relic Reward UI")]
    [Tooltip("Parent panel containing the 3 relic slots")]
    public GameObject relicPanel;
    public Image relicIcon1;
    public Image relicIcon2;
    public Image relicIcon3;
    public TextMeshProUGUI relicName1;
    public TextMeshProUGUI relicName2;
    public TextMeshProUGUI relicName3;
    public Button relicButton1;
    public Button relicButton2;
    public Button relicButton3;

    private EnemySpawner spawner;
    private SpellCaster playerSpellCaster;
    private GameManager.GameState prevState;
    private Coroutine rewardCoroutine;
    private Spell offeredSpell;
    private Dictionary<string, JObject> spellCatalog;
    private List<Relic> ownedRelics = new List<Relic>();

    void Start()
    {
        // Initialize singleton
        Instance = this;

        spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (rewardUI != null) rewardUI.SetActive(false);
        if (acceptSpellButton != null) acceptSpellButton.onClick.AddListener(AcceptSpell);
        if (nextWaveButton != null) nextWaveButton.onClick.AddListener(OnNextWaveClicked);

        prevState = GameManager.Instance.state;

        // load spells.json
        var ta = Resources.Load<TextAsset>("spells");
        if (ta != null)
            spellCatalog = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(ta.text);
        else
            Debug.LogError("RewardScreenManager: spells.json not found in Resources!");
    }

    void Update()
    {
        var state = GameManager.Instance.state;
        if (state == prevState) return;

        // show reward screen on every WAVEEND (including endless)
        if (state == GameManager.GameState.WAVEEND)
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

        // ALWAYS reset UI to show spell first
        ResetUIToSpellMode();

        if (titleText != null) titleText.text = "You Survived!";
        if (currentWaveText != null) currentWaveText.text = $"Current Wave: {spawner.currentWave - 1}";
        if (nextWaveText != null) nextWaveText.text = $"Next Wave: {spawner.currentWave}";
        if (enemiesKilledText != null) enemiesKilledText.text = $"Enemies Killed: {spawner.lastWaveEnemyCount}";

        // ALWAYS generate and show spell reward first
        GenerateSpellReward();

        // CHECK IF THIS IS A RELIC WAVE (every 3rd wave: 3, 6, 9, etc.)
        int completedWave = spawner.currentWave - 1; // Wave we just completed
        if (completedWave % 3 == 0 && completedWave > 0)
        {
            Debug.Log($"Wave {completedWave} is a relic wave! Showing relics alongside spell.");
            ShowRelicReward();
        }

        if (rewardUI != null) rewardUI.SetActive(true);
    }

    void ResetUIToSpellMode()
    {
        // Show spell UI elements
        if (spellIcon != null) spellIcon.gameObject.SetActive(true);
        if (spellNameText != null) spellNameText.gameObject.SetActive(true);
        if (spellDescriptionText != null) spellDescriptionText.gameObject.SetActive(true);
        if (damageValueText != null) damageValueText.gameObject.SetActive(true);
        if (manaValueText != null) manaValueText.gameObject.SetActive(true);
        if (acceptSpellButton != null)
        {
            acceptSpellButton.gameObject.SetActive(true);
            acceptSpellButton.interactable = true;
        }
        if (nextWaveButton != null)
        {
            nextWaveButton.gameObject.SetActive(true);
            nextWaveButton.interactable = true;
        }

        // Hide relic UI
        if (relicPanel != null) relicPanel.SetActive(false);
    }

    void ShowRelicReward()
    {
        // Load and show relics ALONGSIDE the spell
        var relicsText = Resources.Load<TextAsset>("relics");
        if (relicsText == null)
        {
            Debug.LogError("No relics.json found, only showing spell");
            return;
        }

        try
        {
            // Parse relics
            var list = JsonUtility.FromJson<RelicDataList>("{\"relics\":" + relicsText.text + "}");
            var allRelics = new List<Relic>();

            foreach (var relicData in list.relics)
            {
                allRelics.Add(new Relic(relicData));
            }

            // Get relics not already owned
            var availableRelics = allRelics.Where(r => !ownedRelics.Any(owned => owned.Name == r.Name)).ToList();

            if (availableRelics.Count == 0)
            {
                Debug.Log("All relics already owned, only showing spell");
                return;
            }

            // Pick 3 random available relics
            var choices = availableRelics.OrderBy(_ => UnityEngine.Random.value).Take(3).ToArray();

            ShowRelics(choices);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load relics: {e.Message}");
        }
    }

    void GenerateSpellReward()
    {
        // cache player SpellCaster
        if (playerSpellCaster == null && GameManager.Instance.player != null)
            playerSpellCaster = GameManager.Instance.player.GetComponent<SpellCaster>();

        if (playerSpellCaster == null)
        {
            Debug.LogError("RewardScreenManager: Cannot find SpellCaster on player");
            return;
        }

        // build random spell
        var builder = new SpellBuilder();
        offeredSpell = builder.Build(playerSpellCaster);

        UpdateSpellRewardUI(offeredSpell);
    }

    void UpdateSpellRewardUI(Spell spell)
    {
        if (spell == null) return;

        // icon & name
        if (spellIcon != null && GameManager.Instance.spellIconManager != null)
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, spellIcon);

        if (spellNameText != null)
            spellNameText.text = spell.DisplayName;

        // description: modifiers first, then base spell
        if (spellDescriptionText != null && spellCatalog != null)
        {
            var lines = new List<string>();
            // collect modifier wrappers
            var cursor = spell;
            var mods = new List<ModifierSpell>();
            while (cursor is ModifierSpell m)
            {
                mods.Add(m);
                cursor = m.InnerSpell;
            }
            // for each modifier, pull its JSON description
            foreach (var m in mods)
            {
                var parts = m.DisplayName.Split(' ');
                var suffix = parts[^1]; // last word
                foreach (var kv in spellCatalog)
                {
                    var j = kv.Value;
                    if (j["name"]?.Value<string>() == suffix)
                    {
                        lines.Add($"{suffix}: {j["description"].Value<string>()}");
                        break;
                    }
                }
            }
            // then the base spell
            var baseName = cursor.DisplayName;
            foreach (var kv in spellCatalog)
            {
                var j = kv.Value;
                if (j["name"]?.Value<string>() == baseName)
                {
                    lines.Add($"{baseName}: {j["description"].Value<string>()}");
                    break;
                }
            }
            spellDescriptionText.text = string.Join("\n", lines);
        }

        // damage & mana
        if (damageValueText != null)
            damageValueText.text = Mathf.RoundToInt(spell.Damage).ToString();
        if (manaValueText != null)
            manaValueText.text = Mathf.RoundToInt(spell.Mana).ToString();
    }

    void AcceptSpell()
    {
        if (offeredSpell == null || playerSpellCaster == null)
        {
            Debug.LogWarning("Cannot accept spell: missing data");
            return;
        }

        // check for duplicate
        bool duplicate = false;
        for (int i = 0; i < playerSpellCaster.spells.Count; i++)
        {
            if (playerSpellCaster.spells[i] != null &&
                playerSpellCaster.spells[i].DisplayName == offeredSpell.DisplayName)
            {
                duplicate = true;
                Debug.Log($"Duplicate spell in slot {i}, skipping add.");
                break;
            }
        }

        if (duplicate)
        {
            OnNextWaveClicked();
            return;
        }

        // find an empty slot (max 4)
        int slot = -1;
        for (int i = 0; i < 4; i++)
        {
            if (i >= playerSpellCaster.spells.Count)
                playerSpellCaster.spells.Add(null);

            if (playerSpellCaster.spells[i] == null)
            {
                slot = i;
                break;
            }
        }

        if (slot == -1)
        {
            Debug.Log("All spell slots full; cannot add new spell");
            return;
        }

        // assign directly into the list
        playerSpellCaster.spells[slot] = offeredSpell;
        Debug.Log($"Added '{offeredSpell.DisplayName}' to slot {slot}.");

        // refresh UI & proceed
        UpdatePlayerSpellUI();
        OnNextWaveClicked();
    }

    void UpdatePlayerSpellUI()
    {
        var container = Object.FindFirstObjectByType<SpellUIContainer>();
        if (container != null)
            container.UpdateSpellUIs();
        else if (GameManager.Instance.player != null)
            GameManager.Instance.player.GetComponent<PlayerController>()?.UpdateSpellUI();
    }

    void OnNextWaveClicked()
    {
        if (rewardUI != null) rewardUI.SetActive(false);
        if (acceptSpellButton != null) acceptSpellButton.interactable = false;
        if (nextWaveButton != null) nextWaveButton.interactable = false;
        if (relicPanel != null) relicPanel.SetActive(false);
        spawner?.NextWave();
    }

    // — relic stuff below —
    public void ShowRelics(Relic[] relics)
    {
        // DON'T hide spell UI - show relics ALONGSIDE spell
        // Change title to indicate both rewards
        if (titleText != null) titleText.text = "You Survived! Choose a Spell and a Relic!";

        // Show relic UI panel
        if (relicPanel != null) relicPanel.SetActive(true);

        SetupRelicSlot(relicIcon1, relicName1, relicButton1, relics, 0);
        SetupRelicSlot(relicIcon2, relicName2, relicButton2, relics, 1);
        SetupRelicSlot(relicIcon3, relicName3, relicButton3, relics, 2);

        // Don't disable next wave button - let player proceed after choosing spell/relic
    }

    void SetupRelicSlot(Image icon, TextMeshProUGUI nameText, Button button, Relic[] relics, int idx)
    {
        if (idx < relics.Length)
        {
            var r = relics[idx];

            if (icon != null)
            {
                icon.gameObject.SetActive(true);
                if (GameManager.Instance.relicIconManager != null)
                {
                    GameManager.Instance.relicIconManager.PlaceSprite(r.SpriteIndex, icon);
                }
            }

            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.text = r.Name;
            }

            if (button != null)
            {
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    PickRelic(r);
                    HideRelicPanel(); // Hide relic selection after picking
                });
                button.interactable = true;
            }
        }
        else
        {
            if (icon != null) icon.gameObject.SetActive(false);
            if (nameText != null) nameText.gameObject.SetActive(false);
            if (button != null) button.gameObject.SetActive(false);
        }
    }

    void PickRelic(Relic relic)
    {
        if (ownedRelics.Any(r => r.Name == relic.Name))
        {
            Debug.LogWarning($"Relic {relic.Name} already owned");
            return;
        }

        ownedRelics.Add(relic);
        relic.Init();
        Debug.Log($"Picked relic: {relic.Name}");
    }

    void HideRelicPanel()
    {
        if (relicPanel != null) relicPanel.SetActive(false);
        if (titleText != null) titleText.text = "You Survived!";
    }
}