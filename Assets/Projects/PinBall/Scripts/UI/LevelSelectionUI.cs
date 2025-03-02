using UnityEngine;
using System.Collections.Generic;
using Cool.Dcm.Game.PinBall.LevelSystem;
using Cool.Dcm.Game.PinBall.UI;

namespace Cool.Dcm.Game.PinBall.UI
{
    public class LevelSelectionUI : MonoBehaviour
    {
        [Header("预制体")]
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private Transform buttonsParent;

        private void Start()
        {
            LevelManager.Instance.OnLevelsUpdated += UpdateLevelDisplay;
            UpdateLevelDisplay();
        }

        private void OnDestroy()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.OnLevelsUpdated -= UpdateLevelDisplay;
        }

        void UpdateLevelDisplay()
        {
            // 清除现有按钮
            foreach (Transform child in buttonsParent)
                Destroy(child.gameObject);

            // 获取已解锁关卡
            var unlockedLevels = LevelManager.Instance.GetUnlockedLevels();
            
            // 生成新按钮
            foreach (var level in unlockedLevels)
            {
                var buttonObj = Instantiate(levelButtonPrefab, buttonsParent);
                var levelButton = buttonObj.GetComponent<LevelButton>();
                if (levelButton != null)
                {
                    bool isUnlocked = LevelManager.Instance.GetUnlockedLevels().Contains(level);
                    int stars = GameManager.Instance.GetLevelStars(level.levelNumber);
                    levelButton.Initialize(
                        config: level,
                        isUnlocked: isUnlocked,
                        starsAchieved: stars,
                        onClick: () => levelButton.OnClick()
                    );
                }
            }
        }
    }
}
