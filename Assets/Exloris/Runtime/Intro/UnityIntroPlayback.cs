using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace Exloris.Runtime.Intro
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class UnityIntroPlayback : MonoBehaviour, IIntroPlayback
    {
        private VideoPlayer _video;
        private AudioSource _voice;
        private StartupIntroContent _content;
        private RenderTexture _renderTarget;
        private Guid _currentToken;
        private bool _prepared;

        public event Action<Guid, IntroPreparedMedia> Prepared;
        public event Action<Guid> Completed;
        public event Action<Guid, string> Failed;
        public event Action<Guid, long, double> FramePresented;

        public Texture VideoTexture => _renderTarget;
        public double CurrentTimeSeconds => _video == null ? 0d : _video.time;
        public AudioClip VoiceClip => _voice == null ? null : _voice.clip;
        public double LastVideoStartDspTime { get; private set; }
        public double LastVoiceStartDspTime { get; private set; }
        public long LastPreparedFrameCount { get; private set; }
        public long LastPresentedFrameIndex { get; private set; } = -1;
        public int CompletionEventCount { get; private set; }
        public bool VoiceIsPlaying => _voice != null && _voice.isPlaying;

        private void Awake()
        {
            _video = GetComponent<VideoPlayer>();
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.loop = false;
            _voice.volume = 0.6f;

            _video.playOnAwake = false;
            _video.isLooping = false;
            _video.skipOnDrop = false;
            _video.playbackSpeed = 1f;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.audioOutputMode = VideoAudioOutputMode.None;
            _video.sendFrameReadyEvents = true;
            _video.prepareCompleted += HandlePrepareCompleted;
            _video.loopPointReached += HandleLoopPointReached;
            _video.errorReceived += HandleErrorReceived;
            _video.frameReady += HandleFrameReady;
        }

        private void OnDestroy()
        {
            if (_video == null) return;
            _video.prepareCompleted -= HandlePrepareCompleted;
            _video.loopPointReached -= HandleLoopPointReached;
            _video.errorReceived -= HandleErrorReceived;
            _video.frameReady -= HandleFrameReady;
            ReleaseRenderTarget();
        }

        public void Configure(StartupIntroContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _voice.clip = content.Voice;
        }

        public void Prepare(Guid tokenId, IntroMediaVariant variant)
        {
            if (_content == null) throw new InvalidOperationException("Intro playback has no approved content set.");
            if (tokenId == Guid.Empty) throw new ArgumentException("Playback token is required.", nameof(tokenId));
            if (variant == null) throw new ArgumentNullException(nameof(variant));

            _currentToken = tokenId;
            _prepared = false;
            EnsureRenderTarget(variant.VideoWidth, variant.VideoHeight);
            _video.source = VideoSource.Url;
            _video.url = Path.Combine(Application.streamingAssetsPath, "Exloris/Intro", variant.FileName);
            _video.Prepare();
        }

        private void EnsureRenderTarget(int width, int height)
        {
            if (_renderTarget != null &&
                _renderTarget.width == width &&
                _renderTarget.height == height)
            {
                _video.targetTexture = _renderTarget;
                return;
            }

            ReleaseRenderTarget();
            _renderTarget = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "IntroVideoRenderTarget",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _renderTarget.Create();
            _video.targetTexture = _renderTarget;
        }

        private void ReleaseRenderTarget()
        {
            if (_renderTarget == null) return;
            if (_video != null && _video.targetTexture == _renderTarget) _video.targetTexture = null;
            _renderTarget.Release();
            Destroy(_renderTarget);
            _renderTarget = null;
        }

        public void Play(Guid tokenId)
        {
            if (!_prepared || tokenId != _currentToken) return;
            var start = AudioSettings.dspTime;
            LastVideoStartDspTime = start;
            LastVoiceStartDspTime = start;
            _video.Play();
            _voice.Play();
        }

        public void Pause(Guid tokenId)
        {
            if (tokenId != _currentToken) return;
            _video.Pause();
            _voice.Pause();
        }

        public void Resume(Guid previousTokenId, Guid replacementTokenId)
        {
            if (previousTokenId != _currentToken || replacementTokenId == Guid.Empty) return;
            _currentToken = replacementTokenId;
            _video.Play();
            _voice.UnPause();
        }

        public void Stop(Guid tokenId)
        {
            if (tokenId != _currentToken) return;
            _video.Stop();
            _voice.Stop();
            _prepared = false;
            _currentToken = Guid.Empty;
        }

        private void HandlePrepareCompleted(VideoPlayer source)
        {
            _prepared = true;
            LastPreparedFrameCount = (long)source.frameCount;
            Prepared?.Invoke(
                _currentToken,
                new IntroPreparedMedia(source.frameRate, (long)source.frameCount, source.length));
        }

        private void HandleLoopPointReached(VideoPlayer source)
        {
            CompletionEventCount++;
            Completed?.Invoke(_currentToken);
        }

        private void HandleErrorReceived(VideoPlayer source, string message)
        {
            Failed?.Invoke(_currentToken, message ?? "Unknown video error");
        }

        private void HandleFrameReady(VideoPlayer source, long frameIndex)
        {
            LastPresentedFrameIndex = frameIndex;
            FramePresented?.Invoke(_currentToken, frameIndex, source.time);
        }
    }
}
