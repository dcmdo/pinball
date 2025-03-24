using UnityEngine;

namespace Cool.Dcm.Game.PinBall
{
    public class BallController : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float gravityForce = 30f;
        [SerializeField] private float ballLimitHight = 4;

        private Rigidbody rb;
        private ConstantForce force;
        private bool isLaunched = false;
        private Vector3 bounceDirection;

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
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezePositionY;
            rb.useGravity = true;
            rb.isKinematic = true;
        }
        void FixedUpdate()
        {
            if(isLaunched&&transform.position.y<ballLimitHight)
            {
                
                if (!force.enabled) {
                    force.enabled = true;
                    rb.constraints = RigidbodyConstraints.FreezePositionY;
                }
            }
        }
        // void FixedUpdate()
        // {
        //     if (isLaunched)
        //     {
        //         // 限制最大速度
        //         if (rb.velocity.magnitude > maxVelocity)
        //         {
        //             rb.velocity = rb.velocity.normalized * maxVelocity;
        //         }
        //     }
        // }

        public void Hit(Vector3 direction, float force)
        {
            rb.isKinematic = false;
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
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

        public void setDragRigidbody(bool isDrag)
        {
            rb.isKinematic = isDrag; // 关闭运动学以允许物理移动
            rb.constraints = isDrag?RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation:RigidbodyConstraints.None;
        }

        public void MovePosition(Vector3 pos)
        {
            rb.MovePosition(pos);
        }


        void OnCollisionEnter(Collision other)
        {
            PaddleController paddleController = other.gameObject.GetComponentInParent<PaddleController>();
            if(paddleController != null)
            {
               // 获取碰撞点的法线
                ContactPoint contact = other.contacts[0];
                Vector3 normal = contact.normal;

                // 获取入射方向并计算反射方向
                bounceDirection = normal;
                paddleController.BallEnter(this, bounceDirection);
            }
        }

        void OnCollisionStay(Collision other)
        {
            PaddleController paddleController = other.gameObject.GetComponentInParent<PaddleController>();
            if(paddleController != null)
            {
                paddleController.BallUpdate(this, bounceDirection);
                Debug.Log($"BallEnter=={bounceDirection}");
            }
        }
        void OnCollisionExit(Collision other)
        {
            if (other.gameObject.CompareTag("Ground")) {
                force.enabled = false;
            }
            PaddleController paddleController = other.gameObject.GetComponentInParent<PaddleController>();
            if(paddleController != null)
            {
                paddleController.BallExit(this);
            }
            
        }

        // void OnDrawGizmos()
        // {
        //     // 设置 Gizmo 的颜色
        //     Gizmos.color = Color.red;

        //     // 绘制从弹球位置到反弹方向的线
        //     Gizmos.DrawLine(transform.position, transform.position + bounceDirection * 2f);

        //     // 绘制反弹方向的箭头
        //     DrawArrowForGizmo(transform.position, bounceDirection.normalized * 2f, Color.red);
        // }

        // // 绘制箭头的辅助方法
        // void DrawArrowForGizmo(Vector3 pos, Vector3 direction, Color color)
        // {
        //     Gizmos.color = color;
        //     Gizmos.DrawRay(pos, direction*10);

        //     Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * new Vector3(0, 0, 1);
        //     Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * new Vector3(0, 0, 1);
        //     Gizmos.DrawRay(pos + direction*10, right * 1.2f);
        //     Gizmos.DrawRay(pos + direction*10, left * 1.2f);
        // }
    }
}
