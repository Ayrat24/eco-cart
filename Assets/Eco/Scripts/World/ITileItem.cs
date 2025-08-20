
namespace Eco.Scripts.World
{
    public interface ITileItem
    {
        int GetPrefabId();
        bool CanBeRecycled { get; }
    }
}
