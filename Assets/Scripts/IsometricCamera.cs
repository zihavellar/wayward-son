using UnityEngine;

namespace WaywardSon
{
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;
        public float smoothSpeed = 0.125f;
        
        [Header("Offset Settings")]
        public Vector3 offset = new Vector3(-8f, 10f, -8f);

        private void Start()
        {
            // Position camera initially
            if (target != null)
            {
                transform.position = target.position + offset;
                transform.LookAt(target);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Target position based on offset
            Vector3 desiredPosition = target.position + offset;
            
            // Smooth interpolation
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Maintain rotation pointing at target
            transform.LookAt(target.position);
        }
    }
}
