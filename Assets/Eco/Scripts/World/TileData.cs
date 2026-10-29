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
    }
}
