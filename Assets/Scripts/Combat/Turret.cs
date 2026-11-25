using UnityEngine;
using MobaGame.Core;

namespace MobaGame.Combat
{
    public class Turret : MonoBehaviour
    {
        [Header("Turret Identification")]
        public string turretName = "Outer Turret";
        public string targetTag = "Enemy"; // Turret Player attacks "Enemy", Turret Enemy attacks "Player"
        public bool isEnemyTurret = false;
        public bool isCore = false;

        [Header("Combat Settings")]
        public float attackRadius = 9f;
        public float attackDamage = 45f;
        public float attackCooldown = 1.4f;
        public Transform firePoint;

        [Header("References")]
        public HealthSystem healthSystem;
        public LineRenderer laserBeam;

        private Transform currentTarget;
        private float lastAttackTime = 0f;
        private bool deathHandled = false;

        private void Start()
        {
            if (healthSystem == null)
                healthSystem = GetComponent<HealthSystem>();

            if (firePoint == null)
                firePoint = transform;

            if (laserBeam != null)
            {
                laserBeam.enabled = false;
                laserBeam.positionCount = 2;
            }

            if (healthSystem != null)
            {
                healthSystem.OnDamaged += (dmg, isCrit) =>
                {
                    if (DamageTextManager.Instance != null)
                        DamageTextManager.Instance.SpawnDamageText(transform.position + Vector3.up * 2.5f, dmg, isCrit);
                };

                healthSystem.OnDeath += HandleTurretDestroyed;
            }
        }

        private void HandleTurretDestroyed()
        {
            if (deathHandled) return;
            deathHandled = true;

            if (laserBeam != null) laserBeam.enabled = false;

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.PlaySolarFlareAOE(transform.position + Vector3.up * 1.5f, 4.5f);

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayUltimate();

            if (GameManager.Instance != null)
                GameManager.Instance.NotifyTurretDestroyed(this);

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.MatchState.Playing)
            {
                if (laserBeam != null) laserBeam.enabled = false;
                return;
            }

            if (healthSystem != null && healthSystem.isDead)
            {
                HandleTurretDestroyed();
                return;
            }

            FindTarget();

            if (currentTarget != null)
            {
                // Aim laser
                if (laserBeam != null)
                {
                    laserBeam.enabled = true;
                    laserBeam.SetPosition(0, firePoint.position);
                    laserBeam.SetPosition(1, currentTarget.position + Vector3.up);
                }

                // Attack
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                }
            }
            else
            {
                if (laserBeam != null)
                    laserBeam.enabled = false;
            }
        }

        private void FindTarget()
        {
            // Verify current target still valid
            if (currentTarget != null)
            {
                float dist = Vector3.Distance(transform.position, currentTarget.position);
                HealthSystem targetHp = currentTarget.GetComponent<HealthSystem>();
                if (dist > attackRadius || (targetHp != null && targetHp.isDead) || !currentTarget.gameObject.activeInHierarchy)
                {
                    currentTarget = null;
                }
            }

            if (currentTarget == null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, attackRadius);
                float closestDist = float.MaxValue;
                Transform bestTarget = null;

                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag(targetTag))
                    {
                        HealthSystem hp = hit.GetComponent<HealthSystem>();
                        if (hp != null && !hp.isDead)
                        {
                            float d = Vector3.Distance(transform.position, hit.transform.position);
                            if (d < closestDist)
                            {
                                closestDist = d;
                                bestTarget = hit.transform;
                            }
                        }
                    }
                }
                currentTarget = bestTarget;
            }
        }

        private void PerformAttack()
        {
            if (currentTarget == null) return;
            HealthSystem hp = currentTarget.GetComponent<HealthSystem>();
            if (hp != null)
            {
                hp.TakeDamage(attackDamage, gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        }
    }
}
