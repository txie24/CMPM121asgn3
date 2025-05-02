using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI currentWaveText;
    public TextMeshProUGUI nextWaveText;
    public TextMeshProUGUI enemiesKilledText;

    public Button nextWaveButton;

    private EnemySpawner spawner;
    private GameManager.GameState prevState;
    private Coroutine rewardCoroutine;

    void Start()
    {
        spawner = Object.FindFirstObjectByType<EnemySpawner>();

        if (rewardUI != null) rewardUI.SetActive(false);
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

        if (titleText != null)
            titleText.text = "You Survived!";

        if (currentWaveText != null)
            currentWaveText.text = $"Current Wave: {spawner.currentWave - 1}";

        if (nextWaveText != null)
            nextWaveText.text = $"Next Wave: {spawner.currentWave}";

        if (enemiesKilledText != null)
            enemiesKilledText.text = $"Enemies Killed: {spawner.lastWaveEnemyCount}";

        if (rewardUI != null)
            rewardUI.SetActive(true);

        if (nextWaveButton != null)
            nextWaveButton.interactable = true;
    }

    void OnNextWaveClicked()
    {
        if (rewardUI != null)
            rewardUI.SetActive(false);

        if (nextWaveButton != null)
            nextWaveButton.interactable = false;

        if (spawner != null)
            spawner.NextWave();
    }
}
