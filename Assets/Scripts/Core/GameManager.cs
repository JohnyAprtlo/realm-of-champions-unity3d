using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using MobaGame.Combat;
using MobaGame.Player;
using MobaGame.Enemy;

namespace MobaGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum MatchState { Playing, Victory, Defeat, Paused }

        [Header("Match State")]
        public MatchState currentState = MatchState.Playing;
        public float matchTimer = 0f;
        public int playerKills = 0;
        public int enemyKills = 0;

        [Header("Turret Objective")]
        public int totalEnemyTurrets = 6;
        public int destroyedEnemyTurrets = 0;

        [Header("Respawn & Boss Evolution")]
        public float playerRespawnDelay = 5f;
        public float enemyRespawnDelay = 6f;
        public int enemyEvolutionLevel = 1;
        public Vector3 playerSpawnPosition = new Vector3(-65f, 0.15f, -69f);
        public Vector3 enemySpawnPosition = new Vector3(68f, 0.15f, 68f);

        [Header("Entity References")]
        public HealthSystem playerHealth;
        public HealthSystem enemyHealth;
        public HealthSystem enemyCore;
        public HealthSystem playerCore;
        public PlayerController playerController;
        public EnemyAI enemyAI;

        public float PlayerRespawnRemaining { get; private set; }
        public float EnemyRespawnRemaining { get; private set; }

        public event Action<MatchState> OnMatchStateChanged;
        public event Action<int, int> OnScoreChanged;
        public event Action<int, int> OnTurretProgressChanged;
        public event Action<string, string> OnStoryUpdate;
        public event Action<float> OnPlayerRespawnTick;
        public event Action<int, float> OnEnemyRespawnTick;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Time.timeScale = 1f;

            if (playerHealth != null)
                playerHealth.OnDeath += OnPlayerDied;

            if (enemyHealth != null)
                enemyHealth.OnDeath += OnEnemyDied;

            // Count active enemy turrets if not already counted
            Turret[] allTurrets = FindObjectsByType<Turret>();
            int enemyCount = 0;
            foreach (var t in allTurrets)
            {
                if (t.isEnemyTurret) enemyCount++;
            }
            if (enemyCount > 0) totalEnemyTurrets = enemyCount;

            OnTurretProgressChanged?.Invoke(destroyedEnemyTurrets, totalEnemyTurrets);
            PublishStory(
                "BABAK 1: INVASI VOID",
                $"Maria melangkah ke Land of Dawn! Hancurkan seluruh {totalEnemyTurrets} Turret Void untuk mengakhiri invasi Mutant."
            );
        }

        private void Update()
        {
            if (currentState != MatchState.Playing) return;

            matchTimer += Time.deltaTime;

            if (PlayerRespawnRemaining > 0f)
            {
                PlayerRespawnRemaining -= Time.deltaTime;
                if (PlayerRespawnRemaining <= 0f)
                {
                    PlayerRespawnRemaining = 0f;
                    RespawnPlayer();
                }
                OnPlayerRespawnTick?.Invoke(PlayerRespawnRemaining);
            }

            if (EnemyRespawnRemaining > 0f)
            {
                EnemyRespawnRemaining -= Time.deltaTime;
                if (EnemyRespawnRemaining <= 0f)
                {
                    EnemyRespawnRemaining = 0f;
                    RespawnAndEvolveEnemy();
                }
                OnEnemyRespawnTick?.Invoke(enemyEvolutionLevel, EnemyRespawnRemaining);
            }
        }

        public void SetMatchState(MatchState newState)
        {
            currentState = newState;
            OnMatchStateChanged?.Invoke(newState);

            if (newState == MatchState.Victory || newState == MatchState.Defeat)
            {
                Time.timeScale = 0.25f; // Slow motion on victory/defeat
            }
        }

        private void OnPlayerDied()
        {
            if (currentState != MatchState.Playing) return;
            enemyKills++;
            OnScoreChanged?.Invoke(playerKills, enemyKills);

            PlayerRespawnRemaining = playerRespawnDelay;
            OnPlayerRespawnTick?.Invoke(PlayerRespawnRemaining);

            PublishStory(
                "MARIA GUGUR!",
                $"Cahaya Dawnblade menarikmu kembali ke Fountain. Respawn dalam {Mathf.CeilToInt(playerRespawnDelay)} detik..."
            );
        }

        private void RespawnPlayer()
        {
            if (playerController != null)
            {
                playerController.RespawnAtFountain(playerSpawnPosition);
            }
            else if (playerHealth != null)
            {
                playerHealth.Revive(playerHealth.maxHealth);
            }

            int remaining = Mathf.Max(0, totalEnemyTurrets - destroyedEnemyTurrets);
            PublishStory(
                "MARIA BANGKIT KEMBALI!",
                $"Pulih di Fountain! Sisa {remaining} Turret Void yang harus dihancurkan."
            );
        }

        private void OnEnemyDied()
        {
            if (currentState != MatchState.Playing) return;
            playerKills++;
            OnScoreChanged?.Invoke(playerKills, enemyKills);

            EnemyRespawnRemaining = enemyRespawnDelay;
            OnEnemyRespawnTick?.Invoke(enemyEvolutionLevel, EnemyRespawnRemaining);

            PublishStory(
                $"MUTANT LV.{enemyEvolutionLevel} TUMBANG!",
                $"Energi Void menyerap kegelapan... Mutant akan bangkit lebih kuat dalam {Mathf.CeilToInt(enemyRespawnDelay)} detik! Serang Turret sekarang!"
            );
        }

        private void RespawnAndEvolveEnemy()
        {
            enemyEvolutionLevel++;

            if (enemyAI != null)
            {
                enemyAI.RespawnWithEvolution(enemyEvolutionLevel, enemySpawnPosition);
            }
            else if (enemyHealth != null)
            {
                enemyHealth.Revive(enemyHealth.maxHealth + 600f);
            }

            float newHp = enemyHealth != null ? enemyHealth.maxHealth : (1000f + (enemyEvolutionLevel - 1) * 600f);
            OnEnemyRespawnTick?.Invoke(enemyEvolutionLevel, 0f);

            if (enemyEvolutionLevel == 2)
            {
                PublishStory(
                    "BABAK 2: AMARAH MUTANT (LV.2)",
                    $"Void membangkitkan Mutant dengan HP {Mathf.RoundToInt(newHp)} dan serangan lebih brutal!"
                );
            }
            else if (enemyEvolutionLevel == 3)
            {
                PublishStory(
                    "BABAK 3: EVOLUSI VOID REAVER (LV.3)",
                    $"Mutant bermutasi (HP {Mathf.RoundToInt(newHp)})! Jangan beri ampun—hancurkan semua Turret!"
                );
            }
            else
            {
                PublishStory(
                    $"BABAK FINAL: OVERLORD VOID (LV.{enemyEvolutionLevel})",
                    $"Kekuatan puncak Mutant bangkit (HP {Mathf.RoundToInt(newHp)})! Akhiri invasi dengan menghancurkan sisa Turret!"
                );
            }
        }

        public void NotifyTurretDestroyed(Turret turret)
        {
            if (turret == null || currentState != MatchState.Playing) return;

            if (turret.isEnemyTurret)
            {
                destroyedEnemyTurrets++;
                OnTurretProgressChanged?.Invoke(destroyedEnemyTurrets, totalEnemyTurrets);

                if (destroyedEnemyTurrets >= totalEnemyTurrets)
                {
                    PublishStory(
                        "KEGELAPAN VOID RUNTUH!",
                        "Seluruh 6 Turret Void telah dihancurkan! Cahaya Dawnblade menyelamatkan Land of Dawn!"
                    );
                    SetMatchState(MatchState.Victory);
                }
                else
                {
                    int remaining = totalEnemyTurrets - destroyedEnemyTurrets;
                    if (remaining == 1)
                    {
                        PublishStory(
                            $"KLIMAKS! ({destroyedEnemyTurrets}/{totalEnemyTurrets} TURRET HANCUR)",
                            $"{turret.turretName} meledak! Tinggal 1 Turret terakhir untuk memenangkan pertempuran!"
                        );
                    }
                    else if (destroyedEnemyTurrets >= totalEnemyTurrets / 2)
                    {
                        PublishStory(
                            $"SERANGAN BALIK ({destroyedEnemyTurrets}/{totalEnemyTurrets} TURRET HANCUR)",
                            $"{turret.turretName} runtuh! Separuh pertahanan Void telah hancur!"
                        );
                    }
                    else
                    {
                        PublishStory(
                            $"TURRET HANCUR ({destroyedEnemyTurrets}/{totalEnemyTurrets})",
                            $"{turret.turretName} hancur! Terus maju, waspadai Mutant yang terus berevolusi."
                        );
                    }
                }
            }
            else if (turret.isCore)
            {
                PublishStory(
                    "CRYSTAL CORE HANCUR!",
                    "Benteng suci Maria telah runtuh ditelan kegelapan Void..."
                );
                SetMatchState(MatchState.Defeat);
            }
        }

        private void PublishStory(string title, string message)
        {
            OnStoryUpdate?.Invoke(title, message);
        }

        public void RestartBattle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public void TogglePause()
        {
            if (currentState == MatchState.Playing)
            {
                currentState = MatchState.Paused;
                Time.timeScale = 0f;
                OnMatchStateChanged?.Invoke(MatchState.Paused);
            }
            else if (currentState == MatchState.Paused)
            {
                currentState = MatchState.Playing;
                Time.timeScale = 1f;
                OnMatchStateChanged?.Invoke(MatchState.Playing);
            }
        }
    }
}
