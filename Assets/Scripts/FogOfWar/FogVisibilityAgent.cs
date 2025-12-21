

using UnityEngine;                  // Monobehaviour
using System.Collections.Generic;   // List
using System.Linq;                  // ToList



namespace RTS.FogOfWar
{



    public class FogVisibilityAgent : MonoBehaviour
    {
        [SerializeField]
        private RTS_FogOfWar fogWar = null;

        [SerializeField]
        private bool visibility = false;

        [SerializeField]
        [Range(0, 2)]
        private int additionalRadius = 0;

        private List<MeshRenderer> meshRenderers = null;
        private List<SkinnedMeshRenderer> skinnedMeshRenderers = null;



        private void Start()
        {
            // This part is meant to be modified following the project's scene structure later...
            try
            {
                fogWar = FindAnyObjectByType<RTS_FogOfWar>();
            }
            catch
            {
            }

            meshRenderers = GetComponentsInChildren<MeshRenderer>().ToList();
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
        }

        private void OnEnable()
        {
            // If fogWar is still null after Start (or if it was never assigned), disable this component to prevent errors.
            if (fogWar == null) enabled = false;
        }



        private void Update()
        {
            if (fogWar == null)
                return;

            // Terrain fog is shader-based.
            // Unit visibility is handled elsewhere.
            visibility = true;

            foreach (MeshRenderer r in meshRenderers)
                r.enabled = visibility;

            foreach (SkinnedMeshRenderer r in skinnedMeshRenderers)
                r.enabled = visibility;
        }




    }
}