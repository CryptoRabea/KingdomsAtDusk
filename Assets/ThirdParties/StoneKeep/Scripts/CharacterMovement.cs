using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LylekGames.Tools
{
    public class CharacterMovement : MonoBehaviour
    {
        public CharacterController controller;

        public float speed = 3;

        private Vector3 gravity = new Vector3(0, -9.81f, 0);

        public void Update()
        {
            controller.Move(gravity * Time.deltaTime);

            if (Keyboard.current == null) return;

            if (Keyboard.current[Key.W].isPressed)
            {
                controller.Move(transform.forward * speed * Time.deltaTime);
            }
            else if (Keyboard.current[Key.A].isPressed)
            {
                controller.Move(-transform.right * speed * Time.deltaTime);
            }
            else if (Keyboard.current[Key.D].isPressed)
            {
                controller.Move(transform.right * speed * Time.deltaTime);
            }
            else if (Keyboard.current[Key.S].isPressed)
            {
                controller.Move(-transform.forward * speed * Time.deltaTime);
            }
        }
    }
}