using System.Collections.Generic;
using UnityEngine;

public class SaveManager
{
    private readonly Dictionary<Vector2Int, int[]> myDictionary = new();
    public Dictionary<Vector2Int, int[]> FieldTiles => myDictionary;
    
    public void SaveFieldTiles(Vector2Int position, int[] tiles)
    {
        myDictionary[position] = tiles;
    }

    public void SaveFieldTiles()
    {
        DictionaryWrapper wrapper = new DictionaryWrapper();
        foreach (var pair in myDictionary)
        {
            wrapper.items.Add(new Vector2IntArrayPair
            {
                key = new Vector2IntSerializable(pair.Key),
                value = pair.Value
            });
        }

        // Convert to JSON and save
        string json = JsonUtility.ToJson(wrapper);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/field_tiles.json", json);
    }

    public void LoadFieldTiles()
    {
        string path = Application.persistentDataPath + "/field_tiles.json";
        myDictionary.Clear();

        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            DictionaryWrapper wrapper = JsonUtility.FromJson<DictionaryWrapper>(json);

            foreach (var pair in wrapper.items)
            {
                myDictionary[pair.key.ToVector2Int()] = pair.value;
            }
        }
    }

    public void DeleteProgress()
    {
        string path = Application.persistentDataPath + "/field_tiles.json";

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    [System.Serializable]
    public struct Vector2IntArrayPair
    {
        public Vector2IntSerializable key;
        public int[] value;
    }

    [System.Serializable]
    public struct Vector2IntSerializable
    {
        public int x;
        public int y;

        public Vector2IntSerializable(Vector2Int v)
        {
            x = v.x;
            y = v.y;
        }

        public readonly Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }
    }

    [System.Serializable]
    public class DictionaryWrapper
    {
        public List<Vector2IntArrayPair> items = new();
    }
}