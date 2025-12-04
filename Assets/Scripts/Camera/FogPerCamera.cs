using UnityEngine;
using UnityEngine.Rendering;

public class FogPerCamera : MonoBehaviour
{
    public Camera targetCamera;
    private bool originalFogSetting;

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == targetCamera)
        {
            // Store the current setting to restore later
            originalFogSetting = RenderSettings.fog;
            // Disable fog for the specific camera
            RenderSettings.fog = false;
        }
    }
    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == targetCamera)
        {
            // Restore the original fog setting
            RenderSettings.fog = originalFogSetting;
        }
    }
}
