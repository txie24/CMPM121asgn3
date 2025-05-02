using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;


public class GameWinUIManager : MonoBehaviour
{
    public GameObject gameWinUI;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI totalEnemiesKilledText;
    public Button returnButton;

    private GameManager.GameState prevState;

    void Start()
    {
        if (gameWinUI != null) gameWinUI.SetActive(false);
        if (returnButton != null) returnButton.onClick.AddListener(ReturnToMenu);

        prevState = GameManager.Instance.state;
    }

    void Update()
    {
        var state = GameManager.Instance.state;

        if (state == prevState) return;

        if (state == GameManager.GameState.GAMEOVER && !GameManager.Instance.IsPlayerDead)
        {
            ShowVictory();
        }
        else
        {
            if (gameWinUI != null)
                gameWinUI.SetActive(false);
        }

        prevState = state;
    }

    void ShowVictory()
    {
        if (titleText != null)
            titleText.text = "You Won!";

        if (totalEnemiesKilledText != null)
            totalEnemiesKilledText.text = $"Enemies You Killed: {GameManager.Instance.totalEnemiesKilled}";

        if (gameWinUI != null)
            gameWinUI.SetActive(true);
    }

    void ReturnToMenu()
    {


        GameManager.Instance.ResetGame();
        SceneManager.LoadScene("Main");
    }

}
