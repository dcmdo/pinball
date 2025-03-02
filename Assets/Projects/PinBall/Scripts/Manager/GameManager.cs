﻿using UnityEngine;
using UnityEngine.SceneManagement;
using Cool.Dcm.Game.PinBall.LevelSystem;
using Cool.Dcm.Game.PinBall.UI;
using System.Collections.Generic;

namespace Cool.Dcm.Game.PinBall
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("玩家进度")]
        public int TotalStars = 0;
        public int HighestLevel = 1;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadPlayerData();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void LoadPlayerData()
        {
            // TODO: 实现实际的数据加载逻辑
            TotalStars = PlayerPrefs.GetInt("TotalStars", 0);
            HighestLevel = PlayerPrefs.GetInt("HighestLevel", 1);
        }
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private GameObject levelButtonPrefab; // 关卡按钮预制体
        [SerializeField] private Transform levelButtonContainer; // 关卡按钮父节点
        
        public int CurrentLevel { get; private set; } = 1;
        public int playerStars = 0;          // 玩家当前星星总数
        public int highestUnlockedLevel = 1; // 最高解锁关卡
        
        private const string LevelSelectScene = "LevelSelect";
        private const string GameScene = "Game";
        
        private LevelConfig currentLevelConfig;
        private List<LevelConfig> levelCache; // 缓存加载的关卡配置

        void Start()
        {
            if (SceneManager.GetActiveScene().name == GameScene)
            {
                LoadLevelConfig(CurrentLevel);
                levelManager.LoadLevel(currentLevelConfig);
            }
            else if (SceneManager.GetActiveScene().name == LevelSelectScene)
            {
                InitializeLevelSelect();
            }
        }

        private void InitializeLevelSelect()
        {
            // 清空现有按钮
            foreach (Transform child in levelButtonContainer)
            {
                Destroy(child.gameObject);
            }

            // 加载所有关卡配置
            LevelConfig[] allLevels = Resources.LoadAll<LevelConfig>("Levels");
            highestUnlockedLevel = PlayerPrefs.GetInt("HighestUnlockedLevel", 1);
            playerStars = PlayerPrefs.GetInt("TotalStars", 0);

            // 创建关卡按钮
            for (int i = 0; i < allLevels.Length; i++)
            {
                GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
                LevelButton levelButton = buttonObj.GetComponent<LevelButton>();
                
                int levelNumber = i + 1;
                bool isUnlocked = levelNumber <= highestUnlockedLevel;
                int starsAchieved = PlayerPrefs.GetInt($"Level{levelNumber}_Stars", 0);
                
                var levelConfig = allLevels[i];
                levelButton.Initialize(
                    config: levelConfig,
                    isUnlocked: isUnlocked,
                    starsAchieved: starsAchieved,
                    onClick: () => LoadLevel(levelNumber)
                );
            }
        }

        public void LoadLevel(int levelNumber)
        {
            if (levelNumber > highestUnlockedLevel) return;
            
            CurrentLevel = levelNumber;
            SceneManager.LoadScene(GameScene);
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(LevelSelectScene);
        }

        public int GetLevelStars(int levelNumber)
        {
            return PlayerPrefs.GetInt($"Level{levelNumber}_Stars", 0);
        }

        public void UpdateLevelProgress(int starsEarned)
        {
            // 更新最高解锁关卡
            if (CurrentLevel >= highestUnlockedLevel)
            {
                highestUnlockedLevel = CurrentLevel + 1;
                PlayerPrefs.SetInt("HighestUnlockedLevel", highestUnlockedLevel);
            }

            // 更新星星总数
            playerStars += starsEarned;
            PlayerPrefs.SetInt("TotalStars", playerStars);

            // 保存当前关卡星星数（如果比之前记录的高）
            int currentBest = PlayerPrefs.GetInt($"Level{CurrentLevel}_Stars", 0);
            if (starsEarned > currentBest)
            {
                PlayerPrefs.SetInt($"Level{CurrentLevel}_Stars", starsEarned);
            }

            PlayerPrefs.Save();
        }

        void Update()
        {
            HandleGameInput();
        }

        private void HandleGameInput()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ReloadCurrentLevel();
            }
        }

        public void ReloadCurrentLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void LoadLevelConfig(int levelNumber)
        {
            currentLevelConfig = Resources.Load<LevelConfig>($"Levels/Level{levelNumber}");
            if (currentLevelConfig == null)
            {
                Debug.LogError($"Level {levelNumber} config not found!");
            }
        }

        public void NextLevel()
        {
            CurrentLevel++;
            LoadLevelConfig(CurrentLevel);
            levelManager.LoadLevel(currentLevelConfig);
        }
    }
}
