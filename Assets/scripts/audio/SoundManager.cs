using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices; // For iOS native calls
using UnityEngine.SceneManagement;

[Serializable]
public struct SoundData
{
	public string key;
	public AudioClip clip;
	[Range(0f, 1f)] public float volume;
}

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance { get; private set; }

	[Header("Audio Settings")]
	[SerializeField] private AudioSource audioSourcePrefab;
	[SerializeField] private List<SoundData> soundList = new List<SoundData>();

	private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
	public bool isMuted = false;

	public static event Action OnSoundPlayed;

	void OnEnable()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	void OnDisable()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		StopAllSounds();
	}


	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);

#if UNITY_IOS
            InitializeIOSAudio();
#elif UNITY_ANDROID
            InitializeAndroidAudio();
#endif
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		// Load all sounds
		foreach (var sound in soundList)
		{
			AddAudioClip(sound.key, sound.clip, sound.volume);
		}
	}

	/// <summary>
	/// Ensures audio plays correctly on Android.
	/// </summary>
	private void InitializeAndroidAudio()
	{
#if UNITY_ANDROID
        AudioListener.pause = false;
        AudioListener.volume = 1.0f;
#endif
	}

	/// <summary>
	/// Initializes iOS audio session.
	/// </summary>
	private void InitializeIOSAudio()
	{
#if UNITY_IOS
        _SetAudioSessionCategory();
#endif
	}

	public void AddAudioClip(string key, AudioClip clip, float volume = 1f)
	{
		if (!audioSources.ContainsKey(key) && clip != null)
		{
			AudioSource newSource = Instantiate(audioSourcePrefab, transform);
			newSource.clip = clip;
			newSource.volume = volume;
			newSource.playOnAwake = false;
			newSource.loop = false;
			audioSources[key] = newSource;
		}
	}

	public void PlaySound(string key)
	{
		if (isMuted || !audioSources.ContainsKey(key) || audioSources[key] == null)
			return;

#if UNITY_IOS
        RequestAudioFocus();
#elif UNITY_ANDROID
        RequestAndroidAudioFocus();
#endif

		audioSources[key].Play();
		OnSoundPlayed?.Invoke();
	}

	public void StopSound(string key)
	{
		if (audioSources.ContainsKey(key) && audioSources[key] != null)
		{
			audioSources[key].Stop();
		}
	}

	public void SetVolume(string key, float volume)
	{
		if (audioSources.ContainsKey(key))
		{
			audioSources[key].volume = Mathf.Clamp01(volume);
		}
	}

	public void MuteAll(bool mute)
	{
		isMuted = mute;
		foreach (var source in audioSources.Values)
		{
			if (source != null)
				source.mute = mute;
		}
	}

	public void PlayRandomSound()
	{
		if (audioSources.Count == 0 || isMuted) return;
		List<string> keys = new List<string>(audioSources.Keys);
		string randomKey = keys[UnityEngine.Random.Range(0, keys.Count)];
		PlaySound(randomKey);
	}

	public void StopAllSounds()
	{
		foreach (var source in audioSources.Values)
		{
			if (source != null)
				source.Stop();
		}
	}

	public void LoopSound(string key, bool loop)
	{
		if (!audioSources.ContainsKey(key) || audioSources[key] == null)
			return;

		audioSources[key].loop = loop;

		if (loop && !audioSources[key].isPlaying)
		{
			audioSources[key].Play();
		}
		else if (!loop)
		{
			audioSources[key].Stop();
		}
	}

	public bool IsPlaying(string key)
	{
		return audioSources.ContainsKey(key) && audioSources[key].isPlaying;
	}


	/// <summary>
	/// Ensures Android allows audio playback.
	/// </summary>
	private void RequestAndroidAudioFocus()
	{
#if UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var audioManager = activity.Call<AndroidJavaObject>("getSystemService", "audio"))
        {
            int result = audioManager.Call<int>("requestAudioFocus", null, 3, 2);
        }
#endif
	}

	/// <summary>
	/// Ensures iOS allows background sound playback.
	/// </summary>
	private void RequestAudioFocus()
	{
#if UNITY_IOS
        _SetAudioSessionCategory();
#endif
	}

	/// <summary>
	/// Native iOS function to set the AVAudioSession category.
	/// </summary>
#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void _SetAudioSessionCategory();
#endif
}