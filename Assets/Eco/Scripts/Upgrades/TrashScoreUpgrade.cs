using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/TrashScoreUpgrade")]
    public class TrashScoreUpgrade : Upgrade
    {
        [SerializeField] private Color color;
        public TrashType trashType;
        public int ScoreForCurrentUpgrade { get; private set; }
        public Color Color => color;
        
        protected override void Load(int level)
        {
            base.Load(level);
            ApplyUpgrade(level);
        }

        protected override void ApplyUpgrade(int level)
        {
            ScoreForCurrentUpgrade = 1 + level * 2;
        }
    }
}