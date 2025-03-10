using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cool.Dcm.Game.PinBall
{
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
        private BallController currentBall;

        private Vector3 currentDirection;
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
            if(isPressed&&currentBall){
                currentBall.Launch(currentDirection,hitForce);
            }
        }

        bool IsAngleGreaterThanThreshold(float currentAngle, float targetAngle, float threshold = 0.1f)
        {
            return Mathf.Abs(currentAngle - targetAngle) > threshold;
        }

    }

}
