using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.ItemCollecting;
using Eco.Scripts.Scrap;
using Eco.Scripts.Trees;
using Eco.Scripts.World;
using R3;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using VContainer;
using VContainer.Unity;
using Tree = Eco.Scripts.Trees.Tree;

namespace Eco.Scripts.Profiling
{
    /// <summary>
    /// Runs the stress test phases once the game has started: Load (boot until GameController is initialized),
    /// Idle (stand still in the full world), Plant (buy trees one by one), Collect (pick up scrap and recycle it),
    /// Traverse (drive a loop across the world, streaming chunks in and out), Churn (teleport so every loaded chunk
    /// despawns and a new set spawns). Writes a text report and optionally a Profiler capture, then exits play mode.
    /// Created by <see cref="StressTest"/>; not meant to be placed in a scene.
    /// </summary>
    public class StressTestDriver : MonoBehaviour
    {
        private const float ArriveDistance = 3f;
        private const int ProfilerMaxUsedMemory = 512 * 1024 * 1024;

        private readonly List<StressTestPhase> _phases = new();
        private StressTestPhase _current;
        private StressTestConfig _config;
        private string _seedStats;
        private string _runId;
        private int _firstFrame;

        public void SetSeedStats(string seedStats)
        {
            _seedStats = seedStats;
        }

        private void Awake()
        {
            _config = StressTest.Config;
            _runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _firstFrame = Time.frameCount + 1;
            BeginPhase("Load");
            RunAsync(destroyCancellationToken).Forget();
        }

        private void Update()
        {
            // The first frame's delta also covers entering play mode / starting the player, not game work.
            if (Time.frameCount <= _firstFrame)
            {
                return;
            }

            _current?.SampleFrame(Time.unscaledDeltaTime);
        }

        private async UniTask RunAsync(CancellationToken token)
        {
            var resolver = await WaitForGameAsync(token);
            var player = resolver.Resolve<Player>();
            var world = resolver.Resolve<WorldController>();
EndPhase(world);

            BeginPhase("Idle");
            await UniTask.WaitForSeconds(_config.IdleSeconds, cancellationToken: token);
            EndPhase(world);

            BeginPhase("Plant");
            int planted = await PlantTreesAsync(world, token);
            _current.Notes = $"trees planted: {planted}";
            EndPhase(world);

            BeginPhase("Collect");
            _current.Notes = await CollectAsync(player, token);
            EndPhase(world);

            BeginPhase("Traverse");
            string capturePath = StartCapture(token);
            var traverseNotes = await TraverseAsync(player, world, token);
            StopCapture();
            _current.Notes = traverseNotes + (capturePath != null ? $"\nprofiler capture: {capturePath}" : "");
            EndPhase(world);

            BeginPhase("Churn");
            _current.Notes = await ChurnAsync(player, world, token);
            EndPhase(world);

            WriteReport();

            if (_config.QuitWhenDone)
            {
                Quit();
            }
        }

        private static async UniTask<IObjectResolver> WaitForGameAsync(CancellationToken token)
        {
            while (true)
            {
                // WorldSelector reloads the scene before starting, so the first scope found may be replaced.
                var scope = LifetimeScope.Find<GameScope>();
                if (scope != null && scope.Container != null && scope.Container.Resolve<GameController>().Initialized)
                {
                    return scope.Container;
                }

                await UniTask.NextFrame(token);
            }
        }

        private async UniTask<int> PlantTreesAsync(WorldController world, CancellationToken token)
        {
            if (_config.TreePrefabs.Count == 0)
            {
                return 0;
            }

            int planted = 0;
            for (int i = 0; i < _config.TreesToPlant; i++)
            {
                // Same path as planting from the game, minus the cost.
                var prefab = _config.TreePrefabs[i % _config.TreePrefabs.Count];
                if (world.TreePlanter.PlantTree(prefab.GetPrefabId()))
                {
                    planted++;
                }

                await UniTask.WaitForSeconds(_config.PlantInterval, cancellationToken: token);
            }

            return planted;
        }

        private async UniTask<string> CollectAsync(Player player, CancellationToken token)
        {
            var agent = player.GetComponent<NavMeshAgent>();
            var stomach = player.Stomach;
            if (stomach == null || _config.CollectSeconds <= 0f)
            {
                return "skipped (no stomach or collectSeconds = 0)";
            }

            // Scrap that rolled into reach during Idle can already be swallowed. inStomach below doesn't count it,
            // so it would never be recycled and could leave too little room for any item.
            PlayerClickMovement.OnLeftClicked?.Invoke();
            await UniTask.WaitWhile(() => stomach.IsEmptying, cancellationToken: token);

            int picked = 0, inStomach = 0, freedTiles = 0, empties = 0;
            float lastPickup = Time.realtimeSinceStartup;
            using var added = stomach.OnItemAdded.Subscribe(_ =>
            {
                picked++;
                inStomach++;
                lastPickup = Time.realtimeSinceStartup;
            });
            using var recycled = ScrapItem.OnItemRecycled.Subscribe(_ => freedTiles++);

            // Drive a small loop at the player's own speed so the tentacles keep finding scrap.
            var origin = player.transform.position;
            float s = _config.CollectLoopSize;
            var corners = new[]
            {
                origin + new Vector3(s, 0, s), origin + new Vector3(s, 0, -s),
                origin + new Vector3(-s, 0, -s), origin + new Vector3(-s, 0, s)
            };
            int corner = 0;
            agent.SetDestination(corners[corner]);

            float end = Time.realtimeSinceStartup + _config.CollectSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                await UniTask.NextFrame(token);

                // Item weights rarely add up to exactly full, so also empty once pickups stall.
                bool stalled = Time.realtimeSinceStartup - lastPickup > 2f;
                if (inStomach > 0 && !stomach.IsEmptying && (stomach.IsFull || stalled))
                {
                    // Same path as the player's left click.
                    PlayerClickMovement.OnLeftClicked?.Invoke();
                    inStomach = 0;
                    empties++;
                }

                if (!agent.pathPending && agent.remainingDistance <= ArriveDistance)
                {
                    corner = (corner + 1) % corners.Length;
                    agent.SetDestination(corners[corner]);
                }
            }

            agent.ResetPath();
            if (inStomach > 0)
            {
                PlayerClickMovement.OnLeftClicked?.Invoke();
                empties++;
            }

            // Let the last recycle animations finish inside this phase.
            await UniTask.WaitWhile(() => stomach.IsEmptying, cancellationToken: token);
            await UniTask.WaitForSeconds(1.5f, cancellationToken: token);

            return $"picked up {picked} scrap, {freedTiles} tiles freed by recycling, stomach emptied {empties}x";
        }

        private async UniTask<string> ChurnAsync(Player player, WorldController world, CancellationToken token)
        {
            // Two spots 2 * renderRadius + 1 chunks apart: each jump replaces every loaded chunk.
            var agent = player.GetComponent<NavMeshAgent>();
            const int size = WorldController.ChunkSize;
            float y = player.transform.position.y;
            var a = new Vector3(-(world.RenderRadius + 1) * size + size / 2f, y, size / 2f);
            var b = new Vector3(world.RenderRadius * size + size / 2f, y, size / 2f);

            for (int i = 0; i < _config.ChurnJumps; i++)
            {
                agent.Warp(i % 2 == 0 ? a : b);
                await UniTask.WaitForSeconds(_config.ChurnInterval, cancellationToken: token);
            }

            return $"{_config.ChurnJumps} jumps between x={a.x} and x={b.x}, each replacing all " +
                   $"{(world.RenderRadius * 2 + 1) * (world.RenderRadius * 2 + 1)} loaded chunks";
        }

        private async UniTask<string> TraverseAsync(Player player, WorldController world, CancellationToken token)
        {
            var agent = player.GetComponent<NavMeshAgent>();
            float speed = agent.speed, acceleration = agent.acceleration;
            bool autoBraking = agent.autoBraking;
            agent.speed = _config.MoveSpeed;
            agent.acceleration = _config.MoveSpeed * 4f;
            agent.autoBraking = false;

            int radiusChunks = Mathf.Min(_config.RouteRadiusChunks, world.WorldSideSize - 2);
            float r = radiusChunks * WorldController.ChunkSize;
            float y = player.transform.position.y;
            var corners = new[] { new Vector3(r, y, r), new Vector3(r, y, -r), new Vector3(-r, y, -r), new Vector3(-r, y, r) };

            float deadline = Time.realtimeSinceStartup + _config.MaxTraverseSeconds;
            int legs = 0, warps = 0;
            int chunkChanges = 0;
            var lastChunk = world.GetPlayerChunkCoord();

            for (int lap = 0; lap < _config.Laps; lap++)
            {
                foreach (var corner in corners)
                {
                    float legTimeout = Time.realtimeSinceStartup +
                                       Vector3.Distance(player.transform.position, corner) / _config.MoveSpeed * 3f + 5f;
                    agent.SetDestination(corner);

                    while (Time.realtimeSinceStartup < legTimeout && Time.realtimeSinceStartup < deadline)
                    {
                        await UniTask.NextFrame(token);

                        var chunk = world.GetPlayerChunkCoord();
                        if (chunk != lastChunk)
                        {
                            chunkChanges++;
                            lastChunk = chunk;
                        }

                        if (!agent.pathPending && agent.remainingDistance <= ArriveDistance)
                        {
                            break;
                        }
                    }

                    if (Vector3.Distance(player.transform.position, corner) > ArriveDistance * 2f)
                    {
                        // Path blocked or too slow: jump so the run still covers the whole loop.
                        agent.Warp(corner);
                        warps++;
                    }

                    legs++;
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        break;
                    }
                }
            }

            agent.ResetPath();
            agent.speed = speed;
            agent.acceleration = acceleration;
            agent.autoBraking = autoBraking;

            return $"route: {_config.Laps} lap(s), radius {radiusChunks} chunks, speed {_config.MoveSpeed} m/s, " +
                   $"{legs} legs, {chunkChanges} chunk crossings, {warps} warps";
        }

        private string StartCapture(CancellationToken token)
        {
            if (_config.ProfilerCaptureSeconds <= 0f)
            {
                return null;
            }

            string path = Path.Combine(StressTest.ReportDirectory, $"stress_{_runId}.raw");
            Profiler.maxUsedMemory = ProfilerMaxUsedMemory;
            Profiler.logFile = path;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            StopCaptureAfterAsync(_config.ProfilerCaptureSeconds, token).Forget();
            return path;
        }

        private async UniTaskVoid StopCaptureAfterAsync(float seconds, CancellationToken token)
        {
            await UniTask.WaitForSeconds(seconds, ignoreTimeScale: true, cancellationToken: token);
            StopCapture();
        }

        private static void StopCapture()
        {
            if (!Profiler.enableBinaryLog)
            {
                return;
            }

            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = string.Empty;
        }

        private void BeginPhase(string phaseName)
        {
            _current = new StressTestPhase(phaseName, Time.realtimeSinceStartup);
        }

        private void EndPhase(WorldController world)
        {
            _current.End(Time.realtimeSinceStartup);
            _current.Notes = AppendLine(_current.Notes, DescribeScene(world));
            _phases.Add(_current);
            _current = null;
        }

        private static string DescribeScene(WorldController world)
        {
            int scrap = FindObjectsByType<ScrapItem>(FindObjectsSortMode.None).Length;
            int trees = FindObjectsByType<Tree>(FindObjectsSortMode.None).Length;
            int awakeBodies = 0;
            var bodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            foreach (var body in bodies)
            {
                if (!body.isKinematic && !body.IsSleeping())
                {
                    awakeBodies++;
                }
            }

            return $"scene at end: {world.ActiveChunks.Count} chunks, {scrap} scrap, {trees} trees, " +
                   $"{bodies.Length} rigidbodies ({awakeBodies} awake)";
        }

        private void WriteReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Eco Cart stress test {_runId}");
            sb.AppendLine($"unity {Application.unityVersion} | {(Application.isEditor ? "editor" : "player")} | " +
                          $"{SystemInfo.processorType} | {SystemInfo.graphicsDeviceName} | " +
                          $"{Screen.width}x{Screen.height} | detail {Settings.DetailQuality}");
            sb.AppendLine($"seed: {_seedStats}");
            sb.AppendLine();

            foreach (var phase in _phases)
            {
                phase.AppendReport(sb);
            }

            string report = sb.ToString();
            string path = Path.Combine(StressTest.ReportDirectory, $"stress_{_runId}.txt");
            File.WriteAllText(path, report);
            Debug.Log($"[StressTest] Report written to {path}\n{report}");
        }

        private static string AppendLine(string text, string line)
        {
            return string.IsNullOrEmpty(text) ? line : text + "\n" + line;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            _current?.Dispose();
            StopCapture();
        }
    }
}
