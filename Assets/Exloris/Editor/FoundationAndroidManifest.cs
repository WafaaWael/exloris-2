using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace Exloris.Editor
{
    public sealed class FoundationAndroidManifest : IPostGenerateGradleAndroidProject
    {
        private const string InternetPermission = "android.permission.INTERNET";
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("Generated Unity Android manifest was not found.", manifestPath);
            }

            var original = File.ReadAllText(manifestPath);
            var sanitized = Sanitize(original);
            File.WriteAllText(manifestPath, sanitized);
            Debug.Log("Foundation Android manifest policy removed runtime Internet permission.");
        }

        public static string Sanitize(string manifestXml)
        {
            if (string.IsNullOrWhiteSpace(manifestXml))
            {
                throw new ArgumentException("Manifest XML is required.", nameof(manifestXml));
            }

            var document = XDocument.Parse(manifestXml, LoadOptions.PreserveWhitespace);
            var root = document.Root ?? throw new InvalidDataException("Android manifest has no root element.");
            var nameAttribute = XName.Get("name", AndroidNamespace);

            foreach (var permission in root
                         .Elements("uses-permission")
                         .Where(element => string.Equals(
                             (string)element.Attribute(nameAttribute),
                             InternetPermission,
                             StringComparison.Ordinal))
                         .ToArray())
            {
                permission.Remove();
            }

            return document.ToString(SaveOptions.DisableFormatting);
        }
    }
}
