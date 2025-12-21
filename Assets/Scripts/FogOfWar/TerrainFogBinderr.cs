using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainFogBinder : MonoBehaviour
{
    [Header("Assigned at runtime")]
    public Texture2D fogTexture;

    private Terrain terrain;
    private Material terrainMat;

    void Awake()
    {
        terrain = GetComponent<Terrain>();

        if (terrain.materialTemplate == null)
        {
            Debug.LogError("[TerrainFogBinder] Terrain has NO materialTemplate");
            enabled = false;
            return;
        }

        terrainMat = terrain.materialTemplate;
    }

    public void ApplyFog()
    {
        if (fogTexture == null)
        {
            Debug.LogError("[TerrainFogBinder] Fog texture is NULL");
            return;
        }

        // 🔒 REQUIRED SETTINGS (EVERY TIME)
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Point;

        // 1️⃣ Assign texture
        terrainMat.SetTexture("_FogTex", fogTexture);

        // 2️⃣ Push world size
        Vector3 size = terrain.terrainData.size;
        terrainMat.SetVector("_FogWorldSize", new Vector2(size.x, size.z));

        // HARD ASSERT (this should NEVER be null)
        if (terrainMat.GetTexture("_FogTex") == null)
        {
            Debug.LogError("[TerrainFogBinder] Shader does NOT have _FogTex");
        }
    }
}
