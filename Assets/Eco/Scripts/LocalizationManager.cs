// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.Localization.Settings;
// using UnityEngine.Localization.Tables;
//
// public class LocalizationManager
// {
//     private readonly Dictionary<string, StringTable> _tables = new();
//     private bool _isInitialized;
//
//     /// <summary>
//     /// Loads and caches all string tables for the current locale.
//     /// Call this once at startup.
//     /// </summary>
//     private async UniTask Initialize()
//     {
//         if (_isInitialized) return;
//
//         var db = LocalizationSettings.StringDatabase;
//
//         
//         foreach (var tableRef in db.)
//         {
//             var table = await db.GetTableAsync(tableRef);
//             if (table != null && !_tables.ContainsKey(table.TableCollectionName))
//             {
//                 _tables.Add(table.TableCollectionName, table);
//                 Debug.Log($"[LocalizationManager] Cached table: {table.TableCollectionName}");
//             }
//         }
//
//         _isInitialized = true;
//     }
//
//     /// <summary>
//     /// Get a localized string by table and key.
//     /// </summary>
//     public string Get(string tableName, string entryKey)
//     {
//         if (!_tables.TryGetValue(tableName, out var table))
//             return $"[Missing Table: {tableName}]";
//
//         var entry = table.GetEntry(entryKey);
//         if (entry == null)
//             return $"[Missing Key: {entryKey}]";
//
//         return entry.GetLocalizedString();
//     }
//
//     /// <summary>
//     /// Clear cached tables (e.g., if locale changes and you want to reload).
//     /// </summary>
//     public void ClearCache()
//     {
//         _tables.Clear();
//         _isInitialized = false;
//     }
// }