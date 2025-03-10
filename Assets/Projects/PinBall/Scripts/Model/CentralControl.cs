using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cool.Dcm.Game.PinBall
{
    public class CentralControl : MonoBehaviour
    {
        public Transform targetObject; // 新增目标物体引用
        public PaddleController leftPaddle;
        public PaddleController rightPaddle;
        public BallController ball;
        // 私有静态实例变量
        private static CentralControl _instance;

        // 公共静态属性用于访问实例
        public static CentralControl Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CentralControl>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<CentralControl>();
                        singletonObject.name = typeof(CentralControl).ToString() + " (Singleton)";
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return _instance;
            }
        }

        // 私有构造函数防止外部实例化
        private CentralControl() { }

        private Vector3 offset;
        private float mouseZPos;
        private bool isDragging;
        private Vector3 initialBallPosition;
        private Vector3 newPos; 
        private Vector3 clampedDrag;
        private Vector3 resetPosition;
        private bool isAtResetPosition = false;
        private LineRenderer trajectoryLine; // 新增轨迹线渲染器
        
        [SerializeField]
        private float dragRadius = 2f;
        [SerializeField]
        [Range(180, 270)] 
        private float minAngle = 210f;
        [SerializeField]
        [Range(270, 360)]
        private float maxAngle = 330f;
        [SerializeField]
        private float minLaunchForce = 100f;
        [SerializeField] 
        private float maxLaunchForce = 200f;
        [SerializeField]
        [Range(0, 80)]
        private float launchAngle = 45f; // 发射角度参数
        [SerializeField]
        private AnimationCurve forceCurve = AnimationCurve.Linear(0, 0, 1, 1); // 力度控制曲线
        [SerializeField]
        private float minDragThreshold = 0.5f;
        [SerializeField]
        private LineRenderer dragAreaLine;

        [SerializeField] private PlayerInput playerInput;

        private bool isPaused = false;

        void Awake()
        {
            
            playerInput = GetComponent<PlayerInput>();
            playerInput.onActionTriggered +=HandleInput;
            // 初始化重置位置（动态计算目标物体顶部）
            CalculateResetPosition();
            
            // 初始化轨迹线
            trajectoryLine = gameObject.AddComponent<LineRenderer>();
            trajectoryLine.startWidth = 0.1f;
            trajectoryLine.endWidth = 0.1f;
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryLine.startColor = Color.yellow;
            trajectoryLine.endColor = Color.red;
            
            // 绘制可拖拽范围
            DrawDragArea();
        }

        void Update()
        {
            // HandleKeyboardInput();
            HandleMouseDrag();

            // 更新轨迹线
            if (isDragging && selectedObject != null)
            {
                UpdateTrajectory();
            }
            else
            {
                trajectoryLine.positionCount = 0;
            }
        }

        private void HandleInput(InputAction.CallbackContext context){
            switch (context.action.name)
            {
                case "LeftPaddle":
                    if (context.phase == InputActionPhase.Performed)
                    {
                        Debug.Log($"LeftPaddle Performed {context.ReadValueAsButton()}");
                        leftPaddle.RotatePaddle(context.ReadValueAsButton());
                    }
                    break;
                case "RightPaddle":
                    if (context.phase == InputActionPhase.Performed)
                    {
                        Debug.Log($"RightPaddle Performed {context.ReadValueAsButton()}");
                        rightPaddle.RotatePaddle(context.ReadValueAsButton());
                    }
                    break;
                case "Launch":
                // Debug.Log($"Launch {context.ReadValue<Vector2>()}");
                    break;
                case "Pause":
                    if (context.phase == InputActionPhase.Performed)
                    {
                        TogglePause();
                    }
                    break;
                case "ResetBall":
                    Debug.Log($"ResetBall {context.ReadValueAsButton()}");
                    RestBallPos();
                    break;
            }
            
        }

        void TogglePause()
        {
            isPaused = !isPaused;
            if (isPaused)
            {
                Time.timeScale = 0f;
                Debug.Log("游戏已暂停");
            }
            else
            {
                Time.timeScale = 1f;
                Debug.Log("游戏已继续"); 
            }
        }

        // 平滑释放协程
        void RestBallPos()
        {
            // 添加重置功能
            if (ball != null && targetObject != null)
                {
                    CameraController.Instance.SwitchToLaunchView();
                    isAtResetPosition= true;
                    CalculateResetPosition();
                    Debug.Log("Reset Ball Position");
                    // 应用底部对齐计算
                    var ballCollider = ball.GetComponent<SphereCollider>();
                    if (ballCollider != null)
                    {
                        float ballRadius = ballCollider.radius * ball.transform.lossyScale.y;
                        Vector3 finalPosition = resetPosition + Vector3.up * ballRadius;
                        ball.transform.position = finalPosition;
                        
                        // 调试日志
                        Debug.Log($"目标物体顶部位置: {resetPosition}");
                        Debug.Log($"小球半径: {ballRadius} (缩放系数: {ball.transform.lossyScale.y})");
                        Debug.Log($"最终重置位置: {finalPosition}");
                    }
                    else
                    {
                        ball.transform.position = resetPosition;
                        Debug.LogWarning("小球缺少SphereCollider组件，使用默认重置位置");
                    }
                    if (ball.GetComponent<Rigidbody>() != null)
                    {
                        var rb = ball.GetComponent<Rigidbody>();
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.constraints = RigidbodyConstraints.FreezeAll;
                        rb.isKinematic = true; // 设置为运动学模式以便拖动
                    }
                    isAtResetPosition = true; // 立即更新位置状态
                    // 重置拖动状态
                    isDragging = false;
                    selectedObject = null;
                    selectedRigidbody = null;
                    // 添加额外验证
                    Debug.Log($"物理状态已重置 速度:{ball.GetComponent<Rigidbody>().velocity} 角速度:{ball.GetComponent<Rigidbody>().angularVelocity}");
                    Debug.Log($"Ball reset to position. Ready to drag: {isAtResetPosition}");
                    Debug.Log($"实际位置差异: {Vector3.Distance(ball.transform.position, resetPosition)}");
                }
            
        }

        private Transform selectedObject;
        private Rigidbody selectedRigidbody; // 缓存刚体组件
        
        void HandleMouseDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    // 只有当小球在重置位置且静止时才能拖动（添加位置验证）
                    if(hit.transform.GetComponent<BallController>() != null && isAtResetPosition&& !isDragging) // 添加拖动状态检查
                    {
                        Debug.Log("Drag Ball");
                        selectedObject = hit.transform;
                        offset = selectedObject.position - GetMouseWorldPos();
                        selectedRigidbody = selectedObject.GetComponent<Rigidbody>();
                        initialBallPosition = selectedObject.position;
                        
                        if (selectedRigidbody != null)
                        {
                            selectedRigidbody.isKinematic = true; // 关闭运动学以允许物理移动
                            selectedRigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
                        }
                        isDragging = true;
                    }
                    else 
                    {
                        selectedObject = null;
                        return;
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                
                if (selectedRigidbody != null)
                {
                    selectedRigidbody.constraints = RigidbodyConstraints.None;
                    selectedRigidbody.isKinematic = false; // 恢复物理模拟
                    
                    if (clampedDrag.magnitude > minDragThreshold)
                    {
                        // 计算发射方向（应用角度参数）
                        Vector3 launchDirection = clampedDrag.normalized * -1;
                        // 将水平方向力与垂直方向力结合
                        // 根据拖动距离比例计算实际力度
                        float forceRatio = Mathf.Clamp01(clampedDrag.magnitude / dragRadius);
                        float currentLaunchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, forceRatio);
                        
                        float horizontalForce = Mathf.Cos(launchAngle * Mathf.Deg2Rad) * currentLaunchForce;
                        float verticalForce = Mathf.Sin(launchAngle * Mathf.Deg2Rad) * currentLaunchForce;
                        selectedRigidbody.AddForce(new Vector3(launchDirection.x * horizontalForce, verticalForce, launchDirection.z * horizontalForce), ForceMode.Impulse);
                        isAtResetPosition = false; // 发射后立即更新状态
                    }
                    else
                    {
                        selectedObject.position = resetPosition;
                        isAtResetPosition = true; // 更新位置状态
                    }
                    
                    selectedRigidbody = null;
                }
                selectedObject = null;
                CameraController.Instance.SwitchToHitView();
            }

            if (isDragging && selectedObject != null)
            {
                Vector3 targetPos = GetMouseWorldPos() + offset;
                targetPos.y = selectedObject.position.y;
                
                // 限制拖拽范围
                // 限制在XZ平面半圆形范围
                Vector3 dragVector = targetPos - initialBallPosition;
                dragVector.y = 0; // 保持Y轴不变
                
                // 计算角度并限制范围
                float currentAngle = Mathf.Atan2(dragVector.z, dragVector.x) * Mathf.Rad2Deg;
                currentAngle = (currentAngle + 360) % 360; // 转换为0-360度
                Debug.Log($"Current Angle: {currentAngle}");
                // 角度限制
                if (currentAngle < minAngle || currentAngle > maxAngle) {
                    float minDiff = Mathf.Abs(currentAngle - minAngle);
                    float maxDiff = Mathf.Abs(currentAngle - maxAngle);
                    currentAngle = (minDiff < maxDiff) ? minAngle : maxAngle;
                }
                
                // 根据限制后的角度重新计算方向
                Vector3 limitedDir = new Vector3(
                    Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(currentAngle * Mathf.Deg2Rad)
                );
                
                // 应用距离限制
                float dragDistance = Mathf.Clamp(dragVector.magnitude, 0, dragRadius);
                clampedDrag = limitedDir * dragDistance;
                
                newPos = initialBallPosition + clampedDrag;
                newPos.y = selectedObject.position.y; // 保持原有Y轴位置
                
                if (selectedRigidbody != null)
                {
                    selectedRigidbody.MovePosition(newPos);
                }
                else
                {
                    selectedObject.position = newPos;
                }
            }
        }

        private Plane xzPlane = new Plane(Vector3.up, Vector3.zero);
        
        private Vector3 GetMouseWorldPos()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float distance;
            if (xzPlane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            return Vector3.zero;
        }

        // 绘制可拖拽区域
        void DrawDragArea()
        {
            if(dragAreaLine == null)
            {
                return;
            }
            int segments = 50;
            dragAreaLine.startWidth = 0.05f;
            dragAreaLine.endWidth = 0.05f;
            dragAreaLine.material = new Material(Shader.Find("Sprites/Default"));
            dragAreaLine.positionCount = segments + 1;
            dragAreaLine.startColor = Color.cyan;
            dragAreaLine.endColor = Color.blue;

            Vector3[] points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(minAngle, maxAngle, i / (float)segments) * Mathf.Deg2Rad;
                points[i] = resetPosition + new Vector3(
                    Mathf.Cos(angle) * dragRadius,
                    0,
                    Mathf.Sin(angle) * dragRadius
                );
            }
            dragAreaLine.SetPositions(points);
        }

        // 更新发射轨迹预测
        void UpdateTrajectory()
        {
            Vector3 launchDirection = clampedDrag.normalized * -1;
            float forceRatio = Mathf.Clamp01(clampedDrag.magnitude / dragRadius);
            float currentLaunchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, forceCurve.Evaluate(forceRatio));
            
            // 与实际发射保持完全一致的力计算
            float horizontalForce = Mathf.Cos(launchAngle * Mathf.Deg2Rad) * currentLaunchForce;
            float verticalForce = Mathf.Sin(launchAngle * Mathf.Deg2Rad) * currentLaunchForce;
            
            Vector3 velocity = new Vector3(
                launchDirection.x * horizontalForce,
                verticalForce,
                launchDirection.z * horizontalForce
            );

            trajectoryLine.positionCount = 50;
            trajectoryLine.colorGradient = CreateTrajectoryGradient();
            
            Vector3 currentPos = selectedObject.position;
            Vector3 currentVelocity = velocity / selectedRigidbody.mass;
            float simulationStep = 0.02f; // 物理模拟时间步长
            int simulationSteps = 100; // 轨迹预测迭代次数
            
            trajectoryLine.positionCount = simulationSteps;
            for (int i = 0; i < simulationSteps; i++)
            {
                trajectoryLine.SetPosition(i, currentPos);
                currentVelocity += Physics.gravity * simulationStep;
                currentPos += currentVelocity * simulationStep;
            }
        }

        // 新增重置位置计算方法
        private void CalculateResetPosition()
        {
            if (targetObject != null)
            {
                var targetCollider = targetObject.GetComponent<BoxCollider>();
                if (targetCollider != null)
                {
                    // 计算目标物体顶部中心点位置
                    resetPosition = targetObject.position + 
                        Vector3.up * (targetCollider.size.y * 0.5f * targetObject.lossyScale.y);
                }
                else
                {
                    resetPosition = targetObject.position;
                }
            }
            else
            {
                resetPosition = ball.transform.position;
            }
        }

        // 显示碰撞信息到UI
        public void ShowCollisionInfo(string info)
        {
            // 在实际项目中需要连接具体的UI组件，这里添加调试日志
            Debug.Log($"[碰撞信息] {info}");
            
            // 示例：如果使用UI Text组件，可以取消注释以下代码
            // if (infoText != null) 
            // {
            //     infoText.text = info;
            // }
        }

        // 创建轨迹线颜色渐变
        public Gradient CreateTrajectoryGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.yellow, 0f),
                    new GradientColorKey(Color.red, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            return gradient;
        }
    }

}