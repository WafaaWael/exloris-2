using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exloris.Runtime.Localization;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Exloris.Editor
{
    public static class FoundationLocalizationGenerator
    {
        private const string OutputFolder = "Assets/Exloris/Content/Localization";
        private const string EnglishLocalePath = OutputFolder + "/English(en).asset";
        private const string PseudoLocalePath = OutputFolder + "/Pseudo(qps-ploc).asset";

        public static void Generate()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Exloris/Content/Localization"));
            var english = EnsureEnglishLocale();
            EnsurePseudoLocale();

            var collection = LocalizationEditorSettings.GetStringTableCollection(
                UnityStringTableResolver.TableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    UnityStringTableResolver.TableName,
                    OutputFolder,
                    new List<Locale> { english });
            }

            var table = collection.GetTable(english.Identifier) as StringTable;
            foreach (var pair in FoundationEnglishStrings.ById)
            {
                var entry = table.GetEntry(pair.Key);
                if (entry == null)
                {
                    entry = table.AddEntry(pair.Key, pair.Value);
                }

                entry.Value = pair.Value;
            }

            LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Locale EnsureEnglishLocale()
        {
            var locale = LocalizationEditorSettings.GetLocale("en");
            if (locale != null)
            {
                return locale;
            }

            locale = Locale.CreateLocale("en");
            locale.name = "English (en)";
            AssetDatabase.CreateAsset(locale, EnglishLocalePath);
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void EnsurePseudoLocale()
        {
            var pseudo = AssetDatabase.LoadAssetAtPath<PseudoLocale>(PseudoLocalePath);
            if (pseudo == null)
            {
                pseudo = PseudoLocale.CreatePseudoLocale();
                pseudo.name = "Pseudo (qps-ploc)";
                pseudo.Identifier = new LocaleIdentifier("qps-ploc");
                AssetDatabase.CreateAsset(pseudo, PseudoLocalePath);
                LocalizationEditorSettings.AddLocale(pseudo);
            }

            var expander = pseudo.Methods.OfType<Expander>().First();
            expander.ExpansionRules.Clear();
            expander.AddExpansionRule(0, 11, 1.0f);
            expander.AddExpansionRule(11, int.MaxValue, 0.35f);
            EditorUtility.SetDirty(pseudo);
        }
    }
}
