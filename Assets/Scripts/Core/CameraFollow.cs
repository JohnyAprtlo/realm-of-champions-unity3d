using UnityEngine;

namespace MobaGame.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Third Person Settings")]
        public float distance = 11f;
        public float heightOffset = 1.8f;
        public float followSpeed = 18f;

        [Header("Zoom (Scroll Wheel)")]
        public float minDistance = 6f;
        public float maxDistance = 20f;
        public float zoomSpeed = 5f;

        [Header("Orbit (Hold Right Mouse Button)")]
        public float mouseSensitivity = 3.5f;
        public float minPitch = 15f;
        public float maxPitch = 65f;

        private float yaw = 45f;
        private float pitch = 32f;
        private Vector3 currentLookTarget;
        private bool initialized = false;
        private CameraShake cameraShake;

        public Vector3 ForwardFlat => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 RightFlat => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        private void Start()
        {
            // Keep initial camera angle facing forward along the map (no auto-spinning with WASD)
            yaw = 45f;
            pitch = 32f;
            cameraShake = GetComponent<CameraShake>();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            currentLookTarget = target.position + Vector3.up * heightOffset;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = currentLookTarget + rotation * (Vector3.back * distance);
            transform.rotation = rotation;
            initialized = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (!initialized) SnapToTarget();

            // Scroll wheel to zoom in/out
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }

            // Hold Right Mouse Button to rotate camera freely (never auto-spins when pressing A/S/D)
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Exponentially smooth target tracking; rotation stays strictly locked to (pitch, yaw)
            Vector3 desiredLookTarget = target.position + Vector3.up * heightOffset;
            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            currentLookTarget = Vector3.Lerp(currentLookTarget, desiredLookTarget, t);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = currentLookTarget + rotation * (Vector3.back * distance);
            if (desiredPosition.y < 1.5f) desiredPosition.y = 1.5f;

            if (cameraShake != null)
            {
                desiredPosition += rotation * cameraShake.CurrentOffset;
            }

            transform.position = desiredPosition;
            transform.rotation = rotation;
        }
    }
}
