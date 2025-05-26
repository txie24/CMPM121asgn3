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

    [Header("Feedback Text (replaces buttons)")]
    public TextMeshProUGUI spellAcquiredText;
    public TextMeshProUGUI relicTakenText1;
    public TextMeshProUGUI relicTakenText2;
    public TextMeshProUGUI relicTakenText3;

    private EnemySpawner spawner;
    private SpellCaster playerSpellCaster;
    private GameManager.GameState prevState;
    private Coroutine rewardCoroutine;
    private Spell offeredSpell;
    private Dictionary<string, JObject> spellCatalog;
    private List<Relic> ownedRelics = new List<Relic>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        spawner = Object.FindFirstObjectByType<EnemySpawner>();
        rewardUI?.SetActive(false);
        relicPanel?.SetActive(false);

        if (acceptSpellButton != null)
            acceptSpellButton.onClick.AddListener(AcceptSpell);
        if (nextWaveButton != null)
            nextWaveButton.onClick.AddListener(OnNextWaveClicked);

        prevState = GameManager.Instance.state;

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

        // reset feedback texts & buttons
        spellAcquiredText?.gameObject.SetActive(false);
        acceptSpellButton?.gameObject.SetActive(true);
        relicTakenText1?.gameObject.SetActive(false);
        relicTakenText2?.gameObject.SetActive(false);
        relicTakenText3?.gameObject.SetActive(false);

        // header
        titleText?.SetText("You Survived!");
        currentWaveText?.SetText($"Current Wave: {spawner.currentWave - 1}");
        nextWaveText?.SetText($"Next Wave: {spawner.currentWave}");
        enemiesKilledText?.SetText($"Enemies Killed: {spawner.lastWaveEnemyCount}");

        // spell reward
        GenerateSpellReward();
        spellIcon?.gameObject.SetActive(true);
        acceptSpellButton.interactable = true;
        nextWaveButton.interactable = true;

        // hide relic panel to reset
        relicPanel?.SetActive(false);

        // FOR TESTING: always show relics on every wave
        ShowRelicReward();

        // finally, show the full UI
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

        offeredSpell = new SpellBuilder().Build(playerSpellCaster);
        UpdateSpellRewardUI(offeredSpell);
    }

    void UpdateSpellRewardUI(Spell spell)
    {
        if (spell == null) return;

        GameManager.Instance.spellIconManager?.PlaceSprite(spell.IconIndex, spellIcon);
        spellNameText?.SetText(spell.DisplayName);

        if (spellDescriptionText != null && spellCatalog != null)
        {
            var lines = new List<string>();
            var cursor = spell;
            var mods = new List<ModifierSpell>();
            while (cursor is ModifierSpell m)
            {
                mods.Add(m);
                cursor = m.InnerSpell;
            }

            foreach (var m in mods)
            {
                var suffix = m.DisplayName.Split(' ')[^1];
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

        bool duplicate = false;
        for (int i = 0; i < playerSpellCaster.spells.Count; i++)
        {
            if (playerSpellCaster.spells[i]?.DisplayName == offeredSpell.DisplayName)
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
        if (slot < 0)
        {
            Debug.Log("All spell slots full; cannot add new spell");
            return;
        }

        playerSpellCaster.spells[slot] = offeredSpell;
        UpdatePlayerSpellUI();

        // show feedback instead of next-wave
        acceptSpellButton.gameObject.SetActive(false);
        spellAcquiredText?.gameObject.SetActive(true);
    }

    void UpdatePlayerSpellUI()
    {
        var container = Object.FindFirstObjectByType<SpellUIContainer>();
        if (container != null)
            container.UpdateSpellUIs();
        else
            GameManager.Instance.player?.GetComponent<PlayerController>()?.UpdateSpellUI();
    }

    void OnNextWaveClicked()
    {
        rewardUI?.SetActive(false);
        relicPanel?.SetActive(false);
        acceptSpellButton.interactable = false;
        nextWaveButton.interactable = false;
        spawner?.NextWave();
    }

    // — relic stuff below (unchanged loading logic) —

    void ShowRelicReward()
    {
        var relicsText = Resources.Load<TextAsset>("relics");
        if (relicsText == null)
        {
            Debug.LogError("No relics.json found, only showing spell");
            return;
        }

        try
        {
            var list = JsonUtility.FromJson<RelicDataList>("{\"relics\":" + relicsText.text + "}");
            var allRelics = list.relics.Select(d => new Relic(d)).ToList();
            var available = allRelics.Where(r => !ownedRelics.Any(o => o.Name == r.Name)).ToList();
            if (available.Count == 0) return;
            var choices = available.OrderBy(_ => Random.value).Take(3).ToArray();
            ShowRelics(choices);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load relics: {e.Message}");
        }
    }

    public void ShowRelics(Relic[] relics)
    {
        titleText?.SetText("You Survived! Choose a Spell and a Relic!");
        relicPanel?.SetActive(true);

        SetupRelicSlot(relicIcon1, relicName1, relicButton1, relics, 0);
        SetupRelicSlot(relicIcon2, relicName2, relicButton2, relics, 1);
        SetupRelicSlot(relicIcon3, relicName3, relicButton3, relics, 2);
    }

    void SetupRelicSlot(Image icon, TextMeshProUGUI nameText, Button button, Relic[] relics, int idx)
    {
        // hide any old "taken" text
        switch (idx)
        {
            case 0: relicTakenText1?.gameObject.SetActive(false); break;
            case 1: relicTakenText2?.gameObject.SetActive(false); break;
            case 2: relicTakenText3?.gameObject.SetActive(false); break;
        }

        if (idx < relics.Length)
        {
            var r = relics[idx];
            icon?.gameObject.SetActive(true);
            GameManager.Instance.relicIconManager?.PlaceSprite(r.SpriteIndex, icon);

            nameText?.gameObject.SetActive(true);
            nameText.text = r.Name;

            button?.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();

            int localIdx = idx;
            Relic localRelic = r;
            button.onClick.AddListener(() =>
            {
                PickRelic(localRelic);
                HideOtherRelicSlots(localIdx);
                button.gameObject.SetActive(false);
                switch (localIdx)
                {
                    case 0: relicTakenText1?.gameObject.SetActive(true); break;
                    case 1: relicTakenText2?.gameObject.SetActive(true); break;
                    case 2: relicTakenText3?.gameObject.SetActive(true); break;
                }
            });

            button.interactable = true;
        }
        else
        {
            icon?.gameObject.SetActive(false);
            nameText?.gameObject.SetActive(false);
            button?.gameObject.SetActive(false);
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
    }

    /// <summary>
    /// After picking one relic, hide the other two slots completely.
    /// </summary>
    private void HideOtherRelicSlots(int selectedIdx)
    {
        if (selectedIdx != 0)
        {
            relicIcon1.gameObject.SetActive(false);
            relicName1.gameObject.SetActive(false);
            relicButton1.gameObject.SetActive(false);
            relicTakenText1.gameObject.SetActive(false);
        }
        if (selectedIdx != 1)
        {
            relicIcon2.gameObject.SetActive(false);
            relicName2.gameObject.SetActive(false);
            relicButton2.gameObject.SetActive(false);
            relicTakenText2.gameObject.SetActive(false);
        }
        if (selectedIdx != 2)
        {
            relicIcon3.gameObject.SetActive(false);
            relicName3.gameObject.SetActive(false);
            relicButton3.gameObject.SetActive(false);
            relicTakenText3.gameObject.SetActive(false);
        }
    }
}
