using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Eco.Scripts.Jelly
{
    /// <summary>
    /// Draws swallowed items into their own texture, which the Eco/Jelly shader shows through the body. Items then
    /// only appear where the jelly is: transparent ones included, and parts poking through the surface get the
    /// same jelly look while anything outside the body's outline isn't drawn.
    /// Add it to every URP renderer and take the same layer out of the renderer's Opaque and Transparent Layer
    /// Masks, or swallowed items show twice (layer still in the masks) or not at all (feature missing).
    /// </summary>
    public class JellyInsideFeature : ScriptableRendererFeature
    {
        [Tooltip("Layer swallowed items are moved to (see Stomach).")]
        [SerializeField] private LayerMask layerMask;
        [Tooltip("Resolution of the inside texture relative to the camera.")]
        [SerializeField, Range(0.25f, 1f)] private float resolutionScale = 1f;

        private static readonly int InsideTextureId = Shader.PropertyToID("_JellyInsideTexture");

        private JellyInsidePass _pass;

        /// <summary>Set by the stomach; nothing to draw while it is empty, so the pass is skipped.</summary>
        public static bool IsEmpty { get; set; } = true;

        public override void Create()
        {
            _pass = new JellyInsidePass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraType = renderingData.cameraData.camera.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            {
                return;
            }

            if (IsEmpty)
            {
                // Don't leave the jelly reading last frame's (released) texture
                Shader.SetGlobalTexture(InsideTextureId, Texture2D.blackTexture);
                return;
            }

            _pass.Setup(layerMask, resolutionScale);
            renderer.EnqueuePass(_pass);
        }
    }
}
