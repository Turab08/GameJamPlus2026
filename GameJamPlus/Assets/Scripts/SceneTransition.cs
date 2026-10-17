using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    public ParticleSystem transitionFX;
    public void TransitionToNextScene()
    {
        PlayTransitionFX();

        Invoke("LoadNextScene", 1f); 
    }

    private void LoadNextScene()
    {
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = (currentSceneIndex + 1) % UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
    }


    void PlayTransitionFX()
    {
        transitionFX.Play();

    }
}
