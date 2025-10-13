using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Pooling;
using Eco.Scripts.World;
using HighlightPlus;
using PrimeTween;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Eco.Scripts.Trash
{
    public class TrashPile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ITileItem,
        IPooledObject
    {
        [SerializeField] HighlightEffect highlightEffect;
        [SerializeField] private float heightPerSize = 0.1f;
        [SerializeField] private float digDuration = 0.5f;
        private int _size = 5;
        private Tile _tile;
        private bool _isDigging;
        private CancellationTokenSource _cancellationTokenSource;
        public bool CanBeRecycled { get; }

        public static float DigTime = 0.5f;

        public void Initialize(Tile tile, int size)
        {
            _size = size;
            _size = 3;
            _tile = tile;

            var pos = transform.position;
            pos.y = size * heightPerSize;
            transform.position = pos;
        }

        public void Dig()
        {
            if (_isDigging)
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

            //await UniTask.WaitForSeconds(digDuration, cancellationToken: cancellationToken);

            //reduce pile size
            var pileHeight = transform.position.y;
            pileHeight -= heightPerSize;
            _size -= 1;
            Tween.PositionY(transform, pileHeight, digDuration);


            //Spawn random trash
            var trash = PoolManager.Instance.GetRandomTrash();
            var trashPos = transform.position;
            trashPos.y = 0;
            trashPos.z += Random.Range(-1f, 1f);
            trashPos.x += Random.Range(-1f, 1f);

            trash.transform.position = trashPos;
            trash.OnFallenOut();
            trash.AddForce(new Vector3(Random.Range(-3f, 3f), Random.Range(3f, 4f), Random.Range(-3f, 3f)));

            if (_size <= 0)
            {
                _tile.item = null;
                PoolManager.Instance.ReturnItem(this);
                return;
                Debug.LogError("clear");
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
            return 0;
        }

        public void OnSpawn()
        {
        }

        public void OnDespawn()
        {
            Clear();
            _isDigging = false;
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void Clear()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}