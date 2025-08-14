using UnityEngine;

namespace Eco.Scripts
{
    [CreateAssetMenu(fileName = "Eco", menuName = "Game Settings")]
    public class Settings : ScriptableObject
    {
        [SerializeField] DetailQualities detailQuality;
        public static DetailQualities DetailQuality { get; private set; }

        public void Load()
        {
            DetailQuality = detailQuality;
        }
        
        public enum DetailQualities
        {
            Low,
            Medium,
            High
        }
    }
}
