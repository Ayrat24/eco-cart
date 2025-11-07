using UnityEngine;
using Newtonsoft.Json;

namespace Eco.Scripts.World
{
    [System.Serializable]
    public class WorldProgressData
    {
        public float ClearPercentage { get; set; }
        public float GreenPercentage { get; set; }

        public static WorldProgressData LoadForWorld(string worldId)
        {
            string path = $"{Application.persistentDataPath}/world_progress_{worldId}.json";
            
            if (!System.IO.File.Exists(path))
            {
                return new WorldProgressData(); // Return empty data if no save exists
            }

            try
            {
                string json = System.IO.File.ReadAllText(path);
                return JsonConvert.DeserializeObject<WorldProgressData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load progress for world {worldId}: {ex.Message}");
                return new WorldProgressData();
            }
        }

        public static void SaveForWorld(string worldId, float clearPercentage, float greenPercentage)
        {
            var data = new WorldProgressData
            {
                ClearPercentage = clearPercentage,
                GreenPercentage = greenPercentage
            };

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string path = $"{Application.persistentDataPath}/world_progress_{worldId}.json";
                System.IO.File.WriteAllText(path, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save progress for world {worldId}: {ex.Message}");
            }
        }

        public static void DeleteForWorld(string worldId)
        {
            string path = $"{Application.persistentDataPath}/world_progress_{worldId}.json";
            
            if (System.IO.File.Exists(path))
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to delete progress for world {worldId}: {ex.Message}");
                }
            }
        }
    }
}

