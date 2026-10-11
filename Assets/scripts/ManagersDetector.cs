using UnityEngine;

public class ManagersDetector : MonoBehaviour
{
    private SceneManager _sceneManager;
    private SoundManager _soundManager;

    private void Awake()
    {
        //_sceneManager = Object.FindAnyObjectByType<SceneManager>();
        //if (_sceneManager == null)
        //{
        //    Debug.LogError("SceneManager not found in the scene.");
        //}
        //_soundManager = Object.FindAnyObjectByType<SoundManager>();
        //if (_soundManager == null)
        //{
        //    Debug.LogError("SoundManager not found in the scene.");
        //}
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.Instance.LoadScene(sceneName);
    }

    public void LoadScene(int sceneIndex)
    {
        SceneManager.Instance.LoadScene(sceneIndex);
    }

    public void PlaySound(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName);
    }

    public void StopAllSound()
    {
        SoundManager.Instance.StopAllSounds();
    }
}