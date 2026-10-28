using Eco.Scripts.Upgrades;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    /// <summary>
    /// One node of the upgrade tree menu: icon, level badge, name and price. The tree view does the picking, so
    /// none of it takes pointer events. Looks come from UpgradeTree.uss, by state class.
    /// </summary>
    public class UpgradeNodeView : VisualElement
    {
        private const string NodeClass = "upgrade-node";
        private const string LockedClass = "upgrade-node--locked";
        private const string AvailableClass = "upgrade-node--available";
        private const string OwnedClass = "upgrade-node--owned";
        private const string MaxedClass = "upgrade-node--maxed";
        private const string AffordableClass = "upgrade-node--affordable";
        private const string SelectedClass = "upgrade-node--selected";
        private const string HoveredClass = "upgrade-node--hovered";
        private const string BoughtClass = "upgrade-node--bought";

        private readonly Label _level;
        private readonly Label _name;
        private readonly Label _cost;

        public UpgradeNode Node { get; }

        public UpgradeNodeView(UpgradeNode node)
        {
            Node = node;
            pickingMode = PickingMode.Ignore;
            AddToClassList(NodeClass);

            float size = UpgradeTree.NodeSize;
            style.position = Position.Absolute;
            style.left = node.Position.x - size / 2f;
            style.top = node.Position.y - size / 2f;
            style.width = size;
            style.height = size;

            var circle = Create<VisualElement>("upgrade-node__circle");
            var icon = Create<VisualElement>("upgrade-node__icon");
            if (node.Icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(node.Icon);
            }

            circle.Add(icon);
            circle.Add(Create<VisualElement>("upgrade-node__lock"));
            Add(circle);

            _level = Create<Label>("upgrade-node__level");
            Add(_level);

            // Name and price hang under the circle
            var caption = Create<VisualElement>("upgrade-node__caption");
            _name = Create<Label>("upgrade-node__name");
            _cost = Create<Label>("upgrade-node__cost");
            caption.Add(_name);
            caption.Add(_cost);
            Add(caption);
        }

        public void SetName(string text)
        {
            _name.text = text;
        }

        public void Refresh(UpgradeNodeState state, int level, bool affordable, string cost)
        {
            EnableInClassList(LockedClass, state == UpgradeNodeState.Locked);
            EnableInClassList(AvailableClass, state == UpgradeNodeState.Available);
            EnableInClassList(OwnedClass, state == UpgradeNodeState.Owned);
            EnableInClassList(MaxedClass, state == UpgradeNodeState.Maxed);
            EnableInClassList(AffordableClass,
                affordable && state is UpgradeNodeState.Available or UpgradeNodeState.Owned);

            _level.text = $"{level}/{Node.MaxLevel}";
            _level.style.display = Node.MaxLevel > 1 || level > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _cost.text = state is UpgradeNodeState.Available or UpgradeNodeState.Owned ? $"{cost}$" : string.Empty;
            _cost.style.display = string.IsNullOrEmpty(_cost.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetSelected(bool selected)
        {
            EnableInClassList(SelectedClass, selected);
        }

        public void SetHovered(bool hovered)
        {
            EnableInClassList(HoveredClass, hovered);
        }

        /// <summary>Plays the purchase pop (a class the stylesheet transitions out of).</summary>
        public void PlayBought()
        {
            AddToClassList(BoughtClass);
            schedule.Execute(() => RemoveFromClassList(BoughtClass)).StartingIn(150);
        }

        private static T Create<T>(string className) where T : VisualElement, new()
        {
            var element = new T { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            return element;
        }
    }
}
