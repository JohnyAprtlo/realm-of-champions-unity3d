using UnityEngine;

namespace MobaGame.Core
{
    public class WaterFlow : MonoBehaviour
    {
        public Vector2 flowSpeed = new Vector2(0.05f, 0.12f);
        private Renderer rend;

        private void Start()
        {
            rend = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (rend != null && rend.material != null)
            {
                rend.material.mainTextureOffset += flowSpeed * Time.deltaTime;
            }
        }
    }
}
