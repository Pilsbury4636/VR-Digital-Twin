using UnityEngine;

namespace UR5eScanningBridge
{
    /// <summary>
    /// Gripper attach controller using TRANSFORM PARENTING instead of FixedJoint.
    ///
    /// FixedJoint requires a Rigidbody on the same GameObject, which conflicts
    /// with the ArticulationBody on the gripper base. Instead, when the
    /// gripper closes on a grippable, we parent the grippable's transform to
    /// the gripper and set its Rigidbody to kinematic. On release, we unparent
    /// and turn kinematic off.
    ///
    /// Setup:
    ///   1. Attach to robot root GameObject.
    ///   2. Create a tag "Grippable".
    ///   3. Tag any object with a Rigidbody + Collider as "Grippable".
    /// </summary>
    public class GripperAttachController : MonoBehaviour
    {
        [Header("Gripper")]
        [Tooltip("ArticulationBody of robotiq_85_left_knuckle_joint. Auto-found if empty.")]
        public ArticulationBody drivenKnuckleJoint;

        [Tooltip("Transform to parent picked objects under. Usually the gripper base link.")]
        public Transform attachPoint;

        [Header("Thresholds (radians on driven knuckle)")]
        public float attachThreshold = 0.4f;
        public float releaseThreshold = 0.2f;

        [Header("Detection")]
        public float detectionRadius = 0.15f;
        public string grippableTag = "Grippable";

        [Header("Debug")]
        public bool verboseLog = false;

        private Rigidbody attachedObject;
        private Transform originalParent;
        private bool originalKinematic;
        private bool originalUseGravity;

        void Start()
        {
            if (drivenKnuckleJoint == null) AutoFindKnuckleJoint();

            if (attachPoint == null && drivenKnuckleJoint != null)
            {
                Transform t = drivenKnuckleJoint.transform;
                while (t != null && !t.name.Contains("robotiq_85_base"))
                {
                    t = t.parent;
                }
                attachPoint = t != null ? t : drivenKnuckleJoint.transform;
            }

            if (drivenKnuckleJoint == null)
            {
                Debug.LogError("[GripperAttach] Could not find driven knuckle joint");
                enabled = false;
            }
            else
            {
                Debug.Log($"[GripperAttach] Watching {drivenKnuckleJoint.name}, " +
                          $"attach point: {attachPoint?.name}");
            }
        }

        private void AutoFindKnuckleJoint()
        {
            ArticulationBody[] allBodies = GetComponentsInChildren<ArticulationBody>();
            foreach (var body in allBodies)
            {
                if (body.name.Contains("robotiq_85_left_knuckle") ||
                    body.name == "robotiq_85_left_knuckle_link")
                {
                    drivenKnuckleJoint = body;
                    return;
                }
            }
        }

        void FixedUpdate()
        {
            if (drivenKnuckleJoint == null) return;

            float currentAngle = drivenKnuckleJoint.jointPosition[0];
            bool isClosing = currentAngle > attachThreshold;
            bool isOpening = currentAngle < releaseThreshold;

            if (isClosing && attachedObject == null)
            {
                TryAttachNearestGrippable();
            }

            if (isOpening && attachedObject != null)
            {
                ReleaseAttached();
            }
        }

        private void TryAttachNearestGrippable()
        {
            if (attachPoint == null) return;

            Collider[] hits = Physics.OverlapSphere(attachPoint.position, detectionRadius);
            Rigidbody closest = null;
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.CompareTag(grippableTag)) continue;
                Rigidbody rb = hit.attachedRigidbody;
                if (rb == null) continue;

                float dist = Vector3.Distance(hit.transform.position, attachPoint.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = rb;
                }
            }

            if (closest != null)
            {
                AttachObject(closest);
            }
            else if (verboseLog)
            {
                Debug.Log("[GripperAttach] Closing but no grippable in range");
            }
        }

        private void AttachObject(Rigidbody rb)
        {
            // Save original state
            originalParent = rb.transform.parent;
            originalKinematic = rb.isKinematic;
            originalUseGravity = rb.useGravity;

            // Disable physics on the object so it just follows the gripper
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Parent under the gripper attach point
            rb.transform.SetParent(attachPoint, worldPositionStays: true);

            attachedObject = rb;
            Debug.Log($"[GripperAttach] Attached '{rb.name}'");
        }

        private void ReleaseAttached()
        {
            if (attachedObject == null) return;

            // Restore original parent and physics state
            attachedObject.transform.SetParent(originalParent, worldPositionStays: true);
            attachedObject.isKinematic = originalKinematic;
            attachedObject.useGravity = originalUseGravity;

            Debug.Log($"[GripperAttach] Released '{attachedObject.name}'");
            attachedObject = null;
            originalParent = null;
        }

        void OnDrawGizmosSelected()
        {
            if (attachPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(attachPoint.position, detectionRadius);
            }
        }
    }
}