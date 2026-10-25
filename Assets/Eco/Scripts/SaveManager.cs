using System.Collections.Generic;
using Eco.Scripts.Profiling;
using Eco.Scripts.World;
using LargeNumbers;
using UnityEngine;
using Newtonsoft.Json;
using R3;

namespace Eco.Scripts
{
    public class SaveManager
    {
        private readonly Dictionary<Vector2Int, TileData[]> _fieldTiles = new();
        public Dictionary<Vector2Int, TileData[]> FieldTiles => _fieldTiles;
        public PlayerProgress Progress { get; set; }
        public readonly Subject<Vector2Int> OnChunkUpdated = new();

        private string _worldId;
        private const string LastWorldIdKey = "last_world_id";

        // Stress test runs use a sandbox so they never overwrite the player's saves.
        public static string SaveDirectory =>
            StressTest.IsActive ? StressTest.SaveDirectory : Application.persistentDataPath;

        public void SaveFieldTiles(Vector2Int position, TileData[] tiles)
        {
            _fieldTiles[position] = tiles;
            OnChunkUpdated.OnNext(position);
        }

        public void Save()
        {
            using var marker = EcoProfilerMarkers.SaveGame.Auto();

            SaveFieldTiles();
            SavePlayerProgress();

            // The stress world isn't in WorldSelector's list; remembering it would leave the real game with no world.
            if (!StressTest.IsActive)
            {
                PlayerPrefs.SetString(LastWorldIdKey, _worldId);
            }
        }
        
        private void SaveFieldTiles()
        {
            // Convert Vector2Int -> string (e.g., "x,y")
            var serializableDict = new Dictionary<string, TileData[]>();
            foreach (var pair in _fieldTiles)
            {
                serializableDict[$"{pair.Key.x},{pair.Key.y}"] = pair.Value;
            }

            // Unindented: a third of the size, and much less to parse and allocate on load
            string json = JsonConvert.SerializeObject(serializableDict, Formatting.None);
            System.IO.File.WriteAllText($"{SaveDirectory}/field_tiles_{_worldId}.json", json);
        }

        private void SavePlayerProgress()
        {
            // Saved position belongs to this world; LoadPlayerProgress resets it when another world loads
            Progress.worldId = _worldId;
            string json = JsonConvert.SerializeObject(Progress, Formatting.Indented);
            System.IO.File.WriteAllText(SaveDirectory + "/progress.json", json);
        }

        public void Load(string worldId)
        {
            using var marker = EcoProfilerMarkers.LoadGame.Auto();

            _worldId = worldId;
            LoadPlayerProgress();
            LoadFieldTiles();
        }
        
        private void LoadPlayerProgress()
        {
            string path = $"{SaveDirectory}/progress.json";

            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                Progress = JsonConvert.DeserializeObject<PlayerProgress>(json);

                if (Progress.worldId != _worldId)
                {
                    Progress.playerPosition = new Vector3Serializable(Vector3.zero);
                }
            }
            else
            {
                Progress = new PlayerProgress();
            }
        }

        private void LoadFieldTiles()
        {
            string path = $"{SaveDirectory}/field_tiles_{_worldId}.json";
            _fieldTiles.Clear();

            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var serializableDict = JsonConvert.DeserializeObject<Dictionary<string, TileData[]>>(json);

                foreach (var pair in serializableDict)
                {
                    var parts = pair.Key.Split(',');
                    var pos = new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
                    _fieldTiles[pos] = pair.Value;
                }
            }
        }
        
        public static string GetLastWorldId()
        {
            return PlayerPrefs.GetString(LastWorldIdKey, string.Empty);
        }

        public static void DeleteProgress()
        {
            var path = SaveDirectory;
            try
            {
                // Delete explicit progress file
                var progressPath = System.IO.Path.Combine(path, "progress.json");
                if (System.IO.File.Exists(progressPath))
                    System.IO.File.Delete(progressPath);

                // Delete any field_tiles files (per-world or backups)
                var fieldFiles = System.IO.Directory.GetFiles(path, "field_tiles_*.json");
                foreach (var file in fieldFiles)
                {
                    System.IO.File.Delete(file);
                }
                
                // Delete any world_progress files
                var progressFiles = System.IO.Directory.GetFiles(path, "world_progress_*.json");
                foreach (var file in progressFiles)
                {
                    System.IO.File.Delete(file);
                }
                
                PlayerPrefs.DeleteKey(LastWorldIdKey);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to delete save files in '{path}': {ex}");
            }
        }

        [System.Serializable]
        public class PlayerProgress
        {
            public AlphabeticNotation currency;
            public string selectedCart;
            public string selectedHat;
            public Vector3Serializable playerPosition;
            public Dictionary<string, int> UpgradeLevels = new();
            public string worldId;
        }
        
        [System.Serializable]
        public struct Vector3Serializable {
            public float x;
            public float y;
            public float z;

            public Vector3Serializable(Vector3 v) {
                x = v.x;
                y = v.y;
                z = v.z;
            }

            public Vector3 ToVector3() => new Vector3(x, y, z);
        }
    }
}
