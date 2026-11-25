using UnityEngine;

namespace HoldTheMine
{
    /// <summary>Isometric follow camera with a slight lead towards the mouse aim point.</summary>
    public class IsoCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 34f;
        public float pitch = 50f;
        public float yaw = 45f;
        public float smooth = 8f;
        public float aimLead = 0.18f;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 focus = target.position;
            var pc = target.GetComponent<PlayerController>();
            if (pc != null && GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                Vector3 lead = pc.AimPoint - target.position; lead.y = 0;
                focus += Vector3.ClampMagnitude(lead * aimLead, 5f);
            }
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 desired = focus - rot * Vector3.forward * distance;
            float k = 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
            transform.rotation = rot;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            transform.SetPositionAndRotation(target.position - rot * Vector3.forward * distance, rot);
        }
    }
}
