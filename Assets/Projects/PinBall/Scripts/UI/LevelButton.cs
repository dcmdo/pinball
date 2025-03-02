using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cool.Dcm.Game.PinBall.UI;
using Cool.Dcm.Game.PinBall.LevelSystem;

namespace Cool.Dcm.Game.PinBall.UI
{
    public class LevelButton : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Image previewImage;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI starsRequireText;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image[] starIcons;
        [SerializeField] private Button button;

        [Header("外观设置")] 
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Sprite emptyStar;
        [SerializeField] private Sprite filledStar;

        private LevelConfig config;

        public void Initialize(LevelConfig config, bool isUnlocked, int starsAchieved, UnityEngine.Events.UnityAction onClick)
        {
            this.config = config;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            UpdateLockState(isUnlocked);
            UpdateStars(starsAchieved);
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            // 加载预览图
            previewImage.sprite = LevelManager.Instance.LoadPreviewImage(config.previewImagePath);
            
            levelNameText.text = config.displayName;
            starsRequireText.text = $"{config.unlockCondition.requireStars}";
            
            // 判断解锁状态
            bool isUnlocked = LevelManager.Instance.GetUnlockedLevels().Contains(config);
            UpdateLockState(isUnlocked);
            UpdateStars(GameManager.Instance.GetLevelStars(config.levelNumber));
        }

        public void OnClick()
        {
            LevelManager.Instance.StartLevel(config);
        }

        void UpdateLockState(bool isUnlocked)
        {
            button.interactable = isUnlocked;
            lockIcon.gameObject.SetActive(!isUnlocked);
            levelNameText.gameObject.SetActive(isUnlocked);
            
            // 设置颜色
            var colors = button.colors;
            colors.normalColor = isUnlocked ? unlockedColor : lockedColor;
            button.colors = colors;
        }

        void UpdateStars(int achievedCount)
        {
            for (int i = 0; i < starIcons.Length; i++)
            {
                starIcons[i].sprite = i < achievedCount ? filledStar : emptyStar;
            }
        }
    }
}
