using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using HighlightPlus;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Eco.Scripts.Trash
{
    public class TrashPile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ITileItem
    {
        [SerializeField] HighlightEffect highlightEffect;
        [SerializeField] private float heightPerSize = 0.1f;
        [SerializeField] private float digDuration = 0.5f;
        private int _size = 5;
        private bool _isDigging;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isCleared;
        public bool CanBeRecycled => false; // piles are owned by chunks; don't auto-return to PoolManager

        public event System.Action OnPileCleaned;
        
        public void Initialize(int size)
        {
            _size = size;
            _isCleared = false;

            // set visual height
            var pos = transform.position;
            pos.y = size * heightPerSize;
            transform.position = pos;
            
            gameObject.SetActive(true);
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

            // reduce pile size
            var pileHeight = transform.position.y;
            pileHeight -= heightPerSize;
            _size -= 1;
            Tween.PositionY(transform, pileHeight, digDuration);

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
    }
}