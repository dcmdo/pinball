using UnityEngine;
using System.Collections.Generic;

namespace Cool.Dcm.Game.PinBall.Enemy
{
    [CreateAssetMenu(fileName = "MonsterConfig", menuName = "Configs/Monster Config")]
    public class MonsterConfig : ScriptableObject
    {
        [Tooltip("巡逻路径点坐标列表")] 
        [Header("移动设置")]
        public List<Vector3> patrolPoints = new List<Vector3>();
        [Range(1f, 10f)] public float moveSpeed = 3f;
        [Range(1, 10)] public int pointCount = 3;
        [Range(1f, 20f)] public float detectionRange = 8f;
        [Range(0f, 360f)] public float patrolAngle = 90f;
        [Header("攻击设置")]
        public float attackRange = 2f;
        public float attackInterval = 1f;
        public string aiConfigPath = "AI/DefaultEnemyAI";
        [Range(0.1f, 2f)] public float modelScale = 1f;
    }
}
