using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace LylekGames.Tools
{
    public class MouseLook : MonoBehaviour
    {
        public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
        public RotationAxes axes = RotationAxes.MouseXAndY;
        public float sensitivityX = 2F;
        public float sensitivityY = 2F;

        public float minimumX = -360F;
        public float maximumX = 360F;

        public float minimumY = -60F;
        public float maximumY = 60F;

        private float _sensX;
        private float _sensY;

        float rotationY = 0F;

        public float GetYRotation
        {
            get
            {
                return rotationY;
            }
        }
        public void Start()
        {
            _sensX = sensitivityX;

            _sensY = sensitivityY;
        }
        public void SetYAxis(float min, float max)
        {
            minimumY = min;

            maximumY = max;

            if (minimumY == 0 && maximumY == 0)
            {
                sensitivityY = 0;
            }
            else
            {
                sensitivityY = _sensY;
            }
        }
        public void SetXAxis(float min, float max)
        {
            minimumX = min;

            maximumX = max;

            if (minimumX == 0 && maximumX == 0)
            {
                sensitivityX = 0;
            }
            else
            {
                sensitivityX = _sensX;
            }
        }
        void Update()
        {
            if (Mouse.current == null) return;

            // Get mouse delta and scale it to match the old Input.GetAxis behavior
            float mouseX = Mouse.current.delta.x.ReadValue() * 0.02f;
            float mouseY = Mouse.current.delta.y.ReadValue() * 0.02f;

            if (axes == RotationAxes.MouseXAndY)
            {
                float rotationX = transform.localEulerAngles.y + mouseX * sensitivityX;

                rotationY += mouseY * sensitivityY;

                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
            }
            else if (axes == RotationAxes.MouseX)
            {
                transform.Rotate(0, mouseX * sensitivityX, 0);
            }
            else
            {
                rotationY += mouseY * sensitivityY;

                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
            }
        }
    }
}