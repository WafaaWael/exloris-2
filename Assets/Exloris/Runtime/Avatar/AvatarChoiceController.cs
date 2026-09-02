using System;
using Exloris.Core.Navigation;
using Exloris.Core.Session;

namespace Exloris.Runtime.Avatar
{
    public sealed class AvatarChoiceController
    {
        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IAvatarChoiceView _view;
        private readonly ILeaveConfirmationSink _leaveSink;
        private readonly IAvatarOrderSource _orderSource;
        private bool _hasOrder;
        private AvatarChoice _leftChoice;
        private long _previewToken;

        public AvatarChoiceController(
            SessionState session,
            NavigationAuthority navigation,
            IAvatarChoiceView view,
            ILeaveConfirmationSink leaveSink,
            IAvatarOrderSource orderSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _leaveSink = leaveSink ?? throw new ArgumentNullException(nameof(leaveSink));
            _orderSource = orderSource ?? throw new ArgumentNullException(nameof(orderSource));
        }

        public AvatarPresentationState PresentationState { get; private set; }
        public AvatarChoice? PreviewChoice { get; private set; }
        public long CurrentPreviewToken => _previewToken;

        public bool EnterSelect()
        {
            if (!CanPresent(RouteId.AvatarSelect))
            {
                return false;
            }

            if (_hasOrder)
            {
                _leftChoice = Other(_leftChoice);
            }
            else
            {
                _leftChoice = _orderSource.ChooseInitialLeft();
                if (!IsValid(_leftChoice))
                {
                    throw new InvalidOperationException("Avatar order source returned an invalid choice.");
                }

                _hasOrder = true;
            }

            PreviewChoice = null;
            PresentationState = AvatarPresentationState.Select;
            _view.ShowSelect(_leftChoice, Other(_leftChoice));
            return true;
        }

        public bool HandleChoice(AvatarChoice choice, double nowSeconds)
        {
            if (PresentationState != AvatarPresentationState.Select
                || !IsValid(choice)
                || !CanPresent(RouteId.AvatarSelect))
            {
                return false;
            }

            var request = _navigation.Request(RouteId.AvatarPreview, nowSeconds);
            if (!request.Accepted)
            {
                return false;
            }

            PreviewChoice = choice;
            _previewToken++;
            PresentationState = AvatarPresentationState.Transitioning;
            return true;
        }

        public bool EnterPreview()
        {
            if (!PreviewChoice.HasValue || !CanPresent(RouteId.AvatarPreview))
            {
                return false;
            }

            PresentationState = AvatarPresentationState.Preview;
            _view.ShowPreview(PreviewChoice.Value, _previewToken);
            return true;
        }

        public bool HandleOther(double nowSeconds)
        {
            if (PresentationState != AvatarPresentationState.Preview
                || !CanPresent(RouteId.AvatarPreview))
            {
                return false;
            }

            var request = _navigation.RequestReturn(RouteId.AvatarSelect, nowSeconds);
            if (!request.Accepted)
            {
                return false;
            }

            PreviewChoice = null;
            PresentationState = AvatarPresentationState.Transitioning;
            return true;
        }

        public bool HandleRepeat()
        {
            if (PresentationState != AvatarPresentationState.Preview
                || !PreviewChoice.HasValue
                || !CanPresent(RouteId.AvatarPreview))
            {
                return false;
            }

            _previewToken++;
            _view.ShowPreview(PreviewChoice.Value, _previewToken);
            return true;
        }

        public bool HandleConfirm(double nowSeconds)
        {
            if (PresentationState != AvatarPresentationState.Preview
                || !PreviewChoice.HasValue
                || !CanPresent(RouteId.AvatarPreview))
            {
                return false;
            }

            var request = _navigation.Request(RouteId.JourneyOverview, nowSeconds);
            if (!request.Accepted)
            {
                return false;
            }

            _session.SelectAvatar(TokenFor(PreviewChoice.Value));
            PresentationState = AvatarPresentationState.Transitioning;
            return true;
        }

        public bool HandleHome()
        {
            RouteId caller;
            switch (PresentationState)
            {
                case AvatarPresentationState.Select:
                    caller = RouteId.AvatarSelect;
                    break;
                case AvatarPresentationState.Preview:
                    caller = RouteId.AvatarPreview;
                    break;
                default:
                    return false;
            }

            return CanPresent(caller) && _leaveSink.TryShow(caller);
        }

        private bool CanPresent(RouteId route)
        {
            return _session.IsOpen
                && _session.CurrentOpenSessionLocation == route
                && _navigation.CurrentRoute == route
                && !_navigation.IsLocked
                && _navigation.Recovery == null;
        }

        private static bool IsValid(AvatarChoice choice)
        {
            return choice == AvatarChoice.Boy || choice == AvatarChoice.Girl;
        }

        private static AvatarChoice Other(AvatarChoice choice)
        {
            return choice == AvatarChoice.Boy ? AvatarChoice.Girl : AvatarChoice.Boy;
        }

        private static string TokenFor(AvatarChoice choice)
        {
            return choice == AvatarChoice.Boy ? "avatar_boy" : "avatar_girl";
        }
    }
}
