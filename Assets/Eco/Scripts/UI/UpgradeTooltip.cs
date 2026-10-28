using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    /// <summary>Card next to the hovered (or selected) upgrade node: name, level, effects, price.</summary>
    public class UpgradeTooltip : VisualElement
    {
        private const string EffectClass = "upgrade-tooltip__effect";

        private readonly Label _name;
        private readonly Label _level;
        private readonly Label _description;
        private readonly VisualElement _effects;
        private readonly Label _requirement;
        private readonly Label _price;
        private readonly Label _refund;

        public UpgradeTooltip()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("upgrade-tooltip");
            style.position = Position.Absolute;

            _name = AddLabel("upgrade-tooltip__name");
            _level = AddLabel("upgrade-tooltip__level");
            _description = AddLabel("upgrade-tooltip__description");

            _effects = new VisualElement { pickingMode = PickingMode.Ignore };
            _effects.AddToClassList("upgrade-tooltip__effects");
            Add(_effects);

            _requirement = AddLabel("upgrade-tooltip__requirement");
            _price = AddLabel("upgrade-tooltip__price");
            _refund = AddLabel("upgrade-tooltip__refund");
        }

        /// <summary>Fills the card; empty texts hide their line.</summary>
        public void Set(string name, string level, string description, IReadOnlyList<string> effects,
            string requirement, string price, string refund)
        {
            SetText(_name, name);
            SetText(_level, level);
            SetText(_description, description);
            SetText(_requirement, requirement);
            SetText(_price, price);
            SetText(_refund, refund);

            while (_effects.childCount > effects.Count)
            {
                _effects.RemoveAt(_effects.childCount - 1);
            }

            for (int i = 0; i < effects.Count; i++)
            {
                if (i >= _effects.childCount)
                {
                    var line = new Label { pickingMode = PickingMode.Ignore };
                    line.AddToClassList(EffectClass);
                    _effects.Add(line);
                }

                ((Label)_effects[i]).text = effects[i];
            }
        }

        private Label AddLabel(string className)
        {
            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            Add(label);
            return label;
        }

        private static void SetText(Label label, string text)
        {
            label.text = text;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
