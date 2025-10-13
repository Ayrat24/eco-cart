using System;
using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class ToolSelector
    {
        private readonly UIDocument _document;
        private List<Button> _buttons;
        private int _currentTool = -1;
        private IDisposable _subscription;

        private const string SelectedClass = "selected";
        public static readonly Subject<int> OnToolSelected = new();

        private readonly List<Action> _eventCallbacks = new();
        private readonly List<UnlockableUpgradeType> _toolTypes = new()
        {
            UnlockableUpgradeType.Cart,
            UnlockableUpgradeType.Spade,
            UnlockableUpgradeType.Smoke
        };

        public ToolSelector(UIDocument uiDocument)
        {
            _document = uiDocument;
        }

        public void Init()
        {
            _buttons = _document.rootVisualElement.Query<Button>("ToolButton").ToList();

            for (int i = 0; i < _buttons.Count; i++)
            {
                var index = i;
                Action callback = () => OnClicked(index);
                _buttons[i].clicked += callback;
                _eventCallbacks.Add(callback);

                if (!Stats.IsUpgradeUnlocked(_toolTypes[index]))
                {
                    _buttons[i].style.display = DisplayStyle.None;
                }
            }

            _subscription = Stats.OnUnlocked.Subscribe(OnToolUnlocked);
            
            OnClicked(0);
        }

        private void OnToolUnlocked(UnlockableUpgradeType type)
        {
            for (int i = 0; i < _toolTypes.Count; i++)
            {
                if (_toolTypes[i] != type)
                {
                    continue;
                }

                _buttons[i].style.display = DisplayStyle.Flex;
                return;
            }
        }

        private void OnClicked(int index)
        {
            if (_currentTool == index)
            {
                return;
            }

            _currentTool = index;
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (i != index)
                {
                    _buttons[i].RemoveFromClassList(SelectedClass);
                    continue;
                }

                _buttons[i].AddToClassList(SelectedClass);
                OnToolSelected.OnNext(index);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].clicked -= _eventCallbacks[i];
            }

            _buttons.Clear();
            _eventCallbacks.Clear();
            _subscription?.Dispose();
        }
    }
}