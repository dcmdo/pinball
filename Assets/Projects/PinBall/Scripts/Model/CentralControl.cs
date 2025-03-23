using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cool.Dcm.Game.PinBall
{
    public class CentralControl : MonoBehaviour
    {
        public Transform targetObject; // 新增目标物体引用
        public PaddleController leftPaddle;
        public PaddleController rightPaddle;
        public GameObject ballPrefab;
        private List<BallController> ballList =new List<BallController>();
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

        private BallController currentDragBall;//在发射位置 拖动的小球

        void Start()
        {
            
            playerInput = GetComponent<PlayerInput>();
            playerInput.onActionTriggered +=HandleInput;
            // 初始化重置位置（动态计算目标物体顶部）
            CalculateResetPosition();
            GenerateBall();
            
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


        #region 处理用户输入
        private void HandleInput(InputAction.CallbackContext context){
            switch (context.action.name)
            {
                case "LeftPaddle":
                    if (context.phase == InputActionPhase.Performed&&!isAtResetPosition)
                    {
                        leftPaddle.RotatePaddle(context.ReadValueAsButton());
                    }
                    break;
                case "RightPaddle":
                    if (context.phase == InputActionPhase.Performed&&!isAtResetPosition)
                    {
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
                case "DragPress":
                    if (context.phase == InputActionPhase.Started)
                    {
                        StartDrag(context);
                    }
                    if (context.phase == InputActionPhase.Canceled)
                    {
                        EndDrag(context);
                    }
                    break;
                case "DragPosition":
                    if(context.phase == InputActionPhase.Performed)
                    {
                        InDrag(context);
                    }                    
                    break;
            }
            
        }
        

        #endregion





        #region  暂停功能
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
        #endregion
        
        








        #region 用户拖拽操作相关功能
        private Plane xzPlane = new Plane(Vector3.up, Vector3.zero);

        private Vector2 startDragPosition;

        private void StartDrag(InputAction.CallbackContext context)
        {
            // 获取点击时的屏幕位置
            Ray ray = Camera.main.ScreenPointToRay(startDragPosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                currentDragBall = hit.transform.GetComponent<BallController>();
                // 只有当小球在重置位置且静止时才能拖动（添加位置验证）
                if(currentDragBall != null && isAtResetPosition&& !isDragging) // 添加拖动状态检查
                {
                    Debug.Log("Drag Ball");
                    offset = currentDragBall.transform.position - GetMouseWorldPos(startDragPosition);
                    initialBallPosition = currentDragBall.transform.position;
                    currentDragBall.setDragRigidbody(true);
                    isDragging = true;
                }
                else 
                {
                    
                    return;
                }
            }
        }

        private void InDrag(InputAction.CallbackContext context){
            if (isDragging && currentDragBall != null)
            {
                // 获取当前鼠标位置并转换为世界坐标
                Vector3 targetPos = GetMouseWorldPos(context.ReadValue<Vector2>()) + offset;
                targetPos.y = currentDragBall.transform.position.y;
                
                // 限制拖拽范围
                // 限制在XZ平面半圆形范围
                Vector3 dragVector = targetPos - initialBallPosition;
                dragVector.y = 0; // 保持Y轴不变
                
                // 计算角度并限制范围
                float currentAngle = Mathf.Atan2(dragVector.z, dragVector.x) * Mathf.Rad2Deg;
                currentAngle = (currentAngle + 360) % 360; // 转换为0-360度
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
                newPos.y = currentDragBall.transform.position.y; // 保持原有Y轴位置
                
                currentDragBall.MovePosition(newPos);
                UpdateTrajectory();
            }else{
                startDragPosition = context.ReadValue<Vector2>();
            }
           
        }

        private void EndDrag(InputAction.CallbackContext context)
        {
            isDragging = false;
            if (currentDragBall != null)
            {
                currentDragBall.setDragRigidbody(false);
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
                    currentDragBall.Launch(new Vector3(launchDirection.x * horizontalForce, verticalForce, launchDirection.z * horizontalForce),currentLaunchForce);
                    isAtResetPosition = false; // 发射后立即更新状态
                    CameraController.Instance.SwitchToHitView();
                    trajectoryLine.positionCount = 0;
                }
                else
                {
                    setBallPos(currentDragBall);
                    isAtResetPosition = true; // 更新位置状态
                }
                
                currentDragBall = null;
                
            }
            currentDragBall = null;
                
        }

        private Vector3 GetMouseWorldPos(Vector2 screenPosition)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
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
            
            Vector3 currentPos = currentDragBall.transform.position;
            Vector3 currentVelocity = velocity;
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
        
        #endregion
        





        #region 小球生成及位置重置

        public void GenerateBall()
        {
            if (ballPrefab != null)
            {
                GameObject ball = Instantiate(ballPrefab, targetObject.position, Quaternion.identity);
                BallController ballController = ball.GetComponent<BallController>();
                ballList.Add(ballController);
                setBallPos(ballController);
                isAtResetPosition= true;
            }
        }

        // 平滑释放协程
        void RestBallPos()
        {
            if(isAtResetPosition){
                Debug.Log("小球已处于重置位置");
                return;
            }
            Debug.Log(targetObject);
            // 添加重置功能
            if (ballList.Count>0 && targetObject != null)
            {
                CameraController.Instance.SwitchToLaunchView();
                Debug.Log("Reset Ball Position");
                var mainBall  = GetMainBall();
                setBallPos(mainBall);
                isAtResetPosition= true;
                // 添加额外验证
                Debug.Log($"物理状态已重置 速度:{mainBall.GetComponent<Rigidbody>().velocity} 角速度:{mainBall.GetComponent<Rigidbody>().angularVelocity}");
                Debug.Log($"Ball reset to position. Ready to drag: {isAtResetPosition}");
                Debug.Log($"实际位置差异: {Vector3.Distance(mainBall.transform.position, resetPosition)}");
            }
        }

        bool setBallPos(BallController ball){
            if(ball == null)
            {
                Debug.LogWarning("小球为空，无法设置位置");
                return false;
            }
            // 应用底部对齐计算
            var ballCollider =  ball.GetComponent<SphereCollider>();
            Vector3 finalPosition;
            if (ballCollider != null)
            {
                float ballRadius = ballCollider.radius * ball.transform.lossyScale.y;
                finalPosition = resetPosition + Vector3.up * ballRadius;
                
                // 调试日志
                // Debug.Log($"目标物体顶部位置: {resetPosition}");
                // Debug.Log($"小球半径: {ballRadius} (缩放系数: {ball.transform.lossyScale.y})");
                // Debug.Log($"最终重置位置: {finalPosition}");
            }
            else
            {
                finalPosition = resetPosition;
                Debug.LogWarning("小球缺少SphereCollider组件，使用默认重置位置");
            }
            
            ball.ResetBall(finalPosition);
            return true;
        }

        BallController GetMainBall(){
            return ballList[0];
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
                    resetPosition = targetObject.position + Vector3.up * (targetCollider.size.y * 0.5f * targetObject.lossyScale.y);
                }
                else
                {
                    resetPosition = targetObject.position;
                }
            }
        }

        #endregion

    }

}