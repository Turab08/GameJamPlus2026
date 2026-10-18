using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameWinPanel;

    private void Start()
    {
        Time.timeScale = 1;
    }

    public void OnGameOver()
    {
        gameOverPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.GameOverSound);
        Time.timeScale = 0f;
    }
    public void OnGameWin()
    {
        gameWinPanel.SetActive(true);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.VictorySound);
        Time.timeScale = 0f;
    }
}
