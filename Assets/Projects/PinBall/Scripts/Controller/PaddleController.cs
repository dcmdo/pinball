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
        [System.Serializable]
        private class BallTrajectory
        {
            public BallController ball;
            public LineRenderer lineRenderer;
            public Vector3 direction;
        }

        [Header("Visual Settings")]
        [SerializeField] private float dashLength = 0.2f;
        [SerializeField] private float gapLength = 0.1f;
        private Dictionary<BallController, BallTrajectory> trajectories = new Dictionary<BallController, BallTrajectory>();

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
                ShowTrajectoryLine(ball, direction);
            }
        }

        public void BallUpdate(BallController ball, Vector3 direction)
        {
            if (trajectories.ContainsKey(ball))
            {
                trajectories[ball].direction = direction;
            }
        }

        public void BallExit(BallController ball)
        {
            if (ballHitData.ContainsKey(ball))
            {
                ballHitData.Remove(ball);
                HideTrajectoryLine(ball);
            }
        }

        private void ShowTrajectoryLine(BallController ball, Vector3 direction)
        {
            // 如果已存在该小球的轨迹线，先移除
            if (trajectories.ContainsKey(ball))
            {
                Destroy(trajectories[ball].lineRenderer);
                trajectories.Remove(ball);
            }

            // 创建新的轨迹线
            LineRenderer lineRenderer = new GameObject($"TrajectoryLine_{ball.GetInstanceID()}").AddComponent<LineRenderer>();
            lineRenderer.transform.SetParent(transform);
            
            // 设置轨迹线属性
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.red;
            
            // 设置虚线效果
            lineRenderer.material.mainTextureScale = new Vector2(dashLength, 1);
            lineRenderer.material.mainTextureOffset = new Vector2(gapLength, 0);
            lineRenderer.useWorldSpace = true;

            // 生成虚线点
            int pointCount = 20;
            float totalLength = 50f;
            float segmentLength = totalLength / (pointCount - 1);
            
            Vector3[] points = new Vector3[pointCount];
            Vector3 startPos = ball.transform.position;
            for (int i = 0; i < pointCount; i++)
            {
                points[i] = startPos + direction * (i * segmentLength);
            }

            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPositions(points);

            // 保存轨迹线信息
            trajectories[ball] = new BallTrajectory
            {
                ball = ball,
                lineRenderer = lineRenderer,
                direction = direction
            };
        }

        private void HideTrajectoryLine(BallController ball)
        {
            if (trajectories.TryGetValue(ball, out BallTrajectory trajectory))
            {
                if (trajectory.lineRenderer != null)
                {
                    Destroy(trajectory.lineRenderer.gameObject);
                }
                trajectories.Remove(ball);
            }
        }

        private void OnDestroy()
        {
            // 清理所有轨迹线
            foreach (var trajectory in trajectories.Values)
            {
                if (trajectory.lineRenderer != null)
                {
                    Destroy(trajectory.lineRenderer.gameObject);
                }
            }
            trajectories.Clear();
        }
    }
}
