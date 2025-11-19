using UnityEngine;

public class GPUDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== GPU DIAGNOSTIC ===");
        Debug.Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
        Debug.Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
        Debug.Log($"Graphics Vendor: {SystemInfo.graphicsDeviceVendor}");
        Debug.Log($"Graphics Version: {SystemInfo.graphicsDeviceVersion}");
        Debug.Log($"Max Texture Size: {SystemInfo.maxTextureSize}");
        Debug.Log($"Supports Compute Shaders: {SystemInfo.supportsComputeShaders}");
        Debug.Log("=====================");
    }
}