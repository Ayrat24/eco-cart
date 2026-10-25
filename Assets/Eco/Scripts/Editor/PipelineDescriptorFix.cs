#if UNITY_6000_5_OR_NEWER
using System.Reflection;
using UnityEditor;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Workaround for com.unity.pipeline 0.9.1-exp.1 on Unity 6.5: PipelineDescriptorPublisher starts from
    /// [OnCodeLoaded] and calls AssetDatabase.IsAssetImportWorkerProcess, which 6.5 forbids during code reload.
    /// The descriptor file (Library/Pipeline/.unity-pipeline-port) is never written, so the `unity` CLI can't
    /// find the editor. Re-run the publisher once the reload has finished. Delete when the package fixes it.
    /// </summary>
    [InitializeOnLoad]
    internal static class PipelineDescriptorFix
    {
        private const string PublisherType = "Unity.Pipeline.PipelineDescriptorPublisher, Unity.Pipeline";

        static PipelineDescriptorFix()
        {
            // Start() itself skips import worker processes, but that check is what can't run yet.
            // afterAssemblyReload fires even when the editor is unfocused and idle; delayCall is the fallback.
            AssemblyReloadEvents.afterAssemblyReload += Publish;
            EditorApplication.delayCall += Publish;
        }

        private static void Publish()
        {
            var start = System.Type.GetType(PublisherType)
                ?.GetMethod("Start", BindingFlags.Static | BindingFlags.NonPublic);

            if (start == null)
            {
                // Package removed or its internals changed: the fix is no longer applicable.
                return;
            }

            start.Invoke(null, null);
        }
    }
}
#endif
