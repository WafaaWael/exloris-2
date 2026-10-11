using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using UnityEngine.SceneManagement;
public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }
    public static event System.Action<Scene, LoadSceneMode> sceneLoaded;

    [SerializeField] private float fadeDuration = 0.25f;
    private bool isLoading;

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

    public void LoadScene(string sceneName, CanvasGroup screen)
    {
        StartSceneLoad(() => UnitySceneManager.LoadSceneAsync(sceneName), screen);
    }

    public void LoadScene(int buildIndex, CanvasGroup screen)
    {
        StartSceneLoad(() => UnitySceneManager.LoadSceneAsync(buildIndex), screen);
    }

    public Tween ShowScreen(CanvasGroup screen)
    {
        if (screen == null)
        {
            return null;
        }

        screen.gameObject.SetActive(true);
        screen.DOKill();
        return screen.DOFade(1f, fadeDuration).SetUpdate(true);
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

    private void StartSceneLoad(System.Func<AsyncOperation> startLoad, CanvasGroup screen)
    {
        if (!isLoading && screen != null)
        {
            StartCoroutine(LoadSceneWithFade(startLoad, screen));
        }
    }

    private IEnumerator LoadSceneWithFade(System.Func<AsyncOperation> startLoad, CanvasGroup screen)
    {
        isLoading = true;
        yield return ShowScreen(screen).WaitForCompletion();

        float loadingStartedAt = Time.realtimeSinceStartup;
        AsyncOperation loadOperation = startLoad();
        loadOperation.allowSceneActivation = false;
        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        loadOperation.allowSceneActivation = true;
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        float loadingDuration = Time.realtimeSinceStartup - loadingStartedAt;
        yield return screen.DOFade(0f, loadingDuration).SetUpdate(true).WaitForCompletion();
        screen.gameObject.SetActive(false);
        isLoading = false;
    }
}
