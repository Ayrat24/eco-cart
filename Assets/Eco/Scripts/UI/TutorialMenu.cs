using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class TutorialMenu
    {
        private Button _openButton;
        private Button _closeButton;
        private VisualElement _content;

        private bool _isOpen = true;

        public void Init(UIDocument uiDoc)
        {
            var root = uiDoc.rootVisualElement;
            _content = root.Q<VisualElement>("HelpMenu");
            _closeButton = _content.Q<Button>("CloseButton");
            _openButton = root.Q<Button>("TutorialButton");

            _closeButton.RegisterCallback<ClickEvent>(OnCloseClicked);
            _openButton.RegisterCallback<ClickEvent>(OnOpenClicked);

            OnCloseClicked(null);
        }

        private void OnOpenClicked(ClickEvent evt)
        {
            if (_isOpen)
            {
                OnCloseClicked(null);
                return;
            }

            _content.style.display = DisplayStyle.Flex;
            _isOpen = true;
        }

        private void OnCloseClicked(ClickEvent evt)
        {
            _content.style.display = DisplayStyle.None;
            _isOpen = false;
        }

        public void Clear()
        {
            _closeButton.UnregisterCallback<ClickEvent>(OnCloseClicked);
            _openButton.UnregisterCallback<ClickEvent>(OnOpenClicked);
        }
    }
}