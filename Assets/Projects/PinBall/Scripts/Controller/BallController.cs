using UnityEngine;

namespace Cool.Dcm.Game.PinBall
{
    public class BallController : MonoBehaviour
    {
        private Rigidbody rb;
        private ConstantForce force;

        private void Awake()
        {
            force = GetComponent<ConstantForce>();
            if (force == null)
            {
                force = gameObject.AddComponent<ConstantForce>();
            }
            force.force = Vector3.back * 30;
            force.enabled = false; // Initially disabled

            
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

        }
        public void Launch(Vector3 direction,float force)
        {
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

        public void ResetBall(Vector3 pos = new Vector3(),Quaternion rot = new Quaternion())
        {
            if(force!=null)
            {
                force.enabled = false;
            }
            if(rb!=null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezeAll;
                rb.isKinematic = true; 
            }
            transform.position = pos;
            transform.rotation = rot;
        }
        // public Vector3 GetVelocity()
        // {
        //     return rb.velocity;
        // }

        // public void AddForce(Vector3 force, ForceMode mode)
        // {
        //     rb.AddForce(force, mode);
        // }


        void OnCollisionStay(Collision collision) {
            // Check if colliding with ground (you may want to use tags or layers)
            if (collision.gameObject.CompareTag("Ground")&&!force.enabled) {
                force.enabled = true;
            }
        }


        // void OnCollisionEnter(Collision other)
        // {
        //     PaddleController paddleController = other.gameObject.GetComponentInParent<PaddleController>();
        //     if(paddleController != null)
        //     {
        //        // 获取碰撞点的法线
        //         ContactPoint contact = other.contacts[0];
        //         Vector3 normal = contact.normal;

        //         // 获取入射方向并计算反射方向
        //         bounceDirection = normal;

        //         // 标记需要绘制 Gizmo
        //         paddleController.SetBall(this,bounceDirection);
        //         // paddleController.SetBall(this);
        //     }
        // }

        // void OnCollisionExit(Collision other)
        // {
        //     PaddleController paddleController = other.gameObject.GetComponentInParent<PaddleController>();
        //     if(paddleController != null)
        //     {
        //         paddleController.SetBall(null,Vector3.zero);
        //         // paddleController.SetBall(null);
        //     }
        //     if (other.gameObject.CompareTag("Ground")) {
        //     force.enabled = false;
        //     }
        // }

        // void OnDrawGizmos()
        // {
        //     // 设置 Gizmo 的颜色
        //         Gizmos.color = Color.red;

        //         // 绘制从弹球位置到反弹方向的线
        //         Gizmos.DrawLine(transform.position, transform.position + bounceDirection * 2f);

        //         // 绘制反弹方向的箭头
        //         DrawArrowForGizmo(transform.position, bounceDirection.normalized * 2f, Color.red);
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
