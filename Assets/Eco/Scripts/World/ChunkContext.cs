using Eco.Scripts.Upgrades;

namespace Eco.Scripts.World
{
    /// <summary>Game services a chunk needs while it is spawned, handed over by <see cref="WorldController"/>.</summary>
    public class ChunkContext
    {
        public WorldData World { get; }
        public TreePlanter TreePlanter { get; }
        public CurrencyManager Currency { get; }
        public PlayerStats Stats { get; }

        public ChunkContext(WorldData world, TreePlanter treePlanter, CurrencyManager currency, PlayerStats stats)
        {
            World = world;
            TreePlanter = treePlanter;
            Currency = currency;
            Stats = stats;
        }
    }
}
