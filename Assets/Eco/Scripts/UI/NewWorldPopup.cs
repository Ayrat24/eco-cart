using System;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class NewWorldPopup
    {
        private readonly UIDocument _uiDocument;
        private VisualElement _popupContainer;
        private Button _yesButton;
        private Button _noButton;
        private Label _messageLabel;

        private bool _isShown;

        public event Action OnAccept;
        public event Action OnDecline;

        private const string FadeClassName = "fade";
        
        public NewWorldPopup(UIDocument uiDocument)
        {
            _uiDocument = uiDocument;
        }

        public void Init()
        {
            var root = _uiDocument.rootVisualElement;

            // Find the popup container (now defined in UXML)
            _popupContainer = root.Q<VisualElement>("NewWorldPopup");
            _messageLabel = _popupContainer.Q<Label>("PopupMessage");
            _yesButton = _popupContainer.Q<Button>("YesButton");
            _noButton = _popupContainer.Q<Button>("NoButton");

            _yesButton.clicked += OnYesClicked;
            _noButton.clicked += OnNoClicked;

            Hide();
        }


        public void Show(float clearPercentage, float greenPercentage)
        {
            if (_isShown) return;
            _popupContainer.style.display = DisplayStyle.Flex;
            _popupContainer.RemoveFromClassList(FadeClassName);
            
            _isShown = true;
        }

        public void Hide()
        {
            _popupContainer.style.display = DisplayStyle.None;
            _popupContainer.AddToClassList(FadeClassName);

            _isShown = false;
        }

        private void OnYesClicked()
        {
            Hide();
            OnAccept?.Invoke();
        }

        private void OnNoClicked()
        {
            Hide();
            OnDecline?.Invoke();
        }

        public void Clear()
        {
            if (_yesButton != null)
                _yesButton.clicked -= OnYesClicked;
            if (_noButton != null)
                _noButton.clicked -= OnNoClicked;
        }
    }
}

