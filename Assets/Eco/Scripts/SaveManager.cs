using System.Collections.Generic;
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

        public void SaveFieldTiles(Vector2Int position, TileData[] tiles)
        {
            _fieldTiles[position] = tiles;
            OnChunkUpdated.OnNext(position);
        }

        public void SaveFieldTiles()
        {
            // Convert Vector2Int -> string (e.g., "x,y")
            var serializableDict = new Dictionary<string, TileData[]>();
            foreach (var pair in _fieldTiles)
            {
                serializableDict[$"{pair.Key.x},{pair.Key.y}"] = pair.Value;
            }

            string json = JsonConvert.SerializeObject(serializableDict, Formatting.Indented);
            System.IO.File.WriteAllText(Application.persistentDataPath + "/field_tiles.json", json);
        }

        public void SavePlayerProgress()
        {
            string json = JsonConvert.SerializeObject(Progress, Formatting.Indented);
            System.IO.File.WriteAllText(Application.persistentDataPath + "/progress.json", json);
        }

        public void LoadPlayerProgress()
        {
            string path = Application.persistentDataPath + "/progress.json";

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

        public void LoadFieldTiles()
        {
            string path = Application.persistentDataPath + "/field_tiles.json";
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

        public void DeleteProgress()
        {
            string path = Application.persistentDataPath + "/field_tiles.json";
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            path = Application.persistentDataPath + "/progress.json";
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        [System.Serializable]
        public class PlayerProgress
        {
            public AlphabeticNotation currency;
            public string selectedCart;
            public Vector3Serializable playerPosition;
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
