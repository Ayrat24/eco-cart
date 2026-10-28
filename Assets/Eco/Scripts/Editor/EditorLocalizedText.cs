using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Reads and creates string table entries without entering play mode.</summary>
    internal static class EditorLocalizedText
    {
        private const string PreviewLocale = "en";

        /// <summary>The English text of <paramref name="text"/>, or <paramref name="fallback"/> if it has none.</summary>
        public static string Get(LocalizedString text, string fallback)
        {
            if (text == null || text.IsEmpty)
            {
                return fallback;
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(text.TableReference);
            var entry = collection != null ? collection.SharedData.GetEntryFromReference(text.TableEntryReference) : null;
            if (entry == null)
            {
                return fallback;
            }

            var table = collection.GetTable(PreviewLocale) as StringTable;
            var value = table != null ? table.GetEntry(entry.Id)?.Value : null;
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        /// <summary>
        /// Points <paramref name="text"/> at entry <paramref name="key"/> of <paramref name="tableName"/>, adding the
        /// key if it is new and filling the English value when it is empty.
        /// </summary>
        public static bool Assign(LocalizedString text, string tableName, string key, string englishValue)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                return false;
            }

            var shared = collection.SharedData;
            var entry = shared.GetEntry(key);
            if (entry == null)
            {
                Undo.RecordObject(shared, "Add Localization Key");
                entry = shared.AddKey(key);
                EditorUtility.SetDirty(shared);
            }

            if (!string.IsNullOrEmpty(englishValue) && collection.GetTable(PreviewLocale) is StringTable english)
            {
                var existing = english.GetEntry(entry.Id);
                if (existing == null || string.IsNullOrEmpty(existing.Value))
                {
                    Undo.RecordObject(english, "Set Localization Value");
                    english.AddEntry(entry.Id, englishValue);
                    EditorUtility.SetDirty(english);
                }
            }

            text.SetReference(shared.TableCollectionNameGuid, entry.Id);
            return true;
        }

        /// <summary>Saves the string tables <paramref name="text"/> points at, if they changed.</summary>
        public static void SaveTables(LocalizedString text)
        {
            if (text == null || text.IsEmpty)
            {
                return;
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(text.TableReference);
            if (collection == null)
            {
                return;
            }

            AssetDatabase.SaveAssetIfDirty(collection.SharedData);
            foreach (var table in collection.StringTables)
            {
                AssetDatabase.SaveAssetIfDirty(table);
            }
        }
    }
}
