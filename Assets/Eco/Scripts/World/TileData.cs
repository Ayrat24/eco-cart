using Newtonsoft.Json;

namespace Eco.Scripts.World
{
    [System.Serializable]
    public struct TileData
    {
        public int objectType;
        public int objectId;
        public int ground;

        // Keeps the pre-rename JSON key so existing saves still load
        [JsonProperty("containedTrash")]
        public bool containedScrap;

        // A fruit tree's fruit and where it stands on its type's clock. Left out of the JSON on every other tile.
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int fruits;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double fruitCycle;
    }
}
