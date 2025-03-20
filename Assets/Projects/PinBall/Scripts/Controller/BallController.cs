using UnityEngine;

namespace Cool.Dcm.Game.PinBall
{
    public class BallController : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float gravityForce = 30f;
        [SerializeField] private float maxVelocity = 20f;
        [SerializeField] private float bounceForce = 5f;

        private Rigidbody rb;
        private ConstantForce force;
        private bool isLaunched = false;

        private void Awake()
        {
            force = GetComponent<ConstantForce>();
            if (force == null)
            {
                force = gameObject.AddComponent<ConstantForce>();
            }
            force.force = Vector3.back * gravityForce;
            force.enabled = false;

            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezePositionY;
            rb.isKinematic = true;
        }

        void FixedUpdate()
        {
            if (isLaunched)
            {
                // 限制最大速度
                if (rb.velocity.magnitude > maxVelocity)
                {
                    rb.velocity = rb.velocity.normalized * maxVelocity;
                }
            }
        }

        public void Launch(Vector3 direction, float force)
        {
            Debug.Log("Launch");
            isLaunched = true;
            rb.isKinematic = false;
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

        public void ResetBall(Vector3 pos = new Vector3(), Quaternion rot = new Quaternion())
        {
            isLaunched = false;
            if(force != null)
            {
                force.enabled = false;
            }
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                rb.isKinematic = true; 
            }
            transform.position = pos;
            transform.rotation = rot;
        }

        void OnCollisionStay(Collision collision) 
        {
            if (collision.gameObject.CompareTag("Ground") && !force.enabled) 
            {
                force.enabled = true;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                // 添加反弹效果
                Vector3 bounceDirection = Vector3.Reflect(rb.velocity.normalized, collision.contacts[0].normal);
                rb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
            }
        }
    }
}
