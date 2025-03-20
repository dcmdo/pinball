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
        [Header("Detection Settings")]
        [SerializeField] private float contactThreshold = 0.1f; // 接触检测阈值
        
        [SerializeField] private GameObject paddle;
        [SerializeField] private LineRenderer trajectoryLine;
        private Dictionary<BallController,BallHitData> ballHitData = new Dictionary<BallController, BallHitData>();
        private List<BallController> nearbyBalls = new List<BallController>();

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
            ;

            initLine();
            
        }

        private void initLine(){
            trajectoryLine = GetComponent<LineRenderer>();
            if(trajectoryLine == null){
                trajectoryLine = gameObject.AddComponent<LineRenderer>();
            }
            // 初始化颜色渐变
            // 创建更平滑的颜色渐变
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.cyan, 0f),
                    new GradientColorKey(Color.magenta, 0.3f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0.5f, 1f)
                }
            );
            trajectoryLine.colorGradient = gradient;

            // 添加动态轨迹效果
            trajectoryLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply")) {
                color = Color.white,
                renderQueue = 3000
            };
            trajectoryLine.generateLightingData = true;
            trajectoryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trajectoryLine.receiveShadows = false;
            
            // 初始化线宽曲线
            AnimationCurve widthCurve = new AnimationCurve(
                new Keyframe(0, 0.2f),
                new Keyframe(1, 0.05f)
            );
            trajectoryLine.widthCurve = widthCurve;
            
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default")) {
                color = Color.white,
                renderQueue = 3000 // 确保透明效果正确
            };
        }

        private void Update()
        {
            if (IsAngleGreaterThanThreshold(paddle.transform.eulerAngles.y, targetAngle,0.01f))
            {
                float currentAngle = paddle.transform.eulerAngles.y;
                currentAngle = Mathf.Repeat(currentAngle + 180f, 360f) - 180f;
            
                float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, currentRotateTime/rotateTime);
                currentRotateTime += Time.deltaTime;
                paddle.transform.rotation = Quaternion.Euler(0, smoothAngle, 0);
            }

            // 检测附近的小球
            CheckNearbyBalls();
        }

        private void CheckNearbyBalls()
        {
            // 获取所有带有BallController组件的物体
            BallController[] allBalls = FindObjectsOfType<BallController>();
            nearbyBalls.Clear();

            foreach (var ball in allBalls)
            {
                if (IsBallInContact(ball))
                {
                    nearbyBalls.Add(ball);
                    if (!ballHitData.ContainsKey(ball))
                    {
                        Vector3 hitDirection = CalculateHitDirection(ball);
                        ballHitData.Add(ball, new BallHitData
                        {
                            hitDirection = hitDirection,
                            hitForce = hitForce
                        });
                    }
                }
            }

            // 移除不再接触的小球
            List<BallController> ballsToRemove = new List<BallController>();
            foreach (var ball in ballHitData.Keys)
            {
                if (!nearbyBalls.Contains(ball))
                {
                    ballsToRemove.Add(ball);
                }
            }
            foreach (var ball in ballsToRemove)
            {
                ballHitData.Remove(ball);
            }
        }

        private bool IsBallInContact(BallController ball)
        {
            Vector3 ballPosition = ball.transform.position;
            Vector3 paddlePosition = paddle.transform.position;
            Vector3 paddleUp = paddle.transform.up;
            Vector3 paddleForward = paddle.transform.forward;

            // 获取小球的SphereCollider组件
            SphereCollider ballCollider = ball.GetComponent<SphereCollider>();
            if (ballCollider == null) return false;

            // 计算实际半径（考虑缩放）
            float ballRadius = ballCollider.radius * Mathf.Max(
                ball.transform.lossyScale.x,
                ball.transform.lossyScale.y,
                ball.transform.lossyScale.z
            );

            // 计算小球到挡板平面的距离
            float distanceToPlane = Vector3.Dot(ballPosition - paddlePosition, paddleUp);
            
            // 检查小球是否在挡板的前面
            float forwardDistance = Vector3.Dot(ballPosition - paddlePosition, paddleForward);
            
            // 检查小球是否在挡板的范围内（可以根据需要调整范围）
            float paddleWidth = 2f; // 挡板宽度
            float paddleLength = 1f; // 挡板长度
            
            bool isInWidthRange = Mathf.Abs(Vector3.Dot(ballPosition - paddlePosition, paddle.transform.right)) < paddleWidth / 2;
            bool isInLengthRange = forwardDistance > 0 && forwardDistance < paddleLength;
            
            // 如果小球在挡板范围内且距离小于阈值，则认为接触
            return isInWidthRange && isInLengthRange && Mathf.Abs(distanceToPlane) < (contactThreshold + ballRadius);
        }

        private Vector3 CalculateHitDirection(BallController ball)
        {
            Vector3 paddleUp = paddle.transform.up;
            Vector3 paddleForward = paddle.transform.forward;
            
            // 计算击打方向（基于挡板当前角度）
            Vector3 hitDirection = Vector3.Reflect(paddleForward, paddleUp);
            hitDirection = Quaternion.Euler(0, paddle.transform.eulerAngles.y, 0) * hitDirection;
            
            return hitDirection.normalized;
        }

        public void RotatePaddle(bool isPressed)
        {
            targetAngle = isPressed ? maxAngle : minAngle;
            currentRotateTime = 0;

            if (isPressed && ballHitData.Count > 0)
            {
                List<BallController> launchBalls = new List<BallController>();
                foreach (var ball in ballHitData)
                {
                    ball.Key.Launch(ball.Value.hitDirection, ball.Value.hitForce);
                    launchBalls.Add(ball.Key);
                }
                foreach (var ball in launchBalls)
                {
                    ballHitData.Remove(ball);
                }
            }
        }

        bool IsAngleGreaterThanThreshold(float currentAngle, float targetAngle, float threshold = 0.1f)
        {
            return Mathf.Abs(currentAngle - targetAngle) > threshold;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Ball")) return;
            
            var controller = collision.gameObject.GetComponent<BallController>();
            if (controller == null) return;

            // 计算平均接触点法线
            Vector3 averageNormal = Vector3.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                averageNormal += collision.GetContact(i).normal;
            }
            averageNormal = (averageNormal / collision.contactCount).normalized;

            // 计算碰撞速度
            float collisionSpeed = collision.impulse.magnitude / Time.fixedDeltaTime;
            
            // 检查碰撞是否来自挡板表面（法线方向应该大致垂直于挡板）
            float dotProduct = Vector3.Dot(averageNormal, transform.up);
            bool isFromPaddleSurface = Mathf.Abs(dotProduct) < 0.5f; // 允许一定的角度偏差
            
            // 只有当碰撞来自挡板表面且力度足够大时才记录
            if (isFromPaddleSurface && collisionSpeed > 0.1f && !ballHitData.ContainsKey(controller))
            {
                ballHitData.Add(controller, new BallHitData
                {
                    hitDirection = averageNormal,
                    hitForce = hitForce
                });
            }
        }
        // private void OnCollisionEnter(Collision collision)
        // {
        //     if(collision.gameObject.CompareTag("Ball")){
        //         BallController controller = collision.gameObject.GetComponent<BallController>();
        //         if(!ballHitData.ContainsKey(controller)){
        //             ballHitData.Add(controller,new BallHitData());
        //         }
        //     }
        // }

        private void OnCollisionExit(Collision collision)
        {
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }
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
            // 安全获取球控制器组件
            if (!collision.gameObject.TryGetComponent<BallController>(out var ballController))
                return;

            if (!ballHitData.ContainsKey(ballController) || collision.contactCount == 0)
                return;

            // 选择最深穿透的接触点
            ContactPoint deepestContact = collision.contacts[0];
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.separation < deepestContact.separation)
                {
                    deepestContact = contact;
                }
            }

            // 计算世界空间击打方向（考虑挡板当前旋转）
            Vector3 localHitDirection = deepestContact.normal;
            // Vector3 worldHitDirection = transform.TransformDirection(localHitDirection).normalized;

            // 计算带速度影响的击打力度（当前速度的20%会叠加到击打力）
            // float velocityFactor = Mathf.Clamp01(rb.velocity.magnitude * 0.2f);
            // float finalHitForce = hitForce * (1 + velocityFactor);

            // 更新击打数据
            ballHitData[ballController] = new BallHitData
            {
                hitDirection = localHitDirection,
                hitForce = hitForce
            };
            if(Vector3.Dot(Vector3.forward,transform.TransformVector(localHitDirection))>0){
                Vector3 hitPoint = deepestContact.point;
                trajectoryLine.enabled = true;
                trajectoryLine.positionCount = 2;
                trajectoryLine.SetPosition(0, hitPoint);
                trajectoryLine.SetPosition(1, hitPoint + -localHitDirection * 20f);
            }
            
        }

    }

}
