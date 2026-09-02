using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Avatar
{
    internal sealed class AvatarControlView
    {
        public AvatarControlView(Image targetImage, Image visibleArt, SemanticControl semantic)
        {
            TargetImage = targetImage;
            VisibleArt = visibleArt;
            Semantic = semantic;
        }

        public Image TargetImage { get; }
        public RectTransform Target => TargetImage.rectTransform;
        public Image VisibleArt { get; }
        public SemanticControl Semantic { get; }
        public Rect CanvasRect { get; set; }
    }

    internal static class AvatarViewFactory
    {
        public static GameObject CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var owner = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            owner.transform.SetParent(parent, false);
            var canvas = owner.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = owner.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return owner;
        }

        public static RectTransform CreateSafeRoot(GameObject canvas)
        {
            var root = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Stretch(root);
            canvas.AddComponent<SafeAreaController>().Configure(root, Vector4.zero);
            return root;
        }

        public static ResponsiveBackgroundPresenter CreateBackground(
            Transform parent, Sprite fourByThree, Sprite sixteenByNine, Sprite ultraWide)
        {
            var owner = new GameObject(
                "AspectSpecificBackground", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(ResponsiveBackgroundPresenter));
            owner.transform.SetParent(parent, false);
            Stretch(owner.GetComponent<RectTransform>());
            var presenter = owner.GetComponent<ResponsiveBackgroundPresenter>();
            presenter.Configure(fourByThree, sixteenByNine, ultraWide);
            return presenter;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            owner.transform.SetParent(parent, false);
            var image = owner.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        public static AvatarControlView CreateControl(
            string name, Transform parent, Sprite sprite, string semanticId)
        {
            var target = CreateImage(name + ".TouchArea", parent, Color.clear, null);
            var art = CreateImage(name + ".VisibleArt", target.transform, Color.white, sprite);
            art.preserveAspect = true;
            var semantic = new SemanticControl(semanticId, () => ToScreenRect(target.rectTransform));
            return new AvatarControlView(target, art, semantic);
        }

        public static TextMeshProUGUI CreateRuntimeText(string name, Transform parent)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            owner.transform.SetParent(parent, false);
            var text = owner.GetComponent<TextMeshProUGUI>();
            text.font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-SemiBold SDF");
            if (text.font == null) throw new InvalidOperationException("Approved Noto TMP font asset is missing.");
            text.text = string.Empty;
            text.fontSize = 46f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return text;
        }

        public static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchored(rect, new Vector2(0.5f, 0.5f), position, size);
        }

        public static void SetTop(AvatarControlView control, Vector2 position, float target)
        {
            SetAnchored(control.Target, new Vector2(0.5f, 1f), position, new Vector2(target, target));
            SetAnchored(control.VisibleArt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(target * 0.72f, target * 0.72f));
        }

        public static void SetBottom(AvatarControlView control, Vector2 position, float target)
        {
            SetAnchored(control.Target, new Vector2(0.5f, 0f), position, new Vector2(target, target));
            SetAnchored(control.VisibleArt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(target * 0.72f, target * 0.72f));
        }

        public static void SetAvatarArt(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.08f);
            rect.anchoredPosition = new Vector2(0f, size.y * 0.08f);
            rect.sizeDelta = size;
        }

        public static Rect ToScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static bool TargetsOverlap(IEnumerable<AvatarControlView> controls)
        {
            var list = new List<AvatarControlView>(controls);
            for (var first = 0; first < list.Count; first++)
            for (var second = first + 1; second < list.Count; second++)
                if (list[first].CanvasRect.Overlaps(list[second].CanvasRect)) return true;
            return false;
        }

        public static bool Inside(IEnumerable<AvatarControlView> controls, Vector2 safeSize)
        {
            foreach (var control in controls)
            {
                var rect = control.CanvasRect;
                if (rect.xMin < 0f || rect.yMin < 0f || rect.xMax > safeSize.x || rect.yMax > safeSize.y)
                    return false;
            }
            return true;
        }

        public static Rect Inset(Rect rect, float fraction)
        {
            return new Rect(
                rect.x + rect.width * fraction,
                rect.y + rect.height * fraction,
                rect.width * (1f - 2f * fraction),
                rect.height * (1f - 2f * fraction));
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
