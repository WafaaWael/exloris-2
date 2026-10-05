using System;
using System.Collections.Generic;
using BoatGame.CoreGameSystems.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct SoundData
{
	public string key;
	public AudioClip clip;
	[Range(0f, 1f)] public float volume;
}

/// <summary>
/// Compatibility façade for older callers that address sounds by string key.
/// AudioService owns all AudioSources and is the single place that performs playback.
/// </summary>
public sealed class SoundManager : MonoBehaviour
{
	public static SoundManager Instance { get; private set; }
	public static event Action OnSoundPlayed;

	[Header("Audio Service")]
	[SerializeField] private AudioService audioService;

	[Header("Legacy Sound-Key Mapping")]
	[SerializeField] private List<SoundData> soundList = new List<SoundData>();
	[SerializeField] private string backgroundMusicKey = "BackGround";
	[SerializeField] private bool playBackgroundOnAwake = true;

	private readonly Dictionary<string, SoundData> sounds = new Dictionary<string, SoundData>();
	public bool isMuted = false;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		// Unity only preserves root GameObjects. This component is currently a child of the
		// main-menu "systems" object, so detach it before making the singleton persistent.
		if (transform.parent != null)
		{
			transform.SetParent(null);
		}

		DontDestroyOnLoad(gameObject);
		ResolveAudioService();

		foreach (var sound in soundList)
		{
			if (!string.IsNullOrWhiteSpace(sound.key) && sound.clip != null)
			{
				sounds[sound.key] = sound;
			}
		}

		audioService?.SetMuted(isMuted);

		if (playBackgroundOnAwake)
		{
			SetLoopMusic(backgroundMusicKey);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		ResolveAudioService();
		audioService?.SetMuted(isMuted);
	}

	public void AddAudioClip(string key, AudioClip clip, float volume = 1f)
	{
		if (string.IsNullOrWhiteSpace(key) || clip == null || sounds.ContainsKey(key))
		{
			return;
		}

		SoundData sound = new SoundData { key = key, clip = clip, volume = Mathf.Clamp01(volume) };
		sounds.Add(key, sound);
		soundList.Add(sound);
	}

	/// <summary>Sets a clip from the key map as AudioService's looping music track.</summary>
	public void SetLoopMusic(string key)
	{
		if (TryGetSound(key, out SoundData sound))
		{
			SetLoopMusic(sound.clip);
		}
	}

	/// <summary>Sets the AudioService music source to loop this clip.</summary>
	public void SetLoopMusic(AudioClip clip)
	{
		if (clip == null || !ResolveAudioService())
		{
			return;
		}

		audioService.SetLoopMusic(clip);
		OnSoundPlayed?.Invoke();
	}

	public void StopLoopMusic()
	{
		if (ResolveAudioService())
		{
			audioService.StopMusic();
		}
	}

	public void PlaySound(string key)
	{
		if (isMuted || !TryGetSound(key, out SoundData sound))
			return;

		if (key == backgroundMusicKey)
		{
			SetLoopMusic(sound.clip);
			return;
		}

		audioService.PlaySfx(sound.clip, sound.volume);
		OnSoundPlayed?.Invoke();
	}

	public void StopSound(string key)
	{
		if (key == backgroundMusicKey)
		{
			StopLoopMusic();
		}
	}

	public void SetVolume(string key, float volume)
	{
		if (!sounds.TryGetValue(key, out SoundData sound))
		{
			return;
		}

		sound.volume = Mathf.Clamp01(volume);
		sounds[key] = sound;
		for (int i = 0; i < soundList.Count; i++)
		{
			if (soundList[i].key == key)
			{
				soundList[i] = sound;
				break;
			}
		}
	}

	public void MuteAll(bool mute)
	{
		isMuted = mute;
		if (ResolveAudioService())
		{
			audioService.SetMuted(mute);
		}
	}

	public void PlayRandomSound()
	{
		if (sounds.Count == 0 || isMuted) return;
		List<string> keys = new List<string>(sounds.Keys);
		string randomKey = keys[UnityEngine.Random.Range(0, keys.Count)];
		PlaySound(randomKey);
	}

	public void StopAllSounds()
	{
		if (ResolveAudioService())
		{
			audioService.StopAll();
		}
	}

	/// <summary>
	/// Legacy API. AudioService only supports looping music here; use SetLoopMusic for new callers.
	/// </summary>
	public void LoopSound(string key, bool loop)
	{
		if (loop)
		{
			SetLoopMusic(key);
		}
		else if (key == backgroundMusicKey)
		{
			StopLoopMusic();
		}
	}

	public bool IsPlaying(string key)
	{
		return key == backgroundMusicKey && ResolveAudioService() && audioService.IsMusicPlaying;
	}

	public void PlayLocalizedClip(AudioClip clip, float volume = 1f)
	{
		if (isMuted || clip == null || !ResolveAudioService())
			return;

		audioService.PlayVoice(clip, volume);

		OnSoundPlayed?.Invoke();
	}

	private bool TryGetSound(string key, out SoundData sound)
	{
		sound = default;
		if (!ResolveAudioService())
		{
			Debug.LogWarning("SoundManager could not find an AudioService.", this);
			return false;
		}

		if (string.IsNullOrWhiteSpace(key) || !sounds.TryGetValue(key, out sound))
		{
			Debug.LogWarning($"No sound is registered with key '{key}'.", this);
			return false;
		}

		return true;
	}

	private bool ResolveAudioService()
	{
		if (audioService != null)
		{
			return true;
		}

		audioService = GetComponent<AudioService>();
		audioService ??= FindAnyObjectByType<AudioService>();
		return audioService != null;
	}
}
