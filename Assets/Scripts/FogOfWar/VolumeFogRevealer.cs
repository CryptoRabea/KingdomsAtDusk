using UnityEngine;
using VolumetricFogAndMist2;

namespace VolumetricFogAndMist2.Demos {

    public class VolumeFogRevealer : MonoBehaviour {

        public VolumetricFog fogVolume;
        public float moveSpeed = 10f;
        public float fogHoleRadius = 8f;
        public float clearDuration = 0.2f;
        public float distanceCheck = 1f;

        Vector3 lastPos = new Vector3(float.MaxValue, 0, 0);

        void Update() {

            float disp = Time.deltaTime * moveSpeed;

           
            // do not call SetFogOfWarAlpha every frame; only when Object moves
            if ((transform.position - lastPos).magnitude > distanceCheck) {
                lastPos = transform.position;
                fogVolume.SetFogOfWarAlpha(transform.position, radius: fogHoleRadius, fogNewAlpha: 0, duration: clearDuration);
            }

        }
    }

}