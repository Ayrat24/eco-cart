using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;

namespace Eco.Scripts.Profiling
{
    /// <summary>
    /// Per-frame stats for one stress test phase: frame times, GC allocations, render counters and the time
    /// spent inside each <see cref="EcoProfilerMarkers"/> marker (plus a few engine markers).
    /// </summary>
    public class StressTestPhase : IDisposable
    {
        private const double NsToMs = 1e-6;

        private static readonly string[] EngineMarkers =
        {
            "GC.Collect",
            "FixedUpdate.PhysicsFixedUpdate",
            "Update.ScriptRunBehaviourUpdate",
            "PostLateUpdate.FinishFrameRendering",
        };

        private readonly List<float> _frameMs = new(4096);
        private readonly List<(string name, ProfilerRecorder recorder, MarkerTotals totals)> _markers = new();
        private ProfilerRecorder _gcAlloc;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _triangles;
        private long _gcAllocTotal;
        private long _gcAllocMax;
        private long _setPassMax;
        private long _trianglesMax;
        private float _startTime;
        private float _duration;

        public string Name { get; }
        public string Notes { get; set; }

        public StressTestPhase(string name, float startTime)
        {
            Name = name;
            _startTime = startTime;

            _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");

            foreach (var marker in EcoProfilerMarkers.Names)
            {
                AddMarker(marker);
            }

            foreach (var marker in EngineMarkers)
            {
                AddMarker(marker);
            }
        }

        private void AddMarker(string name)
        {
            // Look the marker up by name only: engine markers live in different categories.
            var recorder = new ProfilerRecorder(name);
            recorder.Start();
            _markers.Add((name, recorder, new MarkerTotals()));
        }

        public void SampleFrame(float deltaTime)
        {
            _frameMs.Add(deltaTime * 1000f);

            // LastValue is the previous frame's total; a one-frame lag is fine for aggregates.
            long gc = _gcAlloc.Valid ? _gcAlloc.LastValue : 0;
            _gcAllocTotal += gc;
            _gcAllocMax = Math.Max(_gcAllocMax, gc);
            _setPassMax = Math.Max(_setPassMax, _setPass.Valid ? _setPass.LastValue : 0);
            _trianglesMax = Math.Max(_trianglesMax, _triangles.Valid ? _triangles.LastValue : 0);

            foreach (var (_, recorder, totals) in _markers)
            {
                if (!recorder.Valid)
                {
                    continue;
                }

                long ns = recorder.LastValue;
                if (ns <= 0 || recorder.Count == 0)
                {
                    continue;
                }

                totals.TotalNs += ns;
                totals.MaxNs = Math.Max(totals.MaxNs, ns);
                totals.Calls += recorder.GetSample(recorder.Count - 1).Count;
                totals.Frames++;
            }
        }

        public void End(float endTime)
        {
            _duration = endTime - _startTime;
            Dispose();
        }

        public void AppendReport(StringBuilder sb)
        {
            sb.AppendLine($"## {Name} ({_duration:F1}s, {_frameMs.Count} frames)");
            if (!string.IsNullOrEmpty(Notes))
            {
                sb.AppendLine(Notes);
            }

            if (_frameMs.Count == 0)
            {
                sb.AppendLine();
                return;
            }

            var sorted = new List<float>(_frameMs);
            sorted.Sort();
            double sum = 0;
            int over33 = 0, over100 = 0;
            foreach (var ms in _frameMs)
            {
                sum += ms;
                if (ms > 33.3f) over33++;
                if (ms > 100f) over100++;
            }

            sb.AppendLine($"frame ms: avg {sum / _frameMs.Count:F2} | p50 {Percentile(sorted, 0.5f):F2} | " +
                          $"p95 {Percentile(sorted, 0.95f):F2} | p99 {Percentile(sorted, 0.99f):F2} | " +
                          $"max {sorted[^1]:F2} | >33ms {over33} | >100ms {over100}");
            sb.AppendLine($"GC alloc: total {_gcAllocTotal / 1024f / 1024f:F2} MB | " +
                          $"avg {_gcAllocTotal / 1024f / _frameMs.Count:F1} KB/frame | max {_gcAllocMax / 1024f:F1} KB/frame");
            sb.AppendLine($"render max: setpass {_setPassMax} | triangles {_trianglesMax:N0}");
            sb.AppendLine("marker                                     total ms   max ms/frame   frames    calls");

            foreach (var (name, _, totals) in _markers)
            {
                if (totals.Frames == 0)
                {
                    continue;
                }

                sb.AppendLine($"{name,-42} {totals.TotalNs * NsToMs,9:F1} {totals.MaxNs * NsToMs,14:F2} {totals.Frames,8} {totals.Calls,8}");
            }

            sb.AppendLine();
        }

        private static float Percentile(List<float> sorted, float p)
        {
            int index = Math.Clamp((int)Math.Ceiling(p * sorted.Count) - 1, 0, sorted.Count - 1);
            return sorted[index];
        }

        public void Dispose()
        {
            _gcAlloc.Dispose();
            _setPass.Dispose();
            _triangles.Dispose();

            foreach (var (_, recorder, _) in _markers)
            {
                recorder.Dispose();
            }
        }

        private class MarkerTotals
        {
            public long TotalNs;
            public long MaxNs;
            public int Frames;
            public long Calls;
        }
    }
}
