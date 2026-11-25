using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MobaGame.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        public GameObject mainPanel;
        public GameObject aboutPanel;

        [Header("About Sub-Tabs")]
        public GameObject storyTab;
        public GameObject howToPlayTab;
        public GameObject assetSourceTab;

        [Header("Buttons")]
        public Button playButton;
        public Button aboutButton;
        public Button quitButton;
        public Button closeAboutButton;

        public Button tabStoryButton;
        public Button tabHowToPlayButton;
        public Button tabAssetSourceButton;

        private void Start()
        {
            Time.timeScale = 1f;

            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (aboutButton != null) aboutButton.onClick.AddListener(OnAboutClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
            if (closeAboutButton != null) closeAboutButton.onClick.AddListener(OnCloseAboutClicked);

            if (tabStoryButton != null) tabStoryButton.onClick.AddListener(() => SwitchTab(0));
            if (tabHowToPlayButton != null) tabHowToPlayButton.onClick.AddListener(() => SwitchTab(1));
            if (tabAssetSourceButton != null) tabAssetSourceButton.onClick.AddListener(() => SwitchTab(2));

            // Default state
            if (aboutPanel != null) aboutPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            SwitchTab(0);
        }

        public void OnPlayClicked()
        {
            SceneManager.LoadScene("BattleArena");
        }

        public void OnAboutClicked()
        {
            if (aboutPanel != null) aboutPanel.SetActive(true);
        }

        public void OnCloseAboutClicked()
        {
            if (aboutPanel != null) aboutPanel.SetActive(false);
        }

        public void SwitchTab(int tabIndex)
        {
            if (storyTab != null) storyTab.SetActive(tabIndex == 0);
            if (howToPlayTab != null) howToPlayTab.SetActive(tabIndex == 1);
            if (assetSourceTab != null) assetSourceTab.SetActive(tabIndex == 2);
        }

        public void OnQuitClicked()
        {
            Debug.Log("Keluar dari game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
