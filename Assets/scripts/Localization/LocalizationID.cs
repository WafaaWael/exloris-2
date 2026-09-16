using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public class LocalizationID : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private LocalizedString localizedString = new LocalizedString();
    [SerializeField] private LocalizedAudioClip localizedAudio = new LocalizedAudioClip();

    public string StringID => GetEntryKey(localizedString.TableEntryReference);
    public string AudioID => GetEntryKey(localizedAudio.TableEntryReference);

    private static string GetEntryKey(TableEntryReference entryReference)
    {
        if (!string.IsNullOrEmpty(entryReference.Key))
        {
            return entryReference.Key;
        }

        return entryReference.KeyId == 0 ? string.Empty : entryReference.KeyId.ToString();
    }
    private void OnEnable()
    {
        localizedString.StringChanged += UpdateText;
        localizedAudio.AssetChanged += UpdateAudio;
    }
    private void OnDisable()
    {
        localizedString.StringChanged -= UpdateText;
        localizedAudio.AssetChanged -= UpdateAudio;
    }
    private void UpdateText(string text)
    {
        if (textMeshProUGUI != null)
        {
            textMeshProUGUI.text = text;
        }
    }
    private void UpdateAudio(AudioClip audio)
    {
        // Handle audio update
    }
}
