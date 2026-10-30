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
        private const string ProgressFile = "progress.json";
        private const string ChunksFile = "chunks.json";

        private readonly Dictionary<Vector2Int, ChunkSaveData> _chunks = new();
        public Dictionary<Vector2Int, ChunkSaveData> Chunks => _chunks;
        public PlayerProgress Progress { get; set; }
        public readonly Subject<Vector2Int> OnChunkUpdated = new();

        // Stress test runs use a sandbox so they never overwrite the player's saves.
        public static string SaveDirectory =>
            StressTest.IsActive ? StressTest.SaveDirectory : Application.persistentDataPath;

        public void SaveChunk(Vector2Int position, ChunkSaveData data)
        {
            _chunks[position] = data;
            OnChunkUpdated.OnNext(position);
        }

        public void Save()
        {
            using var marker = EcoProfilerMarkers.SaveGame.Auto();

            SaveChunks();
            SavePlayerProgress();
        }

        private void SaveChunks()
        {
            // Convert Vector2Int -> string (e.g., "q,r")
            var serializableDict = new Dictionary<string, ChunkSaveData>();
            foreach (var pair in _chunks)
            {
                serializableDict[$"{pair.Key.x},{pair.Key.y}"] = pair.Value;
            }

            // Unindented: a third of the size, and much less to parse and allocate on load
            string json = JsonConvert.SerializeObject(serializableDict, Formatting.None);
            System.IO.File.WriteAllText($"{SaveDirectory}/{ChunksFile}", json);
        }

        private void SavePlayerProgress()
        {
            string json = JsonConvert.SerializeObject(Progress, Formatting.Indented);
            System.IO.File.WriteAllText($"{SaveDirectory}/{ProgressFile}", json);
        }

        public void Load()
        {
            using var marker = EcoProfilerMarkers.LoadGame.Auto();

            LoadPlayerProgress();
            LoadChunks();
        }

        private void LoadPlayerProgress()
        {
            string path = $"{SaveDirectory}/{ProgressFile}";

            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                Progress = JsonConvert.DeserializeObject<PlayerProgress>(json);
            }
            else
            {
                Progress = new PlayerProgress();
            }
        }

        private void LoadChunks()
        {
            string path = $"{SaveDirectory}/{ChunksFile}";
            _chunks.Clear();

            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var serializableDict = JsonConvert.DeserializeObject<Dictionary<string, ChunkSaveData>>(json);

                foreach (var pair in serializableDict)
                {
                    var parts = pair.Key.Split(',');
                    var pos = new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
                    _chunks[pos] = pair.Value;
                }
            }
        }

        public static void DeleteProgress()
        {
            var path = SaveDirectory;
            try
            {
                DeleteFile(System.IO.Path.Combine(path, ProgressFile));
                DeleteFile(System.IO.Path.Combine(path, ChunksFile));

                // Files of the old multi-world saves
                foreach (var pattern in new[] { "field_tiles_*.json", "world_progress_*.json" })
                {
                    foreach (var file in System.IO.Directory.GetFiles(path, pattern))
                    {
                        System.IO.File.Delete(file);
                    }
                }

                PlayerPrefs.DeleteKey("last_world_id");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to delete save files in '{path}': {ex}");
            }
        }

        private static void DeleteFile(string path)
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }

        [System.Serializable]
        public class PlayerProgress
        {
            public AlphabeticNotation currency;
            public string selectedHat;
            public string selectedPet;

            // Null in a new game: the player starts at the world's spawn hex
            public Vector3Serializable? playerPosition;
            public float wetness;
            public Dictionary<string, int> UpgradeLevels = new();
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
