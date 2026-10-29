using Eco.Scripts.Profiling;
using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Eco.Scripts.Rendering
{
    public sealed class CurvedAtmospherePass : ScriptableRenderPass
    {
        private static readonly ShaderTagId SurfaceTag = new("EcoAtmosphereSurface");
        private static readonly int SourceId = Shader.PropertyToID("_AtmosphereSource");
        private static readonly int FieldSizeId = Shader.PropertyToID("_AtmosphereFieldSize");
        private static readonly int StepId = Shader.PropertyToID("_AtmosphereStep");
        private static readonly int ShapeId = Shader.PropertyToID("_AtmosphereShape");
        private static readonly int HorizonId = Shader.PropertyToID("_AtmosphereHorizonColor");
        private static readonly int SpaceId = Shader.PropertyToID("_AtmosphereSpaceColor");
        private static readonly int ScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        private readonly MaterialPropertyBlock _properties = new();
        private Material _material;
        private WorldVisualSettings _settings;

        private sealed class SurfaceData
        {
            public RendererListHandle Renderers;
        }

        private sealed class PassData
        {
            public Material Material;
            public MaterialPropertyBlock Properties;
            public TextureHandle Source;
            public Vector4 FieldSize;
            public Vector4 Shape;
            public Color Horizon;
            public Color Space;
            public int Pass;
            public int Step;
        }

        public CurvedAtmospherePass()
        {
            profilingSampler = new ProfilingSampler("Eco.CurvedWorld.Atmosphere");
            // Do not request/replace _CameraDepthTexture; water keeps its original refraction inputs.
            ConfigureInput(ScriptableRenderPassInput.None);
        }

        public void Setup(Material material, WorldVisualSettings settings)
        {
            _material = material;
            _settings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var marker = EcoProfilerMarkers.Atmosphere.Auto();
            var resources = frameData.Get<UniversalResourceData>();
            var camera = frameData.Get<UniversalCameraData>();
            if (!resources.activeDepthTexture.IsValid()) return;
            var descriptor = camera.cameraTargetDescriptor;
            // Half resolution up to 960 pixels wide bounds propagation cost at 4K.
            float scale = Mathf.Min(0.5f, 960f / descriptor.width);
            descriptor.width = Mathf.Max(1, Mathf.CeilToInt(descriptor.width * scale));
            descriptor.height = Mathf.Max(1, Mathf.CeilToInt(descriptor.height * scale));
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthStencilFormat = GraphicsFormat.D32_SFloat;
            var surface = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor,
                "Atmosphere Surface Depth", false, FilterMode.Point);

            using (var builder = renderGraph.AddRasterRenderPass<SurfaceData>("Atmosphere Terrain and Water", out var data, profilingSampler))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(SurfaceTag, frameData.Get<UniversalRenderingData>(),
                    camera, frameData.Get<UniversalLightData>(), SortingCriteria.CommonOpaque);
                data.Renderers = renderGraph.CreateRendererList(new RendererListParams(frameData.Get<UniversalRenderingData>().cullResults,
                    drawing, new FilteringSettings(RenderQueueRange.all)));
                builder.UseRendererList(data.Renderers);
                builder.SetRenderAttachmentDepth(surface, AccessFlags.Write);
                builder.SetRenderFunc(static (SurfaceData pass, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1, 0);
                    context.cmd.DrawRendererList(pass.Renderers);
                });
            }

            descriptor.depthStencilFormat = GraphicsFormat.None;
            // Preserve subpixel boundary positions through the propagation passes.
            descriptor.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
            var fieldSize = new Vector4(1f / descriptor.width, 1f / descriptor.height, descriptor.width, descriptor.height);
            TextureHandle NewField(string name) => UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, name, false, FilterMode.Point);
            var seeds = NewField("Atmosphere Silhouette Seeds");
            // A stable angular width, independent of the nearest terrain triangle's depth.
            // Orthographic cameras use the same apparent screen width at their stored FOV.
            float pixelRadius = Mathf.Tan(_settings.AtmosphereThickness * Mathf.Deg2Rad) * descriptor.height
                / (2 * Mathf.Tan(camera.camera.fieldOfView * Mathf.Deg2Rad * 0.5f));
            AddPass(renderGraph, surface, seeds, TextureHandle.nullHandle, fieldSize, 0, 0, pixelRadius);
            int radius = Mathf.Clamp(Mathf.CeilToInt(pixelRadius) + 2, 2, Mathf.Max(descriptor.width, descriptor.height));
            var current = seeds;
            for (int step = Mathf.NextPowerOfTwo(radius); step >= 1; step >>= 1)
            {
                var next = NewField("Atmosphere Silhouette " + step);
                AddPass(renderGraph, current, next, TextureHandle.nullHandle, fieldSize, 1, step, pixelRadius);
                current = next;
            }
            AddPass(renderGraph, current, resources.activeColorTexture, resources.activeDepthTexture, fieldSize, 2, 0, pixelRadius);
        }

        private void AddPass(RenderGraph graph, TextureHandle source, TextureHandle destination, TextureHandle depth,
            Vector4 fieldSize, int shaderPass, int step, float pixelRadius)
        {
            using var builder = graph.AddRasterRenderPass<PassData>(shaderPass == 2 ? "Atmosphere Composite" : "Atmosphere Silhouette Filter",
                out var data, profilingSampler);
            data.Material = _material;
            data.Properties = _properties;
            data.Source = source;
            data.FieldSize = fieldSize;
            data.Pass = shaderPass;
            data.Step = step;
            data.Shape = new Vector4(pixelRadius, _settings.AtmosphereSoftness, _settings.AtmosphereStrength, 0);
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            data.Horizon = linear ? _settings.AtmosphereHorizonColor.linear : _settings.AtmosphereHorizonColor;
            data.Space = linear ? _settings.AtmosphereSpaceColor.linear : _settings.AtmosphereSpaceColor;
            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(destination, 0, shaderPass == 2 ? AccessFlags.ReadWrite : AccessFlags.Write);
            if (depth.IsValid()) builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
            {
                var properties = pass.Properties;
                properties.Clear();
                RTHandle source = pass.Source;
                properties.SetTexture(SourceId, source);
                properties.SetVector(FieldSizeId, pass.FieldSize);
                properties.SetFloat(StepId, pass.Step);
                properties.SetVector(ShapeId, pass.Shape);
                properties.SetVector(HorizonId, pass.Horizon);
                properties.SetVector(SpaceId, pass.Space);
                properties.SetVector(ScaleBiasId, new Vector4(1, 1, 0, 0));
                context.cmd.DrawProcedural(Matrix4x4.identity, pass.Material, pass.Pass, MeshTopology.Triangles, 3, 1, properties);
            });
        }
    }
}
