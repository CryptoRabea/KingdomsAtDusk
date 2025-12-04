using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LylekGames.Tools
{
    public class PlayerInteraction : MonoBehaviour
    {
        public GameObject raycastFrom;

        public Key interactKey;

        private RaycastHit hit;

        public void Update()
        {
            if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
            {
                if (Physics.Raycast(raycastFrom.transform.position, raycastFrom.transform.forward, out hit, 3f))
                {
                    if (hit.transform.tag == "Interact")
                    {
                        hit.transform.gameObject.SendMessage("Interact");
                    }
                }
            }
        }
    }
}