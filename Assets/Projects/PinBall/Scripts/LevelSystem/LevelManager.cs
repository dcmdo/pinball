using UnityEngine;
using Cool.Dcm.Game.PinBall.Enemy;
// using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cool.Dcm.Game.PinBall.LevelSystem
{
    public class LevelManager : MonoBehaviour
    {
        // 添加静态实例用于全局访问
        public static LevelManager Instance { get; private set; }
        
        private LevelConfig currentLevel;
        private List<LevelConfig> allLevels = new List<LevelConfig>();
        
        // 添加初始化方法
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLevelSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeLevelSystem()
        {
            LoadAllLevelConfigs();
            Debug.Log($"Level system initialized with {allLevels.Count} levels");
        }
        
        public void LoadLevel(LevelConfig levelConfig)
        {
            currentLevel = levelConfig;
            
            // 生成关卡元素
            SpawnTerrain();
            SpawnPowerUps();
            SpawnEnemies();
            SetupBallProperties();
        }

        public void StartLevel(LevelConfig levelConfig)
        {
            LoadLevel(levelConfig);
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        // 新增关卡选择界面相关方法
        public void LoadAllLevelConfigs()
        {
            allLevels.Clear();
            
            // 加载JSON配置
            var jsonConfigs = Resources.LoadAll<TextAsset>("Levels/");
            foreach (var configFile in jsonConfigs)
            {
                try 
                {
                    LevelConfig config = ScriptableObject.CreateInstance<LevelConfig>();
                    JsonUtility.FromJsonOverwrite(configFile.text, config);
                    allLevels.Add(config);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载关卡配置失败: {configFile.name}\n{e}");
                }
            }

            // 加载ScriptableObject资源
            var assetConfigs = Resources.LoadAll<LevelConfig>("Levels/");
            foreach (var config in assetConfigs)
            {
                if (!allLevels.Contains(config))
                {
                    allLevels.Add(config);
                }
            }

            // 按关卡编号排序
            allLevels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
            
            Debug.Log($"成功加载 {allLevels.Count} 个关卡配置");
            NotifyLevelsUpdated();
        }

        // 获取所有关卡配置（用于UI界面生成）
        public List<LevelConfig> GetAllLevels()
        {
            return allLevels;
        }

        // 更新后的解锁关卡逻辑（从GameManager获取玩家数据）
        public List<LevelConfig> GetUnlockedLevels()
        {
            List<LevelConfig> unlocked = new List<LevelConfig>();
            int playerStars = GameManager.Instance.TotalStars;
            int highestLevel = GameManager.Instance.HighestLevel;
            
            foreach (var level in allLevels)
            {
                if (level.unlockCondition.requireStars <= playerStars && 
                    level.unlockCondition.requireLevelNumber <= highestLevel)
                {
                    unlocked.Add(level);
                }
            }
            return unlocked;
        }

        // 新增关卡选择事件
        public event System.Action OnLevelsUpdated;
        public void NotifyLevelsUpdated()
        {
            OnLevelsUpdated?.Invoke();
        }

        public LevelConfig GetLevelByNumber(int levelNumber)
        {
            return allLevels.Find(l => l.levelNumber == levelNumber);
        }

        public Sprite LoadPreviewImage(string path)
        {
            return Resources.Load<Sprite>(path);
        }

        void SpawnTerrain()
        {
            var terrainPrefab = Resources.Load<GameObject>(currentLevel.terrain.prefabPath);
            foreach (var point in currentLevel.terrain.spawnPoints)
            {
                Instantiate(terrainPrefab, point, Quaternion.identity);
            }
        }

        void SpawnPowerUps()
        {
            foreach (var powerUp in currentLevel.powerUps)
            {
                var prefab = Resources.Load<GameObject>($"PowerUps/{powerUp.type}");
                {
                    Instantiate(prefab, powerUp.position, Quaternion.identity);
                }
            }
        }

        void SpawnEnemies()
        {
            foreach (var enemy in currentLevel.enemies)
            {
                var prefab = Resources.Load<GameObject>($"Enemies/{enemy.type}");
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    var patrol = instance.GetComponent<MonsterPatrol>();
                    if (patrol != null)
                    {
                    patrol.Initialize(new MonsterConfig {
                        patrolPoints = enemy.path,
                        moveSpeed = enemy.speed,
                        detectionRange = enemy.detectionRange,
                        patrolAngle = enemy.patrolAngle,
                        aiConfigPath = enemy.aiConfigPath,
                        modelScale = enemy.modelScale
                    },new GameObject().transform);
                    
                    // 加载敌人模型
                    var modelPrefab = Resources.Load<GameObject>(enemy.modelPrefab);
                    if (modelPrefab != null) {
                        Instantiate(modelPrefab, instance.transform);
                    }
                    }
                }
            }
        }

        void SetupBallProperties()
        {
            var ball = GameObject.FindGameObjectWithTag("Player");
            if (ball != null)
            {
                var rb = ball.GetComponent<Rigidbody>();
                rb.mass = currentLevel.ballProperties.mass;
                var collider = ball.GetComponent<Collider>();
                collider.material = new PhysicMaterial()
                {
                    bounciness = currentLevel.ballProperties.bounciness,
                    dynamicFriction = 0.6f,
                    staticFriction = 0.6f
                };
            }
        }
    }
}
