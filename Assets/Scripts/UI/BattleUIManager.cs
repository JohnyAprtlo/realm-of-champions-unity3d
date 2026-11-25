using UnityEngine;
using UnityEngine.UI;
using MobaGame.Player;
using MobaGame.Enemy;
using MobaGame.Combat;
using MobaGame.Core;

namespace MobaGame.UI
{
    public class BattleUIManager : MonoBehaviour
    {
        [Header("Player HUD")]
        public Slider playerHpSlider;
        public Text playerHpText;
        public Slider playerManaSlider;
        public Text playerManaText;

        [Header("Enemy Boss HUD")]
        public Slider enemyHpSlider;
        public Text enemyHpText;

        [Header("Match Stats & Objective")]
        public Text timerText;
        public Text scoreText;
        public Text objectiveText;

        [Header("Storyline Banner")]
        public GameObject storyBannerPanel;
        public Text storyTitleText;
        public Text storyBodyText;
        private float storyBannerTimer = 0f;

        [Header("Respawn Overlay")]
        public GameObject respawnPanel;
        public Text respawnCountdownText;

        [Header("Skill Action Buttons (Mobile Legends Style)")]
        public Button attackButton;
        public Button skill1Button;
        public Image skill1CooldownOverlay;
        public Text skill1CooldownText;

        public Button skill2Button;
        public Image skill2CooldownOverlay;
        public Text skill2CooldownText;

        public Button ultButton;
        public Image ultCooldownOverlay;
        public Text ultCooldownText;

        [Header("Outcome Panels")]
        public GameObject victoryPanel;
        public Text victoryDescText;
        public GameObject defeatPanel;
        public GameObject pausePanel;

        [Header("Panel Buttons")]
        public Button victoryRestartBtn;
        public Button victoryMenuBtn;
        public Button defeatRestartBtn;
        public Button defeatMenuBtn;
        public Button pauseResumeBtn;
        public Button pauseRestartBtn;
        public Button pauseMenuBtn;
        public Button topPauseBtn;

        [Header("References")]
        public PlayerController player;
        public EnemyAI enemy;

        private void Start()
        {
            if (player == null)
            {
                GameObject pObj = GameObject.Find("Player");
                if (pObj == null) pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) player = pObj.GetComponent<PlayerController>();
            }

            if (enemy == null)
            {
                GameObject eObj = GameObject.Find("Enemy_Malakar");
                if (eObj == null) eObj = GameObject.FindGameObjectWithTag("Enemy");
                if (eObj != null) enemy = eObj.GetComponent<EnemyAI>();
            }

            // Wire up Buttons
            if (attackButton != null) attackButton.onClick.AddListener(() => { if (player != null) player.PerformBasicAttack(); });
            if (skill1Button != null) skill1Button.onClick.AddListener(() => { if (player != null) player.CastSkill1(); });
            if (skill2Button != null) skill2Button.onClick.AddListener(() => { if (player != null) player.CastSkill2(); });
            if (ultButton != null) ultButton.onClick.AddListener(() => { if (player != null) player.CastUltimate(); });

            if (topPauseBtn != null) topPauseBtn.onClick.AddListener(() => { GameManager.Instance.TogglePause(); });

            // Outcome buttons
            if (victoryRestartBtn != null) victoryRestartBtn.onClick.AddListener(() => GameManager.Instance.RestartBattle());
            if (victoryMenuBtn != null) victoryMenuBtn.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
            if (defeatRestartBtn != null) defeatRestartBtn.onClick.AddListener(() => GameManager.Instance.RestartBattle());
            if (defeatMenuBtn != null) defeatMenuBtn.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());

            if (pauseResumeBtn != null) pauseResumeBtn.onClick.AddListener(() => GameManager.Instance.TogglePause());
            if (pauseRestartBtn != null) pauseRestartBtn.onClick.AddListener(() => GameManager.Instance.RestartBattle());
            if (pauseMenuBtn != null) pauseMenuBtn.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());

            // Listen to GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMatchStateChanged += HandleStateChange;
                GameManager.Instance.OnScoreChanged += HandleScoreChange;
                GameManager.Instance.OnTurretProgressChanged += HandleTurretProgressChanged;
                GameManager.Instance.OnStoryUpdate += HandleStoryUpdate;
                GameManager.Instance.OnPlayerRespawnTick += HandlePlayerRespawnTick;

                HandleTurretProgressChanged(GameManager.Instance.destroyedEnemyTurrets, GameManager.Instance.totalEnemyTurrets);
            }

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (respawnPanel != null) respawnPanel.SetActive(false);
        }

        private void Update()
        {
            UpdatePlayerHUD();
            UpdateEnemyHUD();
            UpdateSkillCooldowns();
            UpdateMatchTimer();

            if (storyBannerTimer > 0f)
            {
                storyBannerTimer -= Time.unscaledDeltaTime;
                if (storyBannerTimer <= 0f && storyBannerPanel != null)
                {
                    storyBannerPanel.SetActive(false);
                }
            }
        }

        private void UpdatePlayerHUD()
        {
            if (player == null || player.healthSystem == null) return;

            if (playerHpSlider != null)
            {
                playerHpSlider.maxValue = player.healthSystem.maxHealth;
                playerHpSlider.value = player.healthSystem.currentHealth;
            }

            if (playerHpText != null)
            {
                playerHpText.text = $"{Mathf.CeilToInt(player.healthSystem.currentHealth)} / {player.healthSystem.maxHealth}";
            }

            if (playerManaSlider != null)
            {
                playerManaSlider.maxValue = player.maxMana;
                playerManaSlider.value = player.currentMana;
            }

            if (playerManaText != null)
            {
                playerManaText.text = $"{Mathf.CeilToInt(player.currentMana)} / {player.maxMana}";
            }
        }

        private void UpdateEnemyHUD()
        {
            if (enemy == null || enemy.healthSystem == null) return;

            int lv = GameManager.Instance != null ? GameManager.Instance.enemyEvolutionLevel : enemy.evolutionLevel;
            float respawnLeft = GameManager.Instance != null ? GameManager.Instance.EnemyRespawnRemaining : 0f;

            if (enemyHpSlider != null)
            {
                enemyHpSlider.maxValue = enemy.healthSystem.maxHealth;
                enemyHpSlider.value = enemy.healthSystem.currentHealth;
            }

            if (enemyHpText != null)
            {
                if (respawnLeft > 0f)
                {
                    enemyHpText.text = $"MUTANT LV.{lv + 1} BANGKIT DALAM {Mathf.CeilToInt(respawnLeft)}d...";
                }
                else
                {
                    enemyHpText.text = $"MUTANT LV.{lv}  •  {Mathf.CeilToInt(enemy.healthSystem.currentHealth)} / {enemy.healthSystem.maxHealth}";
                }
            }
        }

        private void UpdateSkillCooldowns()
        {
            if (player == null) return;

            UpdateCooldownUI(player.skill1Timer, player.skill1Cooldown, skill1CooldownOverlay, skill1CooldownText);
            UpdateCooldownUI(player.skill2Timer, player.skill2Cooldown, skill2CooldownOverlay, skill2CooldownText);
            UpdateCooldownUI(player.ultTimer, player.ultCooldown, ultCooldownOverlay, ultCooldownText);
        }

        private void UpdateCooldownUI(float timer, float maxCd, Image overlay, Text text)
        {
            if (timer > 0)
            {
                if (overlay != null)
                {
                    overlay.enabled = true;
                    overlay.fillAmount = timer / maxCd;
                }
                if (text != null)
                {
                    text.enabled = true;
                    text.text = Mathf.CeilToInt(timer).ToString();
                }
            }
            else
            {
                if (overlay != null) overlay.enabled = false;
                if (text != null) text.enabled = false;
            }
        }

        private void UpdateMatchTimer()
        {
            if (GameManager.Instance == null || timerText == null) return;
            int mins = Mathf.FloorToInt(GameManager.Instance.matchTimer / 60f);
            int secs = Mathf.FloorToInt(GameManager.Instance.matchTimer % 60f);
            timerText.text = $"{mins:00}:{secs:00}";
        }

        private void HandleScoreChange(int playerKills, int enemyKills)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{playerKills}  VS  {enemyKills}";
            }
        }

        private void HandleTurretProgressChanged(int destroyed, int total)
        {
            if (objectiveText != null)
            {
                objectiveText.text = $"TURRET: {destroyed} / {total}";
            }
        }

        private void HandleStoryUpdate(string title, string body)
        {
            if (storyBannerPanel != null)
            {
                storyBannerPanel.SetActive(true);
                storyBannerTimer = 6f;
            }
            if (storyTitleText != null) storyTitleText.text = title;
            if (storyBodyText != null) storyBodyText.text = body;
        }

        private void HandlePlayerRespawnTick(float secondsLeft)
        {
            if (respawnPanel == null) return;
            bool show = secondsLeft > 0f && GameManager.Instance != null && GameManager.Instance.currentState == GameManager.MatchState.Playing;
            respawnPanel.SetActive(show);
            if (show && respawnCountdownText != null)
            {
                respawnCountdownText.text = $"RESPAWN DI FOUNTAIN DALAM {Mathf.CeilToInt(secondsLeft)}...";
            }
        }

        private void HandleStateChange(GameManager.MatchState state)
        {
            if (state == GameManager.MatchState.Victory && victoryPanel != null)
            {
                if (respawnPanel != null) respawnPanel.SetActive(false);
                if (victoryDescText != null && GameManager.Instance != null)
                {
                    int mins = Mathf.FloorToInt(GameManager.Instance.matchTimer / 60f);
                    int secs = Mathf.FloorToInt(GameManager.Instance.matchTimer % 60f);
                    victoryDescText.text =
                        $"Seluruh {GameManager.Instance.totalEnemyTurrets} Turret Void telah hancur & Land of Dawn terselamatkan!\n" +
                        $"Waktu: {mins:00}:{secs:00}  |  Skor: {GameManager.Instance.playerKills} vs {GameManager.Instance.enemyKills}  |  Mutant Tertinggi: LV.{GameManager.Instance.enemyEvolutionLevel}";
                }
                victoryPanel.SetActive(true);
            }
            else if (state == GameManager.MatchState.Defeat && defeatPanel != null)
            {
                if (respawnPanel != null) respawnPanel.SetActive(false);
                defeatPanel.SetActive(true);
            }
            else if (state == GameManager.MatchState.Paused && pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
            else if (state == GameManager.MatchState.Playing && pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }
    }
}
