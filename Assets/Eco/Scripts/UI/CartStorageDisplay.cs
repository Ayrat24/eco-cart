using System;
using System.Collections.Generic;
using Eco.Scripts.ItemCollecting;
using R3;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    public class CartStorageDisplay
    {
        private readonly Player _player;
        private readonly UIDocument _uiDocument;
        private readonly VisualTreeAsset _cartItemTemplate;
        private int _storageSize;

        private VisualElement _container;
        private Label _label;
        private string CartEmptyText => _cartEmptyTextLocalized.GetLocalizedString();
        private string CartFullText => _cartFullTextLocalized.GetLocalizedString();
        private string CartRecyclingText => _cartRecyclingTextLocalized.GetLocalizedString();
        
        private readonly LocalizedString _cartEmptyTextLocalized = new(LocTableName, CartEmptyLocString);
        private readonly LocalizedString _cartFullTextLocalized = new (LocTableName, CartFullLocString);
        private readonly LocalizedString _cartRecyclingTextLocalized  = new (LocTableName, CartRecyclingLocString);

        private IDisposable _playerSubscription;
        private IDisposable _cartSubscription;
        private readonly Dictionary<ICartItem, VisualElement> _items = new();
        private readonly Dictionary<VisualElement, string> _tooltipDescriptions = new();
        private readonly List<VisualElement> _freeVisualElements = new();

        private const string DisappearClassName = "disappear";
        private const string LocTableName = "GameUI";
        private const string CartEmptyLocString = "cart-status-empty";
        private const string CartFullLocString = "cart-status-full";
        private const string CartRecyclingLocString = "cart-status-recycling";
        

        public CartStorageDisplay(UIDocument uiDocument, Player player, VisualTreeAsset cartItemTemplate)
        {
            _player = player;
            _uiDocument = uiDocument;
            _cartItemTemplate = cartItemTemplate;
        }

        public void Init()
        {
            _container = _uiDocument.rootVisualElement.Q<VisualElement>("CartStorage");
            _label = _container.Q<Label>("CartStatus");
            _playerSubscription = _player.OnCartChanged.Subscribe(OnCartChanged);
        }

        private void OnCartChanged(Cart cart)
        {
            _cartSubscription?.Dispose();

            _storageSize = cart.StorageSize;

            var builder = new DisposableBuilder();
            cart.OnItemAdded.Subscribe(OnItemAdded).AddTo(ref builder);
            cart.OnItemRemoved.Subscribe(OnItemRemoved).AddTo(ref builder);
            cart.OnStatusChanged.Subscribe(OnCartStatusChanged).AddTo(ref builder);
            _cartSubscription = builder.Build();
            
            OnCartStatusChanged(Cart.CartState.Empty);
        }

        private void OnItemAdded(ICartItem item)
        {
            var ve = GetVisualElement();
            ve.style.flexGrow = 0;
            ve.style.width = Length.Percent((float)item.GetWeight() / _storageSize * 100);
            ve.style.backgroundColor = item.GetColor();
            _items[item] = ve;
            
            _tooltipDescriptions[ve] = $"{item.GetName()}";

        }

        private void OnItemRemoved(ICartItem item)
        {
            if (item == null)
            {
                foreach (var v in _items.Values)
                {
                    FreeVisualElement(v);
                }

                _items.Clear();
                return;
            }

            if (!_items.TryGetValue(item, out var ve))
            {
                return;
            }

            FreeVisualElement(ve);
            _items.Remove(item);
        }

        private VisualElement GetVisualElement()
        {
            VisualElement visualElement;
            if (_freeVisualElements.Count == 0)
            {
                var t = _cartItemTemplate.Instantiate();
                visualElement = t.Q<VisualElement>("CartItem");
                visualElement.RegisterCallback<PointerEnterEvent>(ShowTooltip);
                visualElement.RegisterCallback<PointerLeaveEvent>(HideTooltip);
                HideTooltip(visualElement);
                
                _container.Add(visualElement);
            }
            else
            {
                visualElement = _freeVisualElements[0];
                _freeVisualElements.RemoveAt(0);
                visualElement.style.display = DisplayStyle.Flex;
            }

            return visualElement;
        }

        private void FreeVisualElement(VisualElement visualElement)
        {
            visualElement.style.display = DisplayStyle.None;
            _freeVisualElements.Add(visualElement);
        }

        private void OnCartStatusChanged(Cart.CartState cartState)
        {
            switch (cartState)
            {
                case Cart.CartState.Full:
                    DisplayLabel(CartFullText);
                    break;
                case Cart.CartState.Recycling:
                    DisplayLabel(CartRecyclingText);
                    break;
                case Cart.CartState.Empty:
                    DisplayLabel(CartEmptyText);
                    break;
                case Cart.CartState.HasItems:
                    _label.AddToClassList(DisappearClassName);
                    _label.style.display = DisplayStyle.None;
                    break;
            }
        }

        private void DisplayLabel(string message)
        {
            _label.RemoveFromClassList(DisappearClassName);
            _label.text = message;
            _label.BringToFront();
            _label.style.display = DisplayStyle.Flex;
        }

        private void ShowTooltip(PointerEnterEvent  enterEvent)
        {
            if (enterEvent.currentTarget is not VisualElement visualElement)
            {
                return;
            }

            ShowTooltip(visualElement);
        }

        private void ShowTooltip(VisualElement visualElement)
        {
            var toolTip = visualElement.Q<Label>("Tooltip");
            toolTip.RemoveFromClassList(DisappearClassName);
            toolTip.style.display = DisplayStyle.Flex;
            toolTip.text = _tooltipDescriptions[visualElement];
        }

        private void HideTooltip(PointerLeaveEvent  exitEvent)
        {
            if (exitEvent.currentTarget is not VisualElement visualElement)
            {
                return;
            }

            HideTooltip(visualElement);
        }

        private static void HideTooltip(VisualElement visualElement)
        {
            var toolTip = visualElement.Q<Label>("Tooltip");
            toolTip.AddToClassList(DisappearClassName);
            toolTip.style.display = DisplayStyle.None;
        }

        public void Clear()
        {
            _cartSubscription?.Dispose();
            _playerSubscription?.Dispose();
        }
    }
}