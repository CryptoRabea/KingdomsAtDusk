

using RTS.FogOfWar;
using System.Collections.Generic;   // List
using System.Linq;                  // ToList
using UnityEngine;                  // Monobehaviour



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
            if (fogWar == null || fogWar.CheckWorldGridRange(transform.position) == false)
            {
                return;
            }

            visibility = fogWar.CheckVisibility(transform.position, additionalRadius);

            foreach (MeshRenderer renderer in meshRenderers)
            {
                renderer.enabled = visibility;
            }

            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                renderer.enabled = visibility;
            }
        }



#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (fogWar == null || Application.isPlaying == false)
            {
                return;
            }

            if (fogWar.CheckWorldGridRange(transform.position) == false)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(
                    transform.position, // Use the actual object position
                    (additionalRadius + 0.5f) * fogWar._UnitScale); // Calculate radius based on grid units
                return;
            }

            if (fogWar.CheckVisibility(transform.position, additionalRadius) == true)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }
            Gizmos.DrawWireSphere(
                transform.position, // Use the actual object position
                (additionalRadius + 0.5f) * fogWar._UnitScale); // Calculate radius based on grid units
        }
#endif
    }



}