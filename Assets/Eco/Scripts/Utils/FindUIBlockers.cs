using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Eco.Scripts.Utils
{
    // Runtime helper to check whether a screen position is over blocking UI.
    public static class FindUIBlockers
    {
        public static bool IsPointerOverUI(Vector2 pointerPosition, GameObject[] uiRootsToIgnore = null)
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = pointerPosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            var blocking = results.Where(r =>
            {
                var go = r.gameObject;
                if (go == null) return false;

                // If this result is under any ignored root, treat it as non-blocking
                if (uiRootsToIgnore != null)
                {
                    foreach (var root in uiRootsToIgnore)
                    {
                        if (root == null) continue;
                        if (go.transform.IsChildOf(root.transform))
                            return false;
                    }
                }

                var moduleName = r.module != null ? r.module.GetType().Name : "";
                // Ignore results from non-UI raycasters (e.g. PhysicsRaycaster)
                if (!string.IsNullOrEmpty(moduleName) &&
                    !moduleName.Contains("GraphicRaycaster") &&
                    !moduleName.Contains("PanelRaycaster") &&
                    !moduleName.Contains("UIR") &&
                    !moduleName.Contains("UIElements"))
                {
                    return false;
                }

                var graphic = go.GetComponent<Graphic>();
                if (graphic != null)
                {
                    return graphic.raycastTarget;
                }

                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null)
                    return cg.blocksRaycasts;

                // If result came from a panel raycaster (UI Toolkit), treat as blocking
                if (moduleName.Contains("PanelRaycaster") || moduleName.Contains("UIR") || moduleName.Contains("UIElements"))
                    return true;

                return false;
            }).ToList();

            return blocking.Count > 0;
        }
    }
}

