using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using HighlightPlus;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Eco.Scripts.Scrap
{
    public class ScrapPile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ITileItem
    {
        [SerializeField] HighlightEffect highlightEffect;
        [SerializeField] private float digDuration = 0.5f;
        [SerializeField] private ParticleSystem digParticleEffect;
        [SerializeField] private Transform pileParent;

        // Pile size per unit of pile height
        private const int SizePerHeight = 5;

        private float _heightPerSize;
        private int _size;
        private bool _isDigging;
        private bool _isCleared;
        private int _digPower;
        private CancellationTokenSource _cancellationTokenSource;
        public bool CanBeRecycled => false; // piles are owned by chunks; don't auto-return to PoolManager

        public event Action OnPileCleaned;
        
        /// <param name="size">Dig steps left at dig power 1.</param>
        /// <param name="maxSize">Size of the untouched pile; bigger piles are taller.</param>
        public void Initialize(int size, int maxSize, int digPower)
        {
            maxSize = Mathf.Max(1, maxSize);
            _size = size;
            _digPower = digPower;
            _isCleared = false;
            _isDigging = false;
            highlightEffect.highlighted = false;

            _heightPerSize = 1f / maxSize * digPower;

            var pos = Vector3.zero;
            pos.y -= (maxSize - size) * _heightPerSize;
            pos.y -= 0.1f;
            transform.localPosition = pos;
            
            AdjustVisualHeight(maxSize / (float)SizePerHeight);
            
            gameObject.SetActive(true);
        }

        private void AdjustVisualHeight(float height)
        {
            Vector3 s = pileParent.localScale;
            s.y = height;
            pileParent.localScale = s;
            Vector3 p = pileParent.localPosition;
            p.y = height * 0.5f;
            pileParent.localPosition = p;
        }

        public void SetDigPower(int digPower)
        {
            _digPower = digPower;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void Dig()
        {
            if (_isDigging || _isCleared)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
            Dig(_cancellationTokenSource.Token).Forget();
        }


        private async UniTask Dig(CancellationToken cancellationToken)
        {
            _isDigging = true;

            // reduce pile size by dig power
            int removedSize = Mathf.Min(_digPower, _size); // don't remove more than available
            var pileHeight = transform.position.y;
            pileHeight -= _heightPerSize * removedSize;
            _size -= removedSize;
            Tween.PositionY(transform, pileHeight, digDuration);
            digParticleEffect.Play();

            if (_size <= 0)
            {
                OnCleaned();
                Clear();
                return;
            }

            await UniTask.WaitForSeconds(digDuration, cancellationToken: cancellationToken);
            _isDigging = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            highlightEffect.highlighted = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            highlightEffect.highlighted = false;
        }

        public int GetPrefabId()
        {
            // For piles we store their current size in the saved TileData.objectId
            return _size;
        }

        private void OnCleaned()
        {
            if (_isCleared)
            {
                return;
            }

            _isCleared = true;
            OnPileCleaned?.Invoke();
            Hide();
        }

        private void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void OnDisable()
        {
            Clear();
        }
    }
}