using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    public static ButtonManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void Menu()
    {
        SceneManager.LoadScene(0);
    }
}
