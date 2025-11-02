using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.World;
using PrimeTween;
using UnityEngine;

namespace Eco.Scripts.Trees
{
    public class Tree : MonoBehaviour, ITileItem
    {
        [SerializeField] private int prefabId;
        [SerializeField] private GameObject particleHolder;
        [SerializeField] ParticleSystem particle;
        [SerializeField] private Transform model;

        private const float ParticleAnimationDuration = 0.3f;
        private const float ModelAnimationDuration = 0.4f;

        private CancellationTokenSource _cancellationTokenSource;
        
        public int GetPrefabId()
        {
            return prefabId;
        }

        public void ShowPlantAnimation()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            PlayPlantAnimationAsync(_cancellationTokenSource.Token).Forget();
        }

        private async UniTask PlayPlantAnimationAsync(CancellationToken token)
        {
            model.transform.localScale = Vector3.zero;
            particleHolder.transform.localScale = Vector3.zero;
            particleHolder.SetActive(true);
            
            await UniTask.NextFrame(token);
            
            particle.Play();
            
            Tween.Scale(particleHolder.transform, Vector3.one, ParticleAnimationDuration);
            
            await UniTask.WaitForSeconds(ParticleAnimationDuration / 2, cancellationToken: token);

            Tween.Scale(model.transform, Vector3.one, ModelAnimationDuration, Ease.OutBack);

            await UniTask.WaitForSeconds(ParticleAnimationDuration / 2, cancellationToken: token);
            Tween.Scale(particleHolder.transform, Vector3.zero, ParticleAnimationDuration);
            await UniTask.WaitForSeconds(ParticleAnimationDuration, cancellationToken: token);
            
            particleHolder.SetActive(false);            
        }
        
        public bool CanBeRecycled => true;
    }
}
