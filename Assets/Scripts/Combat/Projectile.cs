using UnityEngine;

namespace MobaGame.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Stats")]
        public float speed = 15f;
        public float damage = 80f;
        public float lifetime = 4f;
        public string targetTag = "Enemy";
        public GameObject owner;

        private Vector3 direction;
        private bool isInitialized = false;

        public void Initialize(Vector3 dir, float dmg, string target, GameObject shooter = null)
        {
            direction = dir.normalized;
            damage = dmg;
            targetTag = target;
            owner = shooter;
            isInitialized = true;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!isInitialized) return;
            transform.position += direction * (speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == owner) return;

            if (other.CompareTag(targetTag))
            {
                HealthSystem targetHealth = other.GetComponent<HealthSystem>();
                if (targetHealth != null && !targetHealth.isDead)
                {
                    targetHealth.TakeDamage(damage, owner);
                }
                Destroy(gameObject);
            }
            else if (other.CompareTag("Obstacle") || other.CompareTag("Wall"))
            {
                Destroy(gameObject);
            }
        }
    }
}
