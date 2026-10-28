using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eco.Scripts.Hats
{
    /// <summary>Puts the chosen hat on the jelly and remembers it in the save.</summary>
    public class HatController
    {
        private readonly Transform _hatParent;
        private readonly IReadOnlyList<HatDefinition> _hats;

        private Hat _currentHat;

        public HatController(IReadOnlyList<HatDefinition> hats, Transform hatParent)
        {
            _hats = hats;
            _hatParent = hatParent;
        }

        public void Initialize(SaveManager saveManager)
        {
            string savedId = saveManager.Progress.selectedHat;
            if (string.IsNullOrEmpty(savedId) || _hats == null)
            {
                return;
            }

            foreach (var hat in _hats)
            {
                if (hat != null && hat.Id == savedId)
                {
                    Equip(hat);
                    return;
                }
            }
        }

        public void Equip(HatDefinition hat)
        {
            if (_currentHat != null)
            {
                Object.Destroy(_currentHat.gameObject);
                _currentHat = null;
            }

            if (hat == null || hat.Data.prefab == null)
            {
                return;
            }

            _currentHat = Object.Instantiate(hat.Data.prefab, _hatParent);
            _currentHat.transform.localPosition = hat.Data.positionOffset;
            _currentHat.transform.localRotation = Quaternion.identity;
            _currentHat.Setup(hat.Id);
        }

        public void SaveHat(SaveManager saveManager)
        {
            saveManager.Progress.selectedHat = _currentHat != null ? _currentHat.Id : string.Empty;
        }
    }
}
