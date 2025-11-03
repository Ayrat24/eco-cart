using UnityEngine;

namespace Eco.Scripts.Hats
{
    public class Hat : MonoBehaviour
    {
        public string Id { get; private set; }

        public void Setup(string id)
        {
            Id = id;
        }
    }
}
