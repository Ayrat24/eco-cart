using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Text summary of a Profiler capture (.raw): main-thread self time and GC allocations per sample name, plus
    /// the worst frames broken down. Lets you (or an agent over the `unity` CLI) read a stress test capture without
    /// opening the Profiler window:
    /// unity command script eval --code "return Eco.Scripts.EditorTools.ProfilerCaptureSummary.Summarize();"
    /// </summary>
    public static class ProfilerCaptureSummary
    {
        private const string SummarizeMenu = "Eco/Profiling/Summarize Latest Capture";
        private const int MainThread = 0;

        [MenuItem(SummarizeMenu)]
        private static void SummarizeLatest()
        {
            Debug.Log(Summarize());
        }

        /// <param name="path">A .raw capture; defaults to the newest one in Logs/StressTest.</param>
        public static string Summarize(string path = null, int topSamples = 30, int worstFrames = 5)
        {
            path ??= FindLatestCapture();
            if (path == null || !ProfilerDriver.LoadProfile(path, false))
            {
                return $"No capture loaded ({path ?? "none found in Logs/StressTest"}).";
            }

            var selfMs = new Dictionary<string, double>();
            var gcBytes = new Dictionary<string, double>();
            var frames = new List<(int index, float ms)>();

            for (int f = ProfilerDriver.firstFrameIndex; f <= ProfilerDriver.lastFrameIndex; f++)
            {
                using var view = OpenFrame(f);
                if (!view.valid)
                {
                    continue;
                }

                frames.Add((f, view.frameTimeMs));
                Accumulate(view, selfMs, gcBytes);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{path}: {frames.Count} frames, main thread");
            sb.AppendLine("-- self time, ms total");
            foreach (var (name, ms) in Top(selfMs, topSamples))
            {
                sb.AppendLine($"{ms,10:F1}  {name}");
            }

            sb.AppendLine("-- GC alloc, KB total");
            foreach (var (name, bytes) in Top(gcBytes, topSamples))
            {
                sb.AppendLine($"{bytes / 1024,10:F1}  {name}");
            }

            sb.AppendLine("-- worst frames, top self time");
            foreach (var (index, ms) in frames.OrderByDescending(x => x.ms).Take(worstFrames))
            {
                using var view = OpenFrame(index);
                var frameSelf = new Dictionary<string, double>();
                Accumulate(view, frameSelf, null);
                var top = string.Join(" | ", Top(frameSelf, 8).Select(x => $"{x.name} {x.value:F1}"));
                sb.AppendLine($"frame {index} {ms:F1} ms: {top}");
            }

            return sb.ToString();
        }

        private static string FindLatestCapture()
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "StressTest"));
            return Directory.Exists(dir)
                ? new DirectoryInfo(dir).GetFiles("*.raw").OrderByDescending(f => f.LastWriteTime).FirstOrDefault()?.FullName
                : null;
        }

        private static HierarchyFrameDataView OpenFrame(int frame)
        {
            return ProfilerDriver.GetHierarchyFrameDataView(frame, MainThread,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnDontSort, false);
        }

        private static void Accumulate(HierarchyFrameDataView view, Dictionary<string, double> selfMs,
            Dictionary<string, double> gcBytes)
        {
            var children = new List<int>();
            var stack = new Stack<int>();
            int root = view.GetRootItemID();
            stack.Push(root);

            while (stack.Count > 0)
            {
                int id = stack.Pop();
                view.GetItemChildren(id, children);
                foreach (var child in children)
                {
                    stack.Push(child);
                }

                if (id == root)
                {
                    continue;
                }

                var name = view.GetItemName(id);
                Add(selfMs, name, view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime));

                // columnGcMemory is inclusive of children, so parents repeat their children's allocations.
                if (gcBytes != null)
                {
                    float gc = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory);
                    if (gc > 0)
                    {
                        Add(gcBytes, name, gc);
                    }
                }
            }
        }

        private static void Add(Dictionary<string, double> totals, string name, double value)
        {
            totals.TryGetValue(name, out var current);
            totals[name] = current + value;
        }

        private static IEnumerable<(string name, double value)> Top(Dictionary<string, double> totals, int count)
        {
            return totals.OrderByDescending(x => x.Value).Take(count).Select(x => (x.Key, x.Value));
        }
    }
}
