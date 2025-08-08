using System.Collections.Generic;
using Eco.Scripts.Pooling;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

public class WorldController : MonoBehaviour
{
     [Header("Player & Chunk Settings")]
    public Transform player;
    public Field chunkPrefab;
    public int chunkSizeX = 16;
    public int chunkSizeY = 16;
    public int renderRadiusX = 2; // how many chunks in each direction from the player
    public int renderRadiusY = 2; // how many chunks in each direction from the player

    private Vector2Int currentPlayerChunkCoord;
    private readonly Dictionary<Vector2Int, Field> spawnedChunks = new();
    private SaveManager _saveManager;
    
    private ObjectPool<Field> _fieldPool;
    private bool _initialized;
    
    [Inject]
    public void Initialize(SaveManager saveManager)
    {
        _saveManager = saveManager;
    }

    public void SpawnWorld()
    {
        _fieldPool = new ObjectPool<Field>(chunkPrefab, renderRadiusX * renderRadiusY);
        UpdateWorld(true);
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized)
        {
            return;
        }
        
        Vector2Int newChunkCoord = GetPlayerChunkCoord();
        if (newChunkCoord != currentPlayerChunkCoord)
        {
            currentPlayerChunkCoord = newChunkCoord;
            UpdateWorld();
        }
    }

    Vector2Int GetPlayerChunkCoord()
    {
        int x = Mathf.FloorToInt(player.position.x / chunkSizeX);
        int z = Mathf.FloorToInt(player.position.z / chunkSizeY);
        return new Vector2Int(x, z);
    }

    public void UpdateWorld(bool forceUpdate = false)
    {
        Vector2Int center = GetPlayerChunkCoord();
        HashSet<Vector2Int> neededChunks = new();

        // Spawn chunks in render radius
        for (int x = -renderRadiusX; x <= renderRadiusX; x++)
        {
            for (int z = -renderRadiusY; z <= renderRadiusY; z++)
            {
                Vector2Int coord = new Vector2Int(center.x + x, center.y + z);
                neededChunks.Add(coord);

                if (!spawnedChunks.ContainsKey(coord))
                {
                    Vector3 pos = new Vector3(coord.x * chunkSizeX, 0, coord.y * chunkSizeY);
                    Field chunk = _fieldPool.Get();

                    chunk.transform.parent = transform;
                    chunk.transform.position = pos;
                    chunk.transform.rotation = quaternion.identity;

                    chunk.Init(coord, _saveManager);

                    chunk.name = $"Chunk_{coord.x}_{coord.y}";
                    spawnedChunks[coord] = chunk;
                    
                }
            }
        }

        // Optionally remove far-away chunks
        List<Vector2Int> toRemove = new();
        foreach (var chunkCoord in spawnedChunks.Keys)
        {
            if (!neededChunks.Contains(chunkCoord))
                toRemove.Add(chunkCoord);
        }

        foreach (var coord in toRemove)
        {
            _fieldPool.ReturnToPool(spawnedChunks[coord]);
            spawnedChunks.Remove(coord);
        }
    }

    public void SaveWorld()
    {
        foreach (var field in spawnedChunks.Values)
        {
            field.SaveTiles();
        }
    }
}
