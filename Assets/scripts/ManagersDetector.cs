using UnityEngine;

public class ManagersDetector : MonoBehaviour
{
    private SceneManager _sceneManager;
    private SoundManager _soundManager;

    private void Awake()
    {
        _sceneManager = Object.FindAnyObjectByType<SceneManager>();
        if (_sceneManager == null)
        {
            Debug.LogError("SceneManager not found in the scene.");
        }
        _soundManager = Object.FindAnyObjectByType<SoundManager>();
        if (_soundManager == null)
        {
            Debug.LogError("SoundManager not found in the scene.");
        }
    }

    public void LoadScene(string sceneName)
    {
        if (_sceneManager != null)
        {
            _sceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Cannot load scene because SceneManager is not found.");
        }
    }
    public void LoadScene(int sceneIndex)
    {
        if (_sceneManager != null)
        {
            _sceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError("Cannot load scene because SceneManager is not found.");
        }
    }
    public void PlaySound(string soundName)
    {
        if (_soundManager != null)
        {
            _soundManager.PlaySound(soundName);
        }
        else
        {
            Debug.LogError("Cannot play sound because SoundManager is not found.");
        }
    }
    public void StopAllSound()
    {
        if (_soundManager != null)
        {
            _soundManager.StopAllSounds();
        }
        else
        {
            Debug.LogError("Cannot stop sound because SoundManager is not found.");
        }
    }
}
