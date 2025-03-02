using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float maxAngle = 60f;
    [SerializeField] private float minAngle = 120f;
    [SerializeField] private float rotateSpeed = 5f;
    
    [Header("Trajectory Settings")] 
    [SerializeField] private LineRenderer trajectoryLine;
    [SerializeField] private int predictionPoints = 20;
    [SerializeField] private float predictionStep = 0.1f;
    [SerializeField] private LayerMask collisionMask;

    [SerializeField] private GameObject paddle;
    private Keyboard keybord;
    private BallController currentBall;

    private Vector3 currentDirection;

    private void Start()
    {
        keybord = Keyboard.current;
        if(paddle == null)
        {
            paddle = gameObject;
        }
        
        if (trajectoryLine == null)
        {
            trajectoryLine = gameObject.AddComponent<LineRenderer>();
        }
        trajectoryLine.enabled = false;
            trajectoryLine.startWidth = 0.1f;
            trajectoryLine.endWidth = 0.05f;
            
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


    public void RotatePaddle(float input, float speedMultiplier = 1f)
    {
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, input);
        float currentAngle = paddle.transform.rotation.eulerAngles.y;
        float smoothAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotateSpeed * speedMultiplier);
        paddle.transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

        UpdateTrajectory();
    }

    void UpdateTrajectory()
    {
        if (currentBall == null) return;
        
        Vector3 launchDirection = currentDirection;
        var points = PredictTrajectory(currentBall.transform.position, launchDirection);
        
        // 动态调整轨迹线宽和透明度
        float speedFactor = Mathf.Clamp01(launchDirection.magnitude / 20f);
        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0, 0.2f * (1 + speedFactor)),
            new Keyframe(1, 0.05f * (1 + speedFactor))
        );
        trajectoryLine.widthCurve = widthCurve;
        
        // 添加速度响应式预测点
        trajectoryLine.positionCount = Mathf.RoundToInt(predictionPoints * (1 + speedFactor));
        trajectoryLine.SetPositions(points.ToArray());
        
        // 添加轨迹末端特效
        if (points.Count > 1)
        {
            Vector3 endPos = points[points.Count - 1];
            Debug.DrawLine(endPos, endPos + Vector3.up * 0.5f, Color.magenta, 0.1f);
        }
    }

    // Vector3 CalculateReflectionDirection()
    // {
    //     Vector3 paddleNormal = paddle.transform.forward;
    //     Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        
    //     // 当球静止时使用挡板方向作为默认发射方向
    //     if (ballRb.velocity.magnitude < 0.1f)
    //     {
    //         return paddleNormal * 15f; // 给一个合理的初始速度
    //     }
        
    //     return Vector3.Reflect(ballRb.velocity.normalized, paddleNormal);
    // }

    private List<Vector3> PredictTrajectory(Vector3 startPos, Vector3 initialVelocity)
    {
        List<Vector3> points = new List<Vector3>();
        Vector3 currentPos = startPos;
        Vector3 currentVel = initialVelocity;
        float maxCastDistance = 15f;
        float speedFactor = Mathf.Clamp01(currentVel.magnitude / 20f);
        
        // 动态调整预测点数量基于速度
        int dynamicPoints = Mathf.RoundToInt(predictionPoints * (1 + speedFactor));
        trajectoryLine.positionCount = dynamicPoints;

        // 添加速度响应式线宽
        AnimationCurve speedWidthCurve = new AnimationCurve(
            new Keyframe(0, 0.2f * (1 + speedFactor)),
            new Keyframe(1, 0.05f * (1 + speedFactor))
        );
        trajectoryLine.widthCurve = speedWidthCurve;

        // 添加高度响应式透明度
        trajectoryLine.material.SetFloat("_Alpha", 1f);

        for (int i = 0; i < dynamicPoints; i++)
        {
            points.Add(currentPos);
            
            currentVel += Physics.gravity * predictionStep;
            Vector3 nextPos = currentPos + currentVel * predictionStep;
            
            Vector3 moveDirection = (nextPos - currentPos).normalized;
            float moveDistance = Vector3.Distance(nextPos, currentPos);
            
            // 使用更精确的连续碰撞检测
            if (Physics.SphereCast(currentPos, 0.05f, moveDirection, out RaycastHit hit, 
                Mathf.Min(moveDistance, maxCastDistance), collisionMask, QueryTriggerInteraction.Ignore))
            {
                points.Add(hit.point);
                
                // 生成动态碰撞标记粒子
                float collisionStrength;
                GameObject marker = CreateCollisionMarker(hit.point, currentVel.magnitude, out collisionStrength);
                
                // 显示增强型调试信息
                Debug.DrawRay(hit.point, hit.normal * (2f + collisionStrength * 3f), 
                    Color.Lerp(Color.blue, Color.red, collisionStrength), 1f);
                
                // 使用精确反射公式计算新方向
                Vector3 reflectDir = Vector3.Reflect(currentVel.normalized, hit.normal);
                float energyPreservation = Mathf.Clamp01(1 - hit.collider.material.dynamicFriction);
                currentVel = reflectDir * Mathf.Clamp(currentVel.magnitude * (0.85f + energyPreservation * 0.15f), 5f, 30f);
                currentPos = hit.point + reflectDir * 0.1f;

                // 显示高级碰撞信息
                CentralControl.Instance.ShowCollisionInfo(
                    $"{hit.collider.name}\n" +
                    $"速度: {currentVel.magnitude:F1}m/s\n" +
                    $"角度: {Vector3.Angle(hit.normal, reflectDir):F0}°");
            }
            else
            {
                currentPos = nextPos;
            }
            
            // 根据高度调整透明度
            float heightFactor = Mathf.Clamp01(currentPos.y / 10f);
            trajectoryLine.material.SetFloat("_Alpha", 1f - heightFactor * 0.5f);
        }
        return points;
    }

    private GameObject CreateCollisionMarker(Vector3 position, float speed, out float collisionStrength)
    {
        GameObject marker = new GameObject("CollisionMarker");
        marker.transform.position = position;
        
        ParticleSystem ps = marker.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startSpeed = 2f;
        main.startLifetime = 0.5f;
        main.startSize = 0.3f;
        main.startColor = Color.Lerp(Color.cyan, Color.white, Mathf.Clamp01(speed / 15f));
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        collisionStrength = Mathf.Clamp01(speed / 15f);
        marker.transform.localScale = Vector3.one * (0.2f + collisionStrength * 0.3f);
        Destroy(marker, 1f);

        return marker;
    }

    public void SetBall(BallController ball,Vector3 dir)
    {
        currentBall = ball;
        trajectoryLine.enabled = true;
        currentDirection = dir;
        UpdateTrajectory();
    }

    // public void ReleaseBall()
    // {
    //     if (currentBall != null)
    //     {
    //         currentBall.Launch(CalculateReflectionDirection());
    //         currentBall = null;
    //         if(trajectoryLine != null)
    //         {
    //             trajectoryLine.enabled = false;
    //         }
    //     }
    // }
}
