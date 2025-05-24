// File: Assets/Scripts/UI/RewardScreenManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class RewardScreenManager : MonoBehaviour
{
    // singleton
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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        spawner = Object.FindFirstObjectByType<EnemySpawner>();
        rewardUI?.SetActive(false);
        relicPanel?.SetActive(false);

        acceptSpellButton?.onClick.AddListener(AcceptSpell);
        nextWaveButton?.onClick.AddListener(OnNextWaveClicked);

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

        if (state == GameManager.GameState.WAVEEND)
        {
            if (rewardCoroutine != null) StopCoroutine(rewardCoroutine);
            rewardCoroutine = StartCoroutine(ShowRewardScreen());
        }
        else
        {
            rewardUI?.SetActive(false);
        }

        prevState = state;
    }

    IEnumerator ShowRewardScreen()
    {
        yield return new WaitForSeconds(0.25f);

        // update header texts
        titleText?.SetText("You Survived!");
        currentWaveText?.SetText($"Current Wave: {spawner.currentWave - 1}");
        nextWaveText?.SetText($"Next Wave: {spawner.currentWave}");
        enemiesKilledText?.SetText($"Enemies Killed: {spawner.lastWaveEnemyCount}");

        // Always show spell reward
        GenerateSpellReward();
        spellIcon?.gameObject.SetActive(true);
        acceptSpellButton.interactable = true;
        nextWaveButton.interactable = true;

        // hide relic UI by default
        relicPanel?.SetActive(false);

        // show the whole panel
        rewardUI?.SetActive(true);
    }

    void GenerateSpellReward()
    {
        if (playerSpellCaster == null && GameManager.Instance.player != null)
            playerSpellCaster = GameManager.Instance.player.GetComponent<SpellCaster>();

        if (playerSpellCaster == null)
        {
            Debug.LogError("RewardScreenManager: Cannot find SpellCaster on player");
            return;
        }

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

        spellNameText?.SetText(spell.DisplayName);

        // description: modifiers first, then base spell
        if (spellDescriptionText != null && spellCatalog != null)
        {
            var lines = new List<string>();
            // peel off any ModifierSpell wrappers
            var cursor = spell;
            var mods = new List<ModifierSpell>();
            while (cursor is ModifierSpell m)
            {
                mods.Add(m);
                cursor = m.InnerSpell;
            }

            // for each modifier, look up its JSON "name" field
            foreach (var m in mods)
            {
                var suffix = m.DisplayName.Split(' ')[^1]; // last word
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

            // finally the base spell
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

            spellDescriptionText.SetText(string.Join("\n", lines));
        }

        damageValueText?.SetText(Mathf.RoundToInt(spell.Damage).ToString());
        manaValueText?.SetText(Mathf.RoundToInt(spell.Mana).ToString());
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

        // find empty slot
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

        playerSpellCaster.spells[slot] = offeredSpell;
        Debug.Log($"Added '{offeredSpell.DisplayName}' to slot {slot}.");

        UpdatePlayerSpellUI();
        OnNextWaveClicked();
    }

    void UpdatePlayerSpellUI()
    {
        var pc = GameManager.Instance.player?.GetComponent<PlayerController>();
        pc?.UpdateSpellUI();
    }

    void OnNextWaveClicked()
    {
        rewardUI?.SetActive(false);
        relicPanel?.SetActive(false);
        acceptSpellButton.interactable = false;
        nextWaveButton.interactable = false;
        spawner?.NextWave();
    }

    /// <summary>
    /// Called by RelicManager when it’s time to show relics.
    /// </summary>
    public void ShowRelics(Relic[] relics)
    {
        // hide spell reward
        spellIcon?.gameObject.SetActive(false);
        acceptSpellButton.interactable = false;

        // show relic panel
        relicPanel.SetActive(true);
        rewardUI.SetActive(true);

        // slot 1
        SetupRelicSlot(relicIcon1, relicName1, relicButton1, relics, 0);
        SetupRelicSlot(relicIcon2, relicName2, relicButton2, relics, 1);
        SetupRelicSlot(relicIcon3, relicName3, relicButton3, relics, 2);

        // disable Next until pick
        nextWaveButton.interactable = false;
    }

    void SetupRelicSlot(
        Image icon, TextMeshProUGUI nameText, Button button,
        Relic[] relics, int idx)
    {
        if (idx < relics.Length)
        {
            var r = relics[idx];
            icon.gameObject.SetActive(true);
            nameText.text = r.Name;
            GameManager.Instance.relicIconManager?.PlaceSprite(r.SpriteIndex, icon);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                RelicManager.I.PickRelic(r);
                OnNextWaveClicked();
            });
            button.interactable = true;
        }
        else
        {
            icon.gameObject.SetActive(false);
            button.gameObject.SetActive(false);
        }
    }
}
