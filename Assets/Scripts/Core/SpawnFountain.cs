using UnityEngine;
using MobaGame.Combat;
using MobaGame.Player;

namespace MobaGame.Core
{
    public class SpawnFountain : MonoBehaviour
    {
        public string targetTag = "Player";
        public float healPerSecond = 120f;
        public float manaPerSecond = 80f;

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag(targetTag)) return;

            HealthSystem hp = other.GetComponent<HealthSystem>();
            if (hp != null && !hp.isDead)
            {
                hp.Heal(healPerSecond * Time.deltaTime);
            }

            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.currentMana = Mathf.Min(pc.currentMana + manaPerSecond * Time.deltaTime, pc.maxMana);
            }
        }
    }
}
