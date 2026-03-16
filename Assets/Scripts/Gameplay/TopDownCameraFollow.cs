using UnityEngine;

namespace Bomber.Gameplay
{
    public sealed class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -11f);
        [SerializeField] private float followLerp = 8f;
        [SerializeField] private Vector3 fixedEulerAngles = new Vector3(58f, 0f, 0f);

        private Transform target;
        private Quaternion fixedRotation;

        public void Initialize(Transform followTarget, Vector3 cameraOffset, float lerpSpeed, Vector3 cameraEulerAngles)
        {
            target = followTarget;
            offset = cameraOffset;
            followLerp = lerpSpeed;
            fixedEulerAngles = cameraEulerAngles;
            fixedRotation = Quaternion.Euler(fixedEulerAngles);
            transform.rotation = fixedRotation;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followLerp * Time.deltaTime);
            transform.rotation = fixedRotation;
        }
    }
}
