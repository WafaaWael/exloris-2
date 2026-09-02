using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Exloris.Runtime.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class FoundationEvidenceCapture : MonoBehaviour
    {
        public static bool TryGetCapturePath(string[] arguments, out string path)
        {
            path = null;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (!string.Equals(arguments[index], "-foundationCapture", StringComparison.Ordinal)) continue;
                if (string.IsNullOrWhiteSpace(arguments[index + 1])) return false;
                path = arguments[index + 1];
                return true;
            }

            return false;
        }

        private IEnumerator Start()
        {
            if (!TryGetCapturePath(Environment.GetCommandLineArgs(), out var path)) yield break;

            yield return null;
            yield return new WaitForEndOfFrame();
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false);
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
            if (!File.Exists(path))
            {
                Debug.LogError($"Foundation evidence capture was not written: {path}");
                yield break;
            }

            Debug.Log($"Foundation evidence capture written: {path}");
            Application.Quit();
        }
    }
}
