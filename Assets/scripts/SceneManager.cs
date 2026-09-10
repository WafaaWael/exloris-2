using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using UnityEngine.SceneManagement;
public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }
    public static event System.Action<Scene, LoadSceneMode> sceneLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        UnitySceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int buildIndex)
    {
        UnitySceneManager.LoadScene(buildIndex);
    }

    public void ReloadCurrentScene()
    {
        LoadScene(UnitySceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextScene()
    {
        int nextBuildIndex = UnitySceneManager.GetActiveScene().buildIndex + 1;

        if (nextBuildIndex >= UnitySceneManager.sceneCountInBuildSettings)
        {
            nextBuildIndex = 0;
        }

        LoadScene(nextBuildIndex);
    }
}
