using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUIManager : MonoBehaviour
{
    public GameObject gameOverUI;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI currentWaveText;
    public TextMeshProUGUI totalEnemiesKilledText;
    public Button returnButton;

    private GameManager.GameState prevState;

    void Start()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (returnButton != null) returnButton.onClick.AddListener(ReturnToMenu);

        prevState = GameManager.Instance.state;
    }

    void Update()
    {
        var state = GameManager.Instance.state;

        if (state == prevState) return;

        if (state == GameManager.GameState.GAMEOVER && GameManager.Instance.IsPlayerDead)
        {
            ShowGameOver();
        }
        else
        {
            if (gameOverUI != null)
                gameOverUI.SetActive(false);
        }

        prevState = state;
    }

    void ShowGameOver()
    {
        if (titleText != null)
            titleText.text = "Game Over!";

        if (currentWaveText != null)
            currentWaveText.text = $"Total Waves Completed: {GameManager.Instance.wavesCompleted}";

        if (totalEnemiesKilledText != null)
            totalEnemiesKilledText.text = $"Enemies You Killed: {GameManager.Instance.totalEnemiesKilled}";

        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }

    void ReturnToMenu()
    {
        var cam = Camera.main;
        if (cam != null)
            Destroy(cam.gameObject);

        GameManager.Instance.ResetGame();
        SceneManager.LoadScene("Main");
    }
}
