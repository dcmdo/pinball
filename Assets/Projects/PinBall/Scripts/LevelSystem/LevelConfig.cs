using UnityEngine;
using System.Collections.Generic;

namespace Cool.Dcm.Game.PinBall.LevelSystem
{
    [System.Serializable]
    public class LevelConfig : ScriptableObject
    {
        public TerrainConfig terrain;
        public List<PowerUpConfig> powerUps;
        public List<EnemyConfig> enemies;
        public BallProperties ballProperties;
        
        // 新增关卡元数据字段
        #region 关卡元数据
        public int levelNumber;
        public string displayName;
        public string previewImagePath;
        public string description;
        public int difficulty;
        public UnlockCondition unlockCondition;
        #endregion

        [System.Serializable]
        public class UnlockCondition
        {
            public int requireStars;
            public int requireLevelNumber;
        }
    }

    [System.Serializable]
    public class TerrainConfig: ScriptableObject
    {
        public string prefabPath;
        public string materialPath;
        public List<Vector3> spawnPoints;
        public List<Vector3> obstaclePositions;
        [Range(0f, 2f)] public float friction = 1f;
    }

    [System.Serializable]
    public class PowerUpConfig : ScriptableObject
    {
        public string type;
        public Vector3 position;
        [Range(0f, 1f)] public float spawnChance = 0.5f;
        [Range(1f, 30f)] public float duration = 10f;
        [Range(0.1f, 5f)] public float effectValue = 1f;
        [Range(0f, 10f)] public float effectRadius = 3f;
        [Range(1f, 60f)] public float cooldown = 15f;
        public string vfxPath = "Effects/PowerUps/Default";
    }

    [System.Serializable]
    public class EnemyConfig : ScriptableObject
    {
        public string type;
        public List<Vector3> path;
        [Range(1f, 10f)] public float speed = 3f;
        [Range(1, 500)] public int hp = 100;
        [Range(1, 100)] public int attackPower = 10;
        [Range(0.1f, 5f)] public float attackInterval = 1f;
        [Range(1f, 20f)] public float detectionRange = 8f;
        [Range(0f, 360f)] public float patrolAngle = 90f;
        public string behaviorType = "Patrol";
        public string aiConfigPath = "AI/DefaultEnemyAI";
        public string modelPrefab = "Enemies/BaseEnemy";
        [Range(0.1f, 2f)] public float modelScale = 1f;
    }

    [System.Serializable]
    public class BallProperties: ScriptableObject
    {
        public float mass = 1f;
        [Range(0f, 1f)] public float bounciness = 0.8f;
        [Range(0f, 10f)] public float initialSpeed = 5f;
        [Range(1f, 20f)] public float maxSpeed = 15f;
        public Vector3 initialDirection = Vector3.forward;
    }
}
