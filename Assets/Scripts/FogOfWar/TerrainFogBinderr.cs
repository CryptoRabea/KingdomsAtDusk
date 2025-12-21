using UnityEngine;
using RTS.Core;

[RequireComponent(typeof(Terrain))]
public class TerrainFogBinder : MonoBehaviour
{
    [Header("Assigned at runtime")]
    public Texture2D fogTexture;

    private Terrain terrain;
    private Material terrainMat;
    private PlayAreaBounds playAreaBounds;

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

    public void SetPlayAreaBounds(PlayAreaBounds bounds)
    {
        playAreaBounds = bounds;
    }

    public void ApplyFog()
    {
        if (fogTexture == null)
        {
            Debug.LogError("[TerrainFogBinder] Fog texture is NULL");
            return;
        }

        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Point;

        // Assign texture
        terrainMat.SetTexture("_FogTex", fogTexture);

        // Use play area bounds if available, otherwise fall back to terrain size
        Vector2 worldSize;
        Vector2 worldCenter;

        if (playAreaBounds != null)
        {
            worldSize = playAreaBounds.Size;
            worldCenter = new Vector2(playAreaBounds.Center.x, playAreaBounds.Center.z);
        }
        else
        {
            Vector3 size = terrain.terrainData.size;
            worldSize = new Vector2(size.x, size.z);
            // Terrain origin is at its transform position
            worldCenter = new Vector2(
                terrain.transform.position.x + size.x * 0.5f,
                terrain.transform.position.z + size.z * 0.5f
            );
        }

        // Push world size and center to shader
        terrainMat.SetVector("_FogWorldSize", worldSize);
        terrainMat.SetVector("_FogWorldCenter", worldCenter);

        if (terrainMat.GetTexture("_FogTex") == null)
        {
            Debug.LogError("[TerrainFogBinder] Shader does NOT have _FogTex");
        }
    }
}
