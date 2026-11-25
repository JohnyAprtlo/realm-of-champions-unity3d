using System;
using System.Collections;
using UnityEngine;
using MobaGame.Combat;
using MobaGame.Core;

namespace MobaGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement & Jump Settings")]
        public float walkSpeed = 5.5f;
        public float runSpeed = 10.5f;
        public float rotationSpeed = 16f;
        public float jumpForce = 7.5f;
        public LayerMask groundLayer = ~0;
        private bool isGrounded = true;

        [Header("Stats & Mana")]
        public float maxMana = 400f;
        public float currentMana;
        public float manaRegenRate = 14f;

        [Header("Combat Settings")]
        public float basicAttackDamage = 65f;
        public float basicAttackRange = 6.5f;
        public float basicAttackCooldown = 0.55f;
        public Transform attackPoint;

        [Header("Skills Configuration")]
        [Tooltip("Skill 1: Radiant Dash")]
        public float skill1Cooldown = 4.5f;
        public float skill1Damage = 140f;
        public float skill1ManaCost = 45f;
        public float dashDistance = 7f;

        [Tooltip("Skill 2: Solar Flare AOE")]
        public float skill2Cooldown = 7f;
        public float skill2Damage = 210f;
        public float skill2ManaCost = 75f;
        public float skill2Radius = 6f;

        [Tooltip("Skill 3: Celestial Judgement (Ultimate)")]
        public float ultCooldown = 13f;
        public float ultDamage = 450f;
        public float ultManaCost = 120f;
        public float ultRange = 10f;

        [Header("References")]
        public HealthSystem healthSystem;
        public Rigidbody rb;
        public Transform visualModel;
        public Animator animator;

        // Timers
        public float lastBasicAttackTime = -999f;
        public float skill1Timer = 0f;
        public float skill2Timer = 0f;
        public float ultTimer = 0f;

        // Movement
        private float lastJumpTime = -999f;
        private CameraFollow camFollow;
        private Transform hipsBone;

        private void Awake()
        {
            if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
            if (rb == null) rb = GetComponent<Rigidbody>();
            currentMana = maxMana;
        }

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
            if (Camera.main != null)
            {
                camFollow = Camera.main.GetComponent<CameraFollow>();
            }

            if (healthSystem != null)
            {
                healthSystem.OnDamaged += (dmg, isCrit) =>
                {
                    if (DamageTextManager.Instance != null)
                        DamageTextManager.Instance.SpawnDamageText(transform.position, dmg, isCrit);
                };

                healthSystem.OnDeath += () =>
                {
                    if (animator != null) animator.SetTrigger("Die");
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayDefeat();
                };
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.MatchState.Playing)
            {
                if (animator != null) animator.SetFloat("Speed", 0f);
                return;
            }
            if (healthSystem != null && healthSystem.isDead) return;

            UpdateTimersAndMana();
            HandleInput();
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameManager.MatchState.Playing)
            {
                if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return;
            }
            if (healthSystem != null && healthSystem.isDead)
            {
                if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return;
            }
            HandleMovement();
        }

        /// <summary>
        /// Revives the player at the spawn fountain with full HP and Mana.
        /// </summary>
        public void RespawnAtFountain(Vector3 spawnPos)
        {
            transform.position = spawnPos;
            transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (healthSystem != null)
            {
                healthSystem.Revive(healthSystem.maxHealth);
            }

            currentMana = maxMana;
            isGrounded = true;

            if (animator != null)
            {
                animator.ResetTrigger("Die");
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsGrounded", true);
                animator.SetTrigger("Respawn");
            }

            if (camFollow != null)
            {
                camFollow.SnapToTarget();
            }

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.PlayShieldBubble(transform, 1.5f);
        }

        private void LateUpdate()
        {
            // Keep Hips centered in XZ so non-in-place Mixamo clips (like Run Forward) never sway left/right
            if (hipsBone != null && isGrounded && (healthSystem == null || !healthSystem.isDead))
            {
                Vector3 lp = hipsBone.localPosition;
                lp.x = 0f;
                lp.z = 0f;
                hipsBone.localPosition = lp;
            }
        }

        private void UpdateTimersAndMana()
        {
            if (skill1Timer > 0) skill1Timer -= Time.deltaTime;
            if (skill2Timer > 0) skill2Timer -= Time.deltaTime;
            if (ultTimer > 0) ultTimer -= Time.deltaTime;

            if (currentMana < maxMana)
            {
                currentMana = Mathf.Min(currentMana + manaRegenRate * Time.deltaTime, maxMana);
            }
        }

        private void HandleInput()
        {
            CheckGrounded();

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded) PerformJump();
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F)) PerformBasicAttack();
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) CastSkill1();
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) CastSkill2();
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) CastUltimate();
        }

        private void CheckGrounded()
        {
            if (Time.time < lastJumpTime + 0.22f)
            {
                isGrounded = false;
            }
            else
            {
                isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.35f, groundLayer, QueryTriggerInteraction.Ignore);
            }

            if (animator != null)
            {
                animator.SetBool("IsGrounded", isGrounded);
            }
        }

        public void PerformJump()
        {
            if (rb == null) return;
            isGrounded = false;
            lastJumpTime = Time.time;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            if (animator != null)
            {
                animator.SetBool("IsGrounded", false);
                animator.SetTrigger("Jump");
            }
        }

        private void HandleMovement()
        {
            // Get camera-relative movement direction for third person
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 inputDir = Vector3.zero;

            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                // Lock movement strictly to camera's yaw so camera & character never drift apart
                if (camFollow != null)
                {
                    inputDir = (camFollow.ForwardFlat * v + camFollow.RightFlat * h).normalized;
                }
                else
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 camForward = cam.transform.forward;
                        Vector3 camRight = cam.transform.right;
                        camForward.y = 0f;
                        camRight.y = 0f;
                        camForward.Normalize();
                        camRight.Normalize();

                        inputDir = (camForward * v + camRight * h).normalized;
                    }
                    else
                    {
                        inputDir = new Vector3(h, 0f, v).normalized;
                    }
                }
            }

            bool isMoving = inputDir.magnitude > 0.1f;
            bool isSprinting = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            float currentSpeed = isSprinting ? runSpeed : walkSpeed;

            if (animator != null)
            {
                float animSpeed = !isMoving ? 0f : (isSprinting ? 1.0f : 0.5f);
                animator.SetFloat("Speed", animSpeed, 0.08f, Time.fixedDeltaTime);
            }

            if (isMoving)
            {
                Vector3 moveVel = inputDir * currentSpeed;
                rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);

                Quaternion targetRot = Quaternion.LookRotation(inputDir);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
            }
            else
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }

        public void PerformBasicAttack()
        {
            if (Time.time < lastBasicAttackTime + basicAttackCooldown) return;
            lastBasicAttackTime = Time.time;

            if (animator != null) animator.SetTrigger("Attack");
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySlash();

            Transform target = FindNearestTarget(basicAttackRange);
            if (target != null)
            {
                FaceTarget(target.position);
                HealthSystem targetHp = target.GetComponent<HealthSystem>();
                if (targetHp != null && !targetHp.isDead)
                {
                    targetHp.TakeDamage(basicAttackDamage, gameObject);
                    SpawnEnergySlash(target.position);
                    if (SoundManager.Instance != null) SoundManager.Instance.PlayHit();
                    if (CombatVFXManager.Instance != null)
                        CombatVFXManager.Instance.PlayHitImpact(target.position + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.2f));
                }
            }
            else
            {
                Vector3 slashPos = transform.position + transform.forward * 2f;
                SpawnEnergySlash(slashPos);
            }
        }

        public void CastSkill1() // Radiant Dash
        {
            if (skill1Timer > 0 || currentMana < skill1ManaCost) return;
            currentMana -= skill1ManaCost;
            skill1Timer = skill1Cooldown;

            if (animator != null) animator.SetTrigger("Skill1");
            if (SoundManager.Instance != null) SoundManager.Instance.PlayDash();

            Transform target = FindNearestTarget(dashDistance * 1.5f);
            Vector3 dashDir = target != null ? (target.position - transform.position).normalized : transform.forward;
            dashDir.y = 0;

            Vector3 startPos = transform.position;
            Vector3 targetPos = transform.position + dashDir * dashDistance;

            if (CombatVFXManager.Instance != null)
                CombatVFXManager.Instance.PlayDashFX(startPos, targetPos);

            RaycastHit[] hits = Physics.SphereCastAll(transform.position, 2f, dashDir, dashDistance);
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    HealthSystem hp = hit.collider.GetComponent<HealthSystem>();
                    if (hp != null && !hp.isDead)
                    {
                        hp.TakeDamage(skill1Damage, gameObject);
                        if (SoundManager.Instance != null) SoundManager.Instance.PlayHit();
                        if (CombatVFXManager.Instance != null)
                            CombatVFXManager.Instance.PlayHitImpact(hit.collider.transform.position + Vector3.up * 1.2f, new Color(0.2f, 0.8f, 1f));
                    }
                }
            }

            transform.position = targetPos;
        }

        public void CastSkill2() // Solar Flare AOE & Shield
        {
            if (skill2Timer > 0 || currentMana < skill2ManaCost) return;
            currentMana -= skill2ManaCost;
            skill2Timer = skill2Cooldown;

            if (animator != null) animator.SetTrigger("Skill2");
            if (SoundManager.Instance != null) SoundManager.Instance.PlayShield();

            healthSystem.AddShield(120f);

            if (CombatVFXManager.Instance != null)
            {
                CombatVFXManager.Instance.PlaySolarFlareAOE(transform.position, skill2Radius);
                CombatVFXManager.Instance.PlayShieldBubble(transform, 3f);
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, skill2Radius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    HealthSystem hp = hit.GetComponent<HealthSystem>();
                    if (hp != null && !hp.isDead)
                    {
                        hp.TakeDamage(skill2Damage, gameObject);
                        if (SoundManager.Instance != null) SoundManager.Instance.PlayHit();
                        if (CombatVFXManager.Instance != null)
                            CombatVFXManager.Instance.PlayHitImpact(hit.transform.position + Vector3.up * 1.2f, new Color(1f, 0.9f, 0.3f));
                    }
                }
            }
        }

        public void CastUltimate() // Celestial Judgement
        {
            if (ultTimer > 0 || currentMana < ultManaCost) return;
            currentMana -= ultManaCost;
            ultTimer = ultCooldown;

            if (animator != null) animator.SetTrigger("Ultimate");
            if (SoundManager.Instance != null) SoundManager.Instance.PlayUltimate();

            Transform target = FindNearestTarget(ultRange);
            if (target != null)
            {
                FaceTarget(target.position);

                if (CombatVFXManager.Instance != null)
                    CombatVFXManager.Instance.PlayCelestialJudgementFX(target.position);

                HealthSystem hp = target.GetComponent<HealthSystem>();
                if (hp != null && !hp.isDead)
                {
                    hp.TakeDamage(ultDamage, gameObject);

                    Rigidbody enemyRb = target.GetComponent<Rigidbody>();
                    if (enemyRb != null)
                    {
                        Vector3 knockDir = (target.position - transform.position).normalized + Vector3.up * 0.5f;
                        enemyRb.AddForce(knockDir * 14f, ForceMode.Impulse);
                    }
                }
            }
        }

        private Transform FindNearestTarget(float range)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range);
            Transform nearest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    HealthSystem hp = hit.GetComponent<HealthSystem>();
                    if (hp != null && !hp.isDead)
                    {
                        float d = Vector3.Distance(transform.position, hit.transform.position);
                        if (d < minDist)
                        {
                            minDist = d;
                            nearest = hit.transform;
                        }
                    }
                }
            }
            return nearest;
        }

        private void FaceTarget(Vector3 targetPos)
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }

        private void SpawnEnergySlash(Vector3 targetPos)
        {
            GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            slash.name = "SlashFX";
            slash.transform.position = (transform.position + targetPos) * 0.5f + Vector3.up * 1f;
            slash.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Destroy(slash.GetComponent<Collider>());
            Destroy(slash, 0.2f);
        }
    }
}
