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
        [Header("Rotation Settings")]
        [SerializeField] private float maxAngle = 60f; // 挡板抬起角度
        [SerializeField] private float minAngle = 0f;  // 挡板默认角度
        [SerializeField] private float rotateTime = 1f; // 增加旋转速度
        [Header("Detection Settings")]
        [SerializeField] private float contactThreshold = 0.1f; // 接触检测阈值
        [SerializeField] private float rotateRatioThreshold = 0.95f; // 旋转比例阈值
        
        [SerializeField] private GameObject paddle;
        private Dictionary<BallController,BallHitData> ballHitData = new Dictionary<BallController, BallHitData>();

        private float currentRotateTime = 0;
        private float targetAngle;

        private bool isHitState = false;

        private bool isLift = false;
        
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
        }

        private void Update()
        {
            if (IsAngleGreaterThanThreshold(paddle.transform.eulerAngles.y, targetAngle,contactThreshold))
            {
                float currentAngle = paddle.transform.eulerAngles.y;
                currentAngle = Mathf.Repeat(currentAngle + 180f, 360f) - 180f;
                float stepValue  = currentRotateTime/rotateTime;
                float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle,stepValue);
                currentRotateTime += Time.deltaTime;
                paddle.transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

                isHitState = isLift?stepValue<rotateRatioThreshold:false;

                
                // 在向最大角度移动过程中检查并发射小球
                // if (isMovingToMax && ballHitData.Count > 0 && !hasReachedMax)
                // {
                //     LaunchBalls();
                //     // 检查当前角度是否接近最大角度
                //     if (Mathf.Abs(smoothAngle - maxAngle) < contactThreshold)
                //     {
                        
                //         hasReachedMax = true; // 标记已达到最大角度
                //     }
                // }
            }else{
                isHitState = false;
            }
            Debug.Log($"{this.name}-{isHitState}");
            if(isHitState&&ballHitData.Count>0){
                    LaunchBalls();
                }
        }

        public void RotatePaddle(bool isPressed)
        {
            targetAngle = isPressed ? maxAngle : minAngle;
            currentRotateTime = 0;
            isLift = isPressed;
            isHitState = isPressed; // 更新移动状态
        }

        private void LaunchBalls()
        {
            foreach (var ball in ballHitData)
            {
                ball.Key.Launch(ball.Value.hitDirection, ball.Value.hitForce);
            }
            ballHitData.Clear();
        }

        bool IsAngleGreaterThanThreshold(float currentAngle, float targetAngle, float threshold = 0.1f)
        {
            return Mathf.Abs(currentAngle - targetAngle) > threshold;
        }

        public void BallEnter(BallController ball, Vector3 direction)
        {
            if (!ballHitData.ContainsKey(ball))
            {
                ballHitData.Add(ball, new BallHitData
                {
                    hitDirection = direction,
                    hitForce = hitForce
                });
            }
        }

        public void BallExit(BallController ball)
        {
            if (ballHitData.ContainsKey(ball))
            {
                ballHitData.Remove(ball);
            }
        }
    }
}
