using System;
using UnityEngine;

namespace Exloris.Runtime.Media
{
    [DisallowMultipleComponent]
    public sealed class AudioConfigurationMonitor : MonoBehaviour
    {
        private MediaPlaybackCoordinator _coordinator;
        private bool _subscribed;

        public event Action OutputRouteChanged;

        public void Configure(MediaPlaybackCoordinator coordinator)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            Subscribe();
        }

        public void HandleConfigurationChanged(bool deviceWasChanged)
        {
            if (deviceWasChanged)
            {
                _coordinator?.HandleOutputRouteChanged();
                OutputRouteChanged?.Invoke();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _coordinator == null)
            {
                return;
            }

            AudioSettings.OnAudioConfigurationChanged += HandleConfigurationChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            AudioSettings.OnAudioConfigurationChanged -= HandleConfigurationChanged;
            _subscribed = false;
        }
    }
}
