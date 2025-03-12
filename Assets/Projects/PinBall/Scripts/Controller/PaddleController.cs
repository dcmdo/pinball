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
        [SerializeField] private LineRenderer trajectoryLine;
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
            // 使用协程控制显示时间
            StartCoroutine(DisableLineAfterDelay(lineDuration));
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

            Vector3 hitPoint = deepestContact.point;
            trajectoryLine.enabled = true;
            trajectoryLine.positionCount = 2;
            trajectoryLine.SetPosition(0, hitPoint);
            trajectoryLine.SetPosition(1, hitPoint + localHitDirection * 20f);
            
        }

        private System.Collections.IEnumerator DisableLineAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }
        }

    }

}
