using System.IO;
using System.Xml.Linq;

namespace Exloris.Editor
{
    public static class FoundationIOSExportValidator
    {
        public static void Validate(string exportRoot)
        {
            var plistPath = Path.Combine(exportRoot, "Info.plist");
            if (!File.Exists(plistPath))
            {
                throw new InvalidDataException($"iOS export Info.plist is missing: {plistPath}");
            }

            XDocument document;
            try
            {
                document = XDocument.Load(plistPath, LoadOptions.None);
            }
            catch (System.Exception exception)
            {
                throw new InvalidDataException("iOS export Info.plist is not valid XML.", exception);
            }

            var dictionary = document.Root?.Element("dict");
            if (dictionary == null)
            {
                throw new InvalidDataException("iOS export Info.plist must contain an application dictionary.");
            }

            var content = dictionary.ToString(SaveOptions.DisableFormatting);
            if (!content.Contains("<key>CFBundleIdentifier</key>") ||
                !content.Contains("<key>CFBundleExecutable</key>"))
            {
                throw new InvalidDataException("iOS export Info.plist is missing required application keys.");
            }
        }
    }
}
