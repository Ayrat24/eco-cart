using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts
{
    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private UIDocument transitionDocument;

        public static SceneTransition Instance { get; private set; }

        private VisualElement _overlay;
        private VisualElement _loadingSpinner;
        private bool _isTransitioning;
        private Tween _spinnerTween;

        public const float FadeDuration = 0.5f;
        private const string HideClassName = "hide";
        private const string SmallClassName = "small";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
                var root = transitionDocument.rootVisualElement;
                _overlay = root.Q<VisualElement>("TransitionOverlay");
                _loadingSpinner = root.Q<VisualElement>("LoadingContainer");

                // Start fully visible on first load
                _overlay.style.opacity = 1;
                _overlay.style.display = DisplayStyle.Flex;
                _isTransitioning = true;
            }

            public async UniTask FadeOut()
            {
                if (_isTransitioning)
                {
                    return;
                }

                _isTransitioning = true;
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.RemoveFromClassList(HideClassName);
                await UniTask.WaitForSeconds(FadeDuration);
                ShowLoading(true);

            }

            public async UniTask FadeIn()
            {
                _overlay.AddToClassList(HideClassName);
                ShowLoading(false);
                await UniTask.WaitForSeconds(FadeDuration);
                _overlay.style.display = DisplayStyle.None;

                _isTransitioning = false;
            }

            private void ShowLoading(bool show)
            {
                if (show)
                {
                    _loadingSpinner.RemoveFromClassList(SmallClassName);
                    
                    // Start spinning the loading spinner with PrimeTween
                    if (_spinnerTween.isAlive)
                    {
                        _spinnerTween.Stop();
                    }
                    
                    // Use Tween.Custom to rotate the UI element continuously
                    _spinnerTween = Tween.Custom(0f, 360f, 
                        duration: 1f, 
                        onValueChange: angle => _loadingSpinner.style.rotate = new Rotate(angle),
                        cycles: -1, // -1 for infinite loops
                        ease: Ease.Linear);
                }
                else
                {
                    _loadingSpinner.AddToClassList(SmallClassName);
                    
                    // Stop the spinner tween
                    if (_spinnerTween.isAlive)
                    {
                        _spinnerTween.Stop();
                    }
                }
            }
        }
    }
