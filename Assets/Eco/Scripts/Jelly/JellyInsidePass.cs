using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// Renders the swallowed-item layer, opaque then transparent, into _JellyInsideTexture (premultiplied colour,
    /// alpha = coverage) and _JellyInsideDepth for the jelly body to composite. See <see cref="JellyInsideFeature"/>.
    /// </summary>
    public class JellyInsidePass : ScriptableRenderPass
    {
        private static readonly int InsideTextureId = Shader.PropertyToID("_JellyInsideTexture");
        private static readonly int InsideDepthId = Shader.PropertyToID("_JellyInsideDepth");

        private static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly")
        };

        private LayerMask _layerMask;
        private float _resolutionScale = 1f;

        private class PassData
        {
            public RendererListHandle Opaque;
            public RendererListHandle Transparent;
        }

        public JellyInsidePass()
        {
            profilingSampler = new ProfilingSampler("Jelly Inside");
        }

        public void Setup(LayerMask layerMask, float resolutionScale)
        {
            _layerMask = layerMask;
            _resolutionScale = resolutionScale;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();

            var colorDescriptor = cameraData.cameraTargetDescriptor;
            colorDescriptor.width = Mathf.Max(1, Mathf.RoundToInt(colorDescriptor.width * _resolutionScale));
            colorDescriptor.height = Mathf.Max(1, Mathf.RoundToInt(colorDescriptor.height * _resolutionScale));
            colorDescriptor.msaaSamples = 1;
            colorDescriptor.depthStencilFormat = GraphicsFormat.None;
            colorDescriptor.graphicsFormat = cameraData.isHdrEnabled
                ? GraphicsFormat.R16G16B16A16_SFloat
                : QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;

            var depthDescriptor = colorDescriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.None;
            depthDescriptor.depthStencilFormat = GraphicsFormat.D32_SFloat;

            var color = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorDescriptor, "_JellyInsideTexture",
                false, FilterMode.Bilinear);
            var depth = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDescriptor, "_JellyInsideDepth",
                false);

            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler);

            var opaqueDrawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData,
                cameraData.defaultOpaqueSortFlags);
            passData.Opaque = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults,
                opaqueDrawing, new FilteringSettings(RenderQueueRange.opaque, _layerMask)));

            var transparentDrawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData,
                lightData, SortingCriteria.CommonTransparent);
            passData.Transparent = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults,
                transparentDrawing, new FilteringSettings(RenderQueueRange.transparent, _layerMask)));

            builder.UseRendererList(passData.Opaque);
            builder.UseRendererList(passData.Transparent);
            if (resourceData.mainShadowsTexture.IsValid())
            {
                builder.UseTexture(resourceData.mainShadowsTexture);
            }

            if (resourceData.additionalShadowsTexture.IsValid())
            {
                builder.UseTexture(resourceData.additionalShadowsTexture);
            }

            builder.SetRenderAttachment(color, 0);
            builder.SetRenderAttachmentDepth(depth);
            builder.SetGlobalTextureAfterPass(color, InsideTextureId);
            builder.SetGlobalTextureAfterPass(depth, InsideDepthId);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1f, 0);
                context.cmd.DrawRendererList(data.Opaque);
                context.cmd.DrawRendererList(data.Transparent);
            });
        }
    }
}
