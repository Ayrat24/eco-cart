using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Eco.Scripts.Tools
{
    public abstract class Tool : MonoBehaviour
    {
        public bool Active {get; protected set;}
        public abstract UniTask Enable();
        public abstract UniTask Disable();
    }
}
