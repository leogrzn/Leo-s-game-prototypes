using UnityEngine;
using UnityEngine.AI;

namespace HoldTheMine
{
    /// <summary>Swings the cube arms and legs of a blocky character while it moves.</summary>
    public class BlockyAnimator : MonoBehaviour
    {
        public Transform leftArm, rightArm, leftLeg, rightLeg;
        public bool armsHoldGun = true;

        NavMeshAgent agent;
        CharacterController cc;
        Vector3 lastPos;
        float phase;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            cc = GetComponent<CharacterController>();
            lastPos = transform.position;
        }

        void LateUpdate()
        {
            if (Time.deltaTime <= 0) return;
            Vector3 delta = transform.position - lastPos; delta.y = 0;
            lastPos = transform.position;
            float speed = delta.magnitude / Time.deltaTime;
            float amount = Mathf.Clamp01(speed / 3f);
            phase += speed * Time.deltaTime * 2.2f;
            float swing = Mathf.Sin(phase) * 40f * amount;
            if (leftLeg) leftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
            if (rightLeg) rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
            if (armsHoldGun)
            {
                if (leftArm) leftArm.localRotation = Quaternion.Euler(-70f, 0, 0);
                if (rightArm) rightArm.localRotation = Quaternion.Euler(-80f, 0, 0);
            }
            else
            {
                if (leftArm) leftArm.localRotation = Quaternion.Euler(-swing, 0, 0);
                if (rightArm) rightArm.localRotation = Quaternion.Euler(swing, 0, 0);
            }
        }
    }
}
