using Eco.Scripts.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Eco.Scripts.Rendering
{
    public sealed class CurvedAtmosphereFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material material;
        private CurvedAtmospherePass _pass;

        public void SetMaterial(Material value) => material = value;

        public override void Create()
        {
            _pass = new CurvedAtmospherePass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            var controller = CurvedWorldController.Active;
            if (material == null || controller == null || controller.Settings == null
                || !controller.Settings.AtmosphereEnabled || controller.Settings.AtmosphereStrength <= 0
                || controller.BendParameters.z <= 0.00001f
                || camera.renderType == CameraRenderType.Overlay
                || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView))
                return;

            _pass.Setup(material, controller.BendParameters, controller.Settings);
            renderer.EnqueuePass(_pass);
        }
    }
}
