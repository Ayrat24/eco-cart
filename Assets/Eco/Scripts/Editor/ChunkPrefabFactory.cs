using System.IO;
using Eco.Scripts.World;
using UnityEditor;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Creates and maintains the prefab of each <see cref="ChunkDefinition"/>: a variant of the land or water base
    /// prefab, so objects added to it only appear in that chunk. A definition with a scrap pile gets a nested
    /// ScrapPileSite in the middle.
    /// </summary>
    internal static class ChunkPrefabFactory
    {
        public const string PrefabFolder = "Assets/Eco/Data/Prefabs/Chunks";
        public const string BaseFolder = PrefabFolder + "/Base";
        public const string LandBasePath = BaseFolder + "/LandChunkBase.prefab";
        public const string WaterBasePath = BaseFolder + "/WaterChunkBase.prefab";
        public const string PileSitePath = BaseFolder + "/ScrapPileSite.prefab";

        public static bool KindMatches(ChunkDefinition definition)
        {
            return definition.Prefab == null || definition.IsWater == definition.Prefab is WaterChunk;
        }

        public static bool PileMatches(ChunkDefinition definition)
        {
            return definition.Prefab == null ||
                   definition.Scrap.HasPile == (definition.Prefab.GetComponentInChildren<PileSite>(true) != null);
        }

        /// <summary>Creates the prefab if it's missing and adds or removes the pile site to match the definition.</summary>
        public static void Sync(ChunkDefinition definition)
        {
            if (definition.Prefab == null)
            {
                Create(definition);
                return;
            }

            if (KindMatches(definition) && !PileMatches(definition))
            {
                SyncPileSite(definition);
            }
        }

        /// <summary>
        /// Makes a new prefab variant for the definition. An existing prefab of the wrong kind (land/water) is
        /// replaced, losing whatever was added to it.
        /// </summary>
        public static void Create(ChunkDefinition definition)
        {
            string basePath = definition.IsWater ? WaterBasePath : LandBasePath;
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                Debug.LogError($"Chunk base prefab missing at {basePath}.");
                return;
            }

            string path = definition.Prefab != null
                ? AssetDatabase.GetAssetPath(definition.Prefab)
                : AssetDatabase.GenerateUniqueAssetPath($"{PrefabFolder}/{definition.name}.prefab");
            if (definition.Prefab != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            Directory.CreateDirectory(PrefabFolder);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                instance.name = Path.GetFileNameWithoutExtension(path);
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
                SetPrefab(definition, prefab.GetComponent<Chunk>());
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }

            SyncPileSite(definition);
        }

        /// <summary>Renames the prefab to match a renamed definition.</summary>
        public static void Rename(ChunkDefinition definition)
        {
            if (definition.Prefab == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(definition.Prefab);
            if (Path.GetFileNameWithoutExtension(path) != definition.name)
            {
                AssetDatabase.RenameAsset(path, definition.name);
            }
        }

        public static void Delete(ChunkDefinition definition)
        {
            if (definition.Prefab != null)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(definition.Prefab));
            }
        }

        private static void SyncPileSite(ChunkDefinition definition)
        {
            if (definition.Prefab == null || definition.Prefab is not LandChunk)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(definition.Prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var site = root.GetComponentInChildren<PileSite>(true);
                if (definition.Scrap.HasPile && site == null)
                {
                    var sitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PileSitePath);
                    if (sitePrefab == null)
                    {
                        Debug.LogError($"Scrap pile site prefab missing at {PileSitePath}.");
                        return;
                    }

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(sitePrefab, root.transform);
                    instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                }
                else if (!definition.Scrap.HasPile && site != null)
                {
                    if (!PrefabUtility.IsOutermostPrefabInstanceRoot(site.gameObject))
                    {
                        Debug.LogWarning($"The scrap pile in {path} is part of another object; remove it by hand.",
                            definition.Prefab);
                        return;
                    }

                    Object.DestroyImmediate(site.gameObject);
                }
                else
                {
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetPrefab(ChunkDefinition definition, Chunk prefab)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
