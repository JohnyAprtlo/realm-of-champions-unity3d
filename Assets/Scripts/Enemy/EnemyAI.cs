using System.Collections;
using UnityEngine;
using MobaGame.Combat;
using MobaGame.Core;

namespace MobaGame.Enemy
{
    public class EnemyAI : MonoBehaviour
    {
        public enum AIState { Idle, Patrol, Chase, Attack, Retreat, Dead }

        [Header("State & Evolution")]
        public AIState currentState = AIState.Patrol;
        public int evolutionLevel = 1;

        [Header("Movement & Range")]
        public float moveSpeed = 6.0f;
        public float detectionRadius = 13f;
        public float attackRange = 2.8f;
        public float retreatHpThreshold = 0.25f;

        [Header("Combat Stats")]
        public float attackDamage = 45f;
        public float attackCooldown = 0.9f;
        public float skillCooldown = 5.5f;
        public float skillDamage = 120f;

        [Header("References")]
        public HealthSystem healthSystem;
        public Rigidbody rb;
        public Transform playerTarget;
        public Transform baseSpawnPoint;
        public Transform centerPoint;
        public Transform visualModel;
        public Animator animator;

        private float lastAttackTime = 0f;
        private float lastSkillTime = 0f;
        private bool isMovingToCenter = true;

        private float baseMaxHealth;
        private float baseAttackDamage;
        private float baseSkillDamage;
        private float baseMoveSpeed;
        private Vector3 baseVisualScale = Vector3.one * 1.4f;
        private HealthSystem playerHealthSystem;

        private void Awake()
        {
            if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
            if (rb == null) rb = GetComponent<Rigidbody>();

            baseAttackDamage = attackDamage;
            baseSkillDamage = skillDamage;
            baseMoveSpeed = moveSpeed;
            if (healthSystem != null) baseMaxHealth = healthSystem.maxHealth;
        }

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visualModel != null) baseVisualScale = visualModel.localScale;

            if (playerTarget == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTarget = playerObj.transform;
            }
            if (playerTarget != null) playerHealthSystem = playerTarget.GetComponent<HealthSystem>();

            if (healthSystem != null)
            {
                healthSystem.OnDamaged += (dmg, isCrit) =>
                {
                    if (DamageTextManager.Instance != null)
                        DamageTextManager.Instance.SpawnDamageText(transform.position, dmg, isCrit);
                };
                healthSystem.OnDeath += HandleDeath;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.MatchState.Playing)
            {
                if (animator != null) animator.SetFloat("Speed", 0f);
                return;
            }

            if (healthSystem != null && healthSystem.isDead)
            {
                currentState = AIState.Dead;
                return;
            }

            if (playerTarget == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTarget = playerObj.transform;
                    playerHealthSystem = playerTarget.GetComponent<HealthSystem>();
                }
                if (playerTarget == null) return;
            }

            EvaluateState();
            ExecuteState();
        }

        private void EvaluateState()
        {
            float hpRatio = healthSystem.currentHealth / healthSystem.maxHealth;
            bool playerAlive = playerHealthSystem == null || !playerHealthSystem.isDead;
            float distToPlayer = playerAlive ? Vector3.Distance(transform.position, playerTarget.position) : float.MaxValue;

            if (hpRatio <= retreatHpThreshold && baseSpawnPoint != null)
            {
                float distToBase = Vector3.Distance(transform.position, baseSpawnPoint.position);
                if (distToBase > 4f)
                {
                    currentState = AIState.Retreat;
                    return;
                }
                else
                {
                    healthSystem.Heal(35f * Time.deltaTime);
                }
            }

            if (playerAlive && distToPlayer <= attackRange)
            {
                currentState = AIState.Attack;
            }
            else if (playerAlive && distToPlayer <= detectionRadius)
            {
                currentState = AIState.Chase;
            }
            else
            {
                currentState = AIState.Patrol;
            }
        }

        private void ExecuteState()
        {
            switch (currentState)
            {
                case AIState.Patrol:
                    PatrolBehavior();
                    break;
                case AIState.Chase:
                    MoveTowards(playerTarget.position);
                    break;
                case AIState.Attack:
                    AttackBehavior();
                    break;
                case AIState.Retreat:
                    if (baseSpawnPoint != null)
                        MoveTowards(baseSpawnPoint.position);
                    break;
                case AIState.Dead:
                    break;
            }
        }

        private void PatrolBehavior()
        {
            Vector3 target = isMovingToCenter && centerPoint != null ? centerPoint.position : (baseSpawnPoint != null ? baseSpawnPoint.position : transform.position);
            MoveTowards(target);

            if (Vector3.Distance(transform.position, target) < 2f)
            {
                isMovingToCenter = !isMovingToCenter;
            }
        }

        private void MoveTowards(Vector3 targetPos)
        {
            if (animator != null) animator.SetFloat("Speed", currentState == AIState.Patrol ? 0.5f : 1f);

            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0;

            if (dir != Vector3.zero)
            {
                rb.MovePosition(rb.position + dir * (moveSpeed * Time.deltaTime));
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
            }
        }

        private void AttackBehavior()
        {
            if (animator != null) animator.SetFloat("Speed", 0f);

            Vector3 lookDir = (playerTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

            if (Time.time >= lastSkillTime + skillCooldown)
            {
                PerformShadowSkill();
                lastSkillTime = Time.time;
                return;
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformBasicAttack();
                lastAttackTime = Time.time;
            }
        }

        private void PerformBasicAttack()
        {
            if (animator != null) animator.SetTrigger("Attack");

            HealthSystem playerHp = playerTarget.GetComponent<HealthSystem>();
            if (playerHp != null && !playerHp.isDead)
            {
                playerHp.TakeDamage(attackDamage, gameObject);
                if (CombatVFXManager.Instance != null)
                    CombatVFXManager.Instance.PlayHitImpact(playerTarget.position + Vector3.up * 1.2f, new Color(0.9f, 0.1f, 0.2f));
            }
        }

        private void PerformShadowSkill()
        {
            if (animator != null) animator.SetTrigger("Skill");

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.PlayShadowBurstFX(transform.position, 5f);

            Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    HealthSystem hp = hit.GetComponent<HealthSystem>();
                    if (hp != null)
                    {
                        hp.TakeDamage(skillDamage, gameObject);
                        if (CombatVFXManager.Instance != null)
                            CombatVFXManager.Instance.PlayHitImpact(hit.transform.position + Vector3.up * 1.2f, new Color(0.6f, 0.05f, 0.4f));
                    }
                }
            }
        }

        private void HandleDeath()
        {
            currentState = AIState.Dead;
            if (animator != null) animator.SetTrigger("Die");
            StartCoroutine(DelayedDeathDeactivate());
        }

        private IEnumerator DelayedDeathDeactivate()
        {
            yield return new WaitForSeconds(2.5f);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Respawns the Mutant stronger with higher HP, attack, skill damage, speed, and stature.
        /// </summary>
        public void RespawnWithEvolution(int newLevel, Vector3 spawnPos)
        {
            StopAllCoroutines();
            evolutionLevel = Mathf.Max(1, newLevel);

            float scaledHp = baseMaxHealth + (evolutionLevel - 1) * 600f;
            attackDamage = baseAttackDamage + (evolutionLevel - 1) * 18f;
            skillDamage = baseSkillDamage + (evolutionLevel - 1) * 40f;
            moveSpeed = Mathf.Min(baseMoveSpeed + (evolutionLevel - 1) * 0.45f, 9.5f);

            if (visualModel != null)
            {
                float scaleMult = Mathf.Min(1f + (evolutionLevel - 1) * 0.06f, 1.35f);
                visualModel.localScale = baseVisualScale * scaleMult;
            }

            gameObject.SetActive(true);
            transform.position = spawnPos;
            transform.rotation = Quaternion.Euler(0f, -135f, 0f);

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (healthSystem != null)
            {
                healthSystem.Revive(scaledHp);
            }

            currentState = AIState.Patrol;
            isMovingToCenter = true;

            if (animator != null)
            {
                animator.ResetTrigger("Die");
                animator.ResetTrigger("Respawn");
                animator.SetFloat("Speed", 0f);
                animator.Play("Idle", 0, 0f);
            }

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.PlayShadowBurstFX(spawnPos, 4f);
        }
    }
}
