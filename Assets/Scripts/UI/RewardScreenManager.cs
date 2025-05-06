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
        
        // 隐藏奖励界面
        if (rewardUI != null) rewardUI.SetActive(false);
        
        // 绑定按钮事件
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

        // 设置基本的奖励信息
        if (titleText != null)
            titleText.text = "You Survived!";

        if (currentWaveText != null)
            currentWaveText.text = $"Current Wave: {spawner.currentWave - 1}";

        if (nextWaveText != null)
            nextWaveText.text = $"Next Wave: {spawner.currentWave}";

        if (enemiesKilledText != null)
            enemiesKilledText.text = $"Enemies Killed: {spawner.lastWaveEnemyCount}";
            
        // 生成随机法术奖励
        GenerateSpellReward();
            
        // 显示奖励UI
        if (rewardUI != null)
            rewardUI.SetActive(true);

        // 启用按钮
        if (acceptSpellButton != null)
            acceptSpellButton.interactable = true;
        if (nextWaveButton != null)
            nextWaveButton.interactable = true;
    }
    
    void GenerateSpellReward()
    {
        // 获取玩家的SpellCaster组件
        if (playerSpellCaster == null && GameManager.Instance.player != null)
            playerSpellCaster = GameManager.Instance.player.GetComponent<SpellCaster>();
            
        if (playerSpellCaster == null)
        {
            Debug.LogError("无法找到玩家的SpellCaster组件");
            return;
        }
        
        // 使用SpellBuilder生成随机法术
        SpellBuilder builder = new SpellBuilder();
        offeredSpell = builder.Build(playerSpellCaster);
        
        // 更新UI显示
        UpdateSpellRewardUI(offeredSpell);
    }
    
    void UpdateSpellRewardUI(Spell spell)
    {
        if (spell == null) return;
        
        // 设置法术图标
        if (spellIcon != null && GameManager.Instance.spellIconManager != null)
        {
            GameManager.Instance.spellIconManager.PlaceSprite(spell.IconIndex, spellIcon);
        }
        
        // 设置法术名称
        if (spellNameText != null)
        {
            spellNameText.text = spell.DisplayName;
        }
        
        // 设置法术描述
        if (spellDescriptionText != null)
        {
            string description = GetSpellDescription(spell);
            spellDescriptionText.text = description;
        }
        
        // 设置伤害值
        if (damageValueText != null)
        {
            damageValueText.text = Mathf.RoundToInt(spell.Damage).ToString();
        }
        
        // 设置魔法消耗
        if (manaValueText != null)
        {
            manaValueText.text = Mathf.RoundToInt(spell.Mana).ToString();
        }
    }
    
    string GetSpellDescription(Spell spell)
    {
        // 根据法术类型返回适当的描述
        if (spell is ModifierSpell)
        {
            return "修改了基础法术的效果"; // 避免使用受保护的Suffix属性
        }
        else if (spell is ArcaneBolt)
        {
            return "一个奥术能量弹，造成中等伤害";
        }
        else if (spell is ArcaneSpray)
        {
            return "发射多个快速但短命的投射物，每个造成少量伤害";
        }
        else if (spell is MagicMissile)
        {
            return "一个追踪敌人的魔法飞弹";
        }
        else if (spell is ArcaneBlast)
        {
            return "发射一个会在击中敌人后爆炸的奥术弹，生成多个小型弹片";
        }
        
        // 针对其他法术类型添加更多描述
        
        return "一个神秘的法术";
    }
    
    void AcceptSpell()
    {
        if (offeredSpell == null || playerSpellCaster == null) 
        {
            Debug.LogWarning("无法接受法术：法术或玩家法术施放者为空");
            return;
        }
        
        Debug.Log($"接受法术: {offeredSpell.DisplayName}");
        
        // 添加法术到玩家法术栏或替换现有法术
        if (playerSpellCaster.spells.Count < 4)
        {
            // 添加新法术
            playerSpellCaster.spells.Add(offeredSpell);
        }
        else
        {
            // 替换第一个法术（这里可以扩展为让玩家选择替换哪个法术）
            playerSpellCaster.spells[0] = offeredSpell;
        }
        
        // 更新法术UI
        UpdatePlayerSpellUI();
        
        // 进入下一波
        OnNextWaveClicked();
    }
    
    void UpdatePlayerSpellUI()
    {
        // 修复警告：使用FindFirstObjectByType代替FindObjectOfType
        SpellUIContainer container = Object.FindFirstObjectByType<SpellUIContainer>();
        if (container != null && container.spellUIs != null)
        {
            // 更新所有法术UI槽
            for (int i = 0; i < playerSpellCaster.spells.Count && i < container.spellUIs.Length; i++)
            {
                if (playerSpellCaster.spells[i] != null)
                {
                    container.spellUIs[i].SetActive(true);
                    SpellUI spellUI = container.spellUIs[i].GetComponent<SpellUI>();
                    if (spellUI != null)
                    {
                        spellUI.SetSpell(playerSpellCaster.spells[i]);
                    }
                }
            }
        }
    }

    void OnNextWaveClicked()
    {
        // 隐藏奖励界面
        if (rewardUI != null)
            rewardUI.SetActive(false);

        // 禁用按钮，防止多次点击
        if (acceptSpellButton != null)
            acceptSpellButton.interactable = false;
        if (nextWaveButton != null)
            nextWaveButton.interactable = false;

        // 开始下一波
        if (spawner != null)
            spawner.NextWave();
    }
}