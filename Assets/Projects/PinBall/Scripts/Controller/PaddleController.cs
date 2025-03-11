using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cool.Dcm.Game.PinBall
{
    struct BallHitData{
        public Vector3 hitDirection;
        public float hitForce;
    }
    public class PaddleController : MonoBehaviour
    {
        [Header("Collision Settings")]
        [SerializeField] private float hitForce = 150f;
        [SerializeField] private float lineDuration = 0.5f;
        [Header("Rotation Settings")]
        [SerializeField] private float maxAngle = 60f; // 挡板抬起角度
        [SerializeField] private float minAngle = 0f;  // 挡板默认角度
        [SerializeField] private float rotateTime = 1f; // 增加旋转速度
        
        [SerializeField] private GameObject paddle;
        private Dictionary<BallController,BallHitData> ballHitData = new Dictionary<BallController, BallHitData>();

        private Rigidbody rb;

        private float currentRotateTime = 0;
        private float targetAngle;
        
        private void Awake()
        {
            targetAngle = minAngle;
        }
        

        private void Start()
        {

            if(paddle == null)
            {
                paddle = gameObject;
            }
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.isKinematic = true;
        }

        private void Update()
        {
            if (IsAngleGreaterThanThreshold(paddle.transform.eulerAngles.y, targetAngle,0.01f))
            {
                // 获取当前角度并处理360度环绕
                float currentAngle = paddle.transform.eulerAngles.y;
                currentAngle = Mathf.Repeat(currentAngle + 180f, 360f) - 180f;
            
                float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, currentRotateTime/rotateTime);
                currentRotateTime += Time.deltaTime;
                paddle.transform.rotation = Quaternion.Euler(0, smoothAngle, 0);
            }
        }

        public void RotatePaddle(bool isPressed)
        {
            // 根据按钮状态设置目标角度
            targetAngle = isPressed ? maxAngle : minAngle;
            currentRotateTime = 0;
            if(isPressed&&ballHitData.Count>0){
                foreach(var ball in ballHitData){
                    ball.Key.Launch(ball.Value.hitDirection,ball.Value.hitForce);
                }
            }
        }

        bool IsAngleGreaterThanThreshold(float currentAngle, float targetAngle, float threshold = 0.1f)
        {
            return Mathf.Abs(currentAngle - targetAngle) > threshold;
        }


        private void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.CompareTag("Ball")){
                BallController controller = collision.gameObject.GetComponent<BallController>();
                if(ballHitData.ContainsKey(controller)){
                    ballHitData.Remove(controller);
                }else{
                    ballHitData.Add(controller,new BallHitData());
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            // 离开碰撞时触发（可选）
            if(collision.gameObject.CompareTag("Ball")){
                BallController controller = collision.gameObject.GetComponent<BallController>();
                if(ballHitData.ContainsKey(controller)){
                    ballHitData.Remove(controller);
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            // 持续碰撞时触发（可选）
            ProcessCollision(collision);
        }

        private void ProcessCollision(Collision collision)
        {
            BallController controller = collision.gameObject.GetComponent<BallController>();
            if(ballHitData.ContainsKey(controller) && collision.contactCount > 0)
            {
                // 获取第一个接触点的法线方向
                Vector3 hitNormal = collision.contacts[0].normal;
                Vector3 worldHitDirection = transform.TransformDirection(hitNormal).normalized;
                
                // 更新击打数据
                BallHitData newData = new BallHitData
                {
                    hitDirection = worldHitDirection,
                    hitForce = hitForce
                };
                ballHitData[controller] = newData;

                // 当挡板抬起时绘制轨迹
                if (Mathf.Approximately(targetAngle, maxAngle))
                {
                    Vector3 hitPoint = collision.contacts[0].point;
                    Debug.DrawLine(hitPoint, hitPoint + worldHitDirection * 2f,
                                 Color.red, lineDuration);
                }
            }
        }

    }

}
