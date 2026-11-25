using UnityEngine;

namespace MobaGame.Core
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private float shakeDuration = 0f;
        private float shakeIntensity = 0f;

        public Vector3 CurrentOffset { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);
        }

        /// <summary>
        /// Triggers a gentle screen shake. Intensity is clamped to prevent wild camera jumps.
        /// </summary>
        public void TriggerShake(float duration, float intensity)
        {
            // Clamp intensity to prevent jarring camera movement in third person
            shakeDuration = Mathf.Min(duration, 0.3f);
            shakeIntensity = Mathf.Min(intensity, 0.15f);
        }

        private void Update()
        {
            if (shakeDuration > 0f)
            {
                Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
                shakeOffset.z = 0f;
                CurrentOffset = shakeOffset;
                shakeDuration -= Time.deltaTime;
                if (shakeDuration <= 0f)
                {
                    CurrentOffset = Vector3.zero;
                }
            }
            else
            {
                CurrentOffset = Vector3.zero;
            }
        }
    }
}
