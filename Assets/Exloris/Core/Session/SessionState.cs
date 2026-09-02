using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;

namespace Exloris.Core.Session
{
    public sealed class SessionState
    {
        private readonly List<ObservationEvent> _observations = new List<ObservationEvent>();

        public SessionState()
        {
            SoundEnabled = true;
        }

        public string SelectedAvatarId { get; private set; }
        public bool IsOpen { get; private set; }
        public RouteId CurrentOpenSessionLocation { get; private set; }
        public bool SoundEnabled { get; set; }
        public IReadOnlyList<ObservationEvent> Observations => _observations;

        public bool Begin(RouteId initialLocation)
        {
            if (!CanOwnOpenSession(initialLocation))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialLocation),
                    initialLocation,
                    "The route cannot own an open child session.");
            }

            if (IsOpen)
            {
                return false;
            }

            IsOpen = true;
            CurrentOpenSessionLocation = initialLocation;
            return true;
        }

        private static bool CanOwnOpenSession(RouteId route)
        {
            switch (route)
            {
                case RouteId.AvatarSelect:
                case RouteId.AvatarPreview:
                case RouteId.JourneyOverview:
                case RouteId.Boat1:
                case RouteId.Stage1:
                case RouteId.Boat2:
                case RouteId.Stage2:
                case RouteId.Boat3:
                case RouteId.Stage3:
                case RouteId.Completion:
                    return true;
                default:
                    return false;
            }
        }

        public bool MoveTo(RouteId location)
        {
            if (!CanOwnOpenSession(location))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(location),
                    location,
                    "The route cannot own an open child session.");
            }

            if (!IsOpen)
            {
                return false;
            }

            CurrentOpenSessionLocation = location;
            return true;
        }

        public void SelectAvatar(string avatarId)
        {
            if (!string.Equals(avatarId, "avatar_boy", StringComparison.Ordinal)
                && !string.Equals(avatarId, "avatar_girl", StringComparison.Ordinal))
            {
                throw new ArgumentException("Avatar ID must be avatar_boy or avatar_girl.", nameof(avatarId));
            }

            SelectedAvatarId = avatarId;
        }

        public void Record(ObservationEvent observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            _observations.Add(observation);
        }

        public void Reset()
        {
            SelectedAvatarId = null;
            IsOpen = false;
            CurrentOpenSessionLocation = RouteId.StartupLoading;
            SoundEnabled = true;
            _observations.Clear();
        }
    }
}
