using System.IO;
using Exloris.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Editor
{
    public static class FoundationOverlayPrefabGenerator
    {
        private const string OutputFolder = "Assets/Exloris/UI/Overlays";

        public static void Generate()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Exloris/UI/Overlays"));
            SaveLoading();
            SaveRecovery();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SaveLoading()
        {
            var root = CreateOverlayRoot("LoadingOverlay", new Color32(23, 107, 115, 235));
            var icon = CreateRect("LoadingSymbol", root.transform, new Vector2(168f, 168f), Vector2.zero);
            var graphic = icon.AddComponent<SemanticIconGraphic>();
            graphic.IconKind = SemanticIconKind.Loading;
            graphic.color = new Color32(255, 249, 238, 255);
            graphic.raycastTarget = false;
            SaveAndDestroy(root, $"{OutputFolder}/LoadingOverlay.prefab");
        }

        private static void SaveRecovery()
        {
            var root = CreateOverlayRoot("RecoveryOverlay", new Color32(51, 43, 36, 210));
            var panel = CreateRect("Panel", root.transform, new Vector2(720f, 420f), Vector2.zero);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color32(243, 228, 197, 255);
            panelImage.raycastTarget = true;

            CreateAction("Retry", panel.transform, new Vector2(-170f, -65f), SemanticIconKind.Retry);
            CreateAction("Home", panel.transform, new Vector2(170f, -65f), SemanticIconKind.Home);
            SaveAndDestroy(root, $"{OutputFolder}/RecoveryOverlay.prefab");
        }

        private static GameObject CreateOverlayRoot(string name, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            var image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return root;
        }

        private static void CreateAction(
            string name,
            Transform parent,
            Vector2 position,
            SemanticIconKind iconKind)
        {
            var target = CreateRect(name, parent, new Vector2(224f, 224f), position);
            var image = target.AddComponent<Image>();
            image.color = new Color32(159, 64, 53, 255);
            var button = target.AddComponent<Button>();
            button.targetGraphic = image;

            var icon = CreateRect($"{name}Icon", target.transform, new Vector2(120f, 120f), Vector2.zero);
            var graphic = icon.AddComponent<SemanticIconGraphic>();
            graphic.IconKind = iconKind;
            graphic.color = new Color32(255, 249, 238, 255);
            graphic.raycastTarget = false;
        }

        private static GameObject CreateRect(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return child;
        }

        private static void SaveAndDestroy(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
