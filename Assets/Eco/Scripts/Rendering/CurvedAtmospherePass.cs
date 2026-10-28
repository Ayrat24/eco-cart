using Eco.Scripts.Profiling;
using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Eco.Scripts.Rendering
{
    public sealed class CurvedAtmospherePass : ScriptableRenderPass
    {
        private static readonly int BendId = Shader.PropertyToID("_AtmosphereBend");
        private static readonly int ShapeId = Shader.PropertyToID("_AtmosphereShape");
        private static readonly int DistanceId = Shader.PropertyToID("_AtmosphereDistance");
        private static readonly int HorizonId = Shader.PropertyToID("_AtmosphereHorizonColor");
        private static readonly int SpaceId = Shader.PropertyToID("_AtmosphereSpaceColor");
        private static readonly int ScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        private readonly MaterialPropertyBlock _properties = new();
        private Material _material;
        private Vector4 _bend;
        private WorldVisualSettings _settings;

        private sealed class PassData
        {
            public Material Material;
            public MaterialPropertyBlock Properties;
            public Vector4 Bend;
            public Vector4 Shape;
            public Vector4 Distance;
            public Color Horizon;
            public Color Space;
            public TextureHandle Destination;
        }

        public CurvedAtmospherePass()
        {
            profilingSampler = new ProfilingSampler("Eco.CurvedWorld.Atmosphere");
            // The rim is analytic. Requesting depth here can change URP's depth-copy
            // timing and the depth sampled by transparent/refraction shaders.
            ConfigureInput(ScriptableRenderPassInput.None);
        }

        public void Setup(Material material, Vector4 bend, WorldVisualSettings settings)
        {
            _material = material;
            _bend = bend;
            _settings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var marker = EcoProfilerMarkers.Atmosphere.Auto();
            var resources = frameData.Get<UniversalResourceData>();
            using var builder = renderGraph.AddRasterRenderPass<PassData>("Curved World Atmosphere", out var data, profilingSampler);
            data.Material = _material;
            data.Properties = _properties;
            data.Bend = _bend;
            data.Shape = new Vector4(_settings.AtmosphereThickness, _settings.AtmosphereSoftness,
                _settings.AtmosphereStrength, _settings.AtmosphereSurfaceHeight);
            data.Distance = new Vector4(_settings.AtmosphereStartDistance, _settings.AtmosphereFullDistance, 0, 0);
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            data.Horizon = linear ? _settings.AtmosphereHorizonColor.linear : _settings.AtmosphereHorizonColor;
            data.Space = linear ? _settings.AtmosphereSpaceColor.linear : _settings.AtmosphereSpaceColor;
            data.Destination = resources.activeColorTexture;
            // Hardware blending avoids a full-resolution color copy and preserves camera alpha.
            builder.SetRenderAttachment(data.Destination, 0, AccessFlags.ReadWrite);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
            {
                var properties = pass.Properties;
                properties.Clear();
                properties.SetVector(BendId, pass.Bend);
                properties.SetVector(ShapeId, pass.Shape);
                properties.SetVector(DistanceId, pass.Distance);
                // Values are already in the render color space; avoid SetColor's sRGB conversion.
                properties.SetVector(HorizonId, pass.Horizon);
                properties.SetVector(SpaceId, pass.Space);
                properties.SetVector(ScaleBiasId, new Vector4(1, 1, 0, 0));
                context.cmd.DrawProcedural(Matrix4x4.identity, pass.Material, 0, MeshTopology.Triangles, 3, 1, properties);
            });
        }
    }
}
