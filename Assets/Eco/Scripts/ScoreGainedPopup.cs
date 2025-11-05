using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LargeNumbers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts
{
    public class ScoreGainedPopup : MonoBehaviour
    {
        [SerializeField] private GameObject popupPrefab; // UIDocument prefab to spawn
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float popupDuration = 2f;
        [SerializeField] private float floatUpSpeed = 1f;
        
        private Camera _camera;
        private Queue<PopupInstance> _pool = new();
        private List<PopupInstance> _activePopups = new();
        private CancellationTokenSource _cancellationTokenSource;

        private const string AnimateClass = "hide";
        
        private static ScoreGainedPopup _instance;
        public static ScoreGainedPopup Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void Init()
        {
            _camera = Camera.main;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Create pool
            for (int i = 0; i < poolSize; i++)
            {
                var instance = CreatePopupInstance();
                instance.GameObject.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        private PopupInstance CreatePopupInstance()
        {
            var obj = Instantiate(popupPrefab, transform);
            var uiDoc = obj.GetComponent<UIDocument>();
            
            return new PopupInstance
            {
                GameObject = obj,
                Transform = obj.transform,
                UidDocument = uiDoc
            };
        }

        public void ShowPopup(Vector3 worldPosition, AlphabeticNotation amount)
        {
            if (_pool.Count == 0)
            {
                // Pool exhausted, expand it
                var instance = CreatePopupInstance();
                _pool.Enqueue(instance);
            }

            var popup = _pool.Dequeue();

            worldPosition.y = 3;
            worldPosition.z -= 1;
            
            popup.StartPosition = worldPosition;
            popup.Transform.position = worldPosition;
            popup.Text = $"+{amount}";
            popup.ElapsedTime = 0f;
            
            popup.GameObject.SetActive(true);
            popup.Label.RemoveFromClassList(AnimateClass);
            
            // Face camera
            FaceCamera(popup.Transform);
            
            _activePopups.Add(popup);
            
            // Start animation and lifecycle
            AnimatePopup(popup, _cancellationTokenSource.Token).Forget();
        }

        private void Update()
        {
            // Update all active popup positions to face camera and float upward
            foreach (var popup in _activePopups)
            {
                FaceCamera(popup.Transform);
                
                // Float upward
                popup.ElapsedTime += Time.deltaTime;
                float upwardOffset = popup.ElapsedTime * floatUpSpeed;
                popup.Transform.position = popup.StartPosition + Vector3.up * upwardOffset;
            }
        }

        private void FaceCamera(Transform target)
        {
            if (_camera == null) return;
            
            Vector3 targetPosition = target.position + _camera.transform.rotation * Vector3.forward;
            Vector3 targetUp = _camera.transform.rotation * Vector3.up;
            target.LookAt(targetPosition, targetUp);
        }

        private async UniTask AnimatePopup(PopupInstance popup, CancellationToken cancellationToken)
        {
            // Wait one frame to ensure the element is ready
            await UniTask.NextFrame(cancellationToken);
            
            popup.Label.text = popup.Text;

            // Wait for animation duration
            await UniTask.WaitForSeconds(popupDuration, cancellationToken: cancellationToken);
            popup.Label.AddToClassList(AnimateClass);
            await UniTask.WaitForSeconds(0.5f, cancellationToken: cancellationToken);

            // Return to pool
            if (popup.GameObject != null)
            {
                popup.Label.RemoveFromClassList(AnimateClass);
                popup.GameObject.SetActive(false);
                _activePopups.Remove(popup);
                _pool.Enqueue(popup);
            }
        }

        public static void Show(Vector3 worldPosition, AlphabeticNotation amount)
        {
            if (Instance != null)
            {
                Instance.ShowPopup(worldPosition, amount);
            }
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private class PopupInstance
        {
            public GameObject GameObject;
            public Transform Transform;
            public Vector3 StartPosition;
            public float ElapsedTime;
            public UIDocument UidDocument;
            public Label Label => GetLabel();
            public string Text { get; set; }

            private Label _label;
            
            public Label GetLabel()
            {
                return _label ??= UidDocument.rootVisualElement.Q<Label>("Money");
            }
        }
    }
}
