using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;

public class FindUIBlockers
{
    [MenuItem("Tools/Find UI Blockers in Scene")]
    private static void FindBlockers()
    {
        var graphics = Object.FindObjectsOfType<Graphic>(true);
        Debug.Log($"Found {graphics.Length} Graphic(s). Listing those with raycastTarget = true:");
        foreach (var g in graphics.OrderByDescending(x => x.transform.GetSiblingIndex()))
        {
            if (g.raycastTarget)
            {
                Debug.Log($"Graphic: {GetFullPath(g.gameObject)} | Type={g.GetType().Name} | RaycastTarget={g.raycastTarget} | ActiveInHierarchy={g.gameObject.activeInHierarchy}");
            }
        }

        var cgs = Object.FindObjectsOfType<CanvasGroup>(true);
        Debug.Log($"Found {cgs.Length} CanvasGroup(s). Listing those with blocksRaycasts = true:");
        foreach (var cg in cgs)
        {
            if (cg.blocksRaycasts)
            {
                Debug.Log($"CanvasGroup: {GetFullPath(cg.gameObject)} | blocksRaycasts={cg.blocksRaycasts} | ActiveInHierarchy={cg.gameObject.activeInHierarchy}");
            }
        }

        // Try to find UI Toolkit UIDocument components by type name (works across Unity versions)
        var allComponents = Object.FindObjectsOfType<Component>(true);
        var uidocs = allComponents.Where(c => c.GetType().Name == "UIDocument").ToArray();
        Debug.Log($"Found {uidocs.Length} UIDocument(s) (UI Toolkit). Listing their GameObjects:");
        foreach (var doc in uidocs)
        {
            Debug.Log($"UIDocument: {GetFullPath(doc.gameObject)} | Type={doc.GetType().Name} | ActiveInHierarchy={doc.gameObject.activeInHierarchy}");
        }

        Debug.Log("Find UI Blockers complete. Use the reported paths to inspect GameObjects in the Hierarchy and check Graphic/CanvasGroup settings.");
    }

    private static string GetFullPath(GameObject go)
    {
        if (go == null) return "null";
        var parts = new System.Collections.Generic.List<string>();
        var t = go.transform;
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}

