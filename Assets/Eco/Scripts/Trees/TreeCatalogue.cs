using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    [CreateAssetMenu(menuName = "Eco/Script/Trees")]
    public class TreeCatalogue : ScriptableObject
    {
        [SerializeField] List<TreeData> treeData;
        
        
        
        [System.Serializable]
        public class TreeData
        {
            public Tree prefab;
            public string name;
            public int price;
        }
    }
}
