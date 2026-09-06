using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string LevelKey = "SavedLevel";
    private const string StepKey = "SavedStep";

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

    public void SaveProgress(int level, int step)
    {
        PlayerPrefs.SetInt(LevelKey, Mathf.Max(1, level));
        PlayerPrefs.SetInt(StepKey, Mathf.Max(1, step));
        PlayerPrefs.Save();
    }

    public void LoadProgress(out int level, out int step)
    {
        level = PlayerPrefs.GetInt(LevelKey, 1);
        step = PlayerPrefs.GetInt(StepKey, 1);
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(LevelKey);
        PlayerPrefs.DeleteKey(StepKey);
        PlayerPrefs.Save();
    }
}
