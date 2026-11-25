using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MobaGame.Combat
{
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SpawnDamageText(Vector3 worldPos, float damage, bool isCritical)
        {
            StartCoroutine(AnimateDamageText(worldPos + Vector3.up * 2.2f, damage, isCritical));
        }

        private IEnumerator AnimateDamageText(Vector3 startPos, float damage, bool isCritical)
        {
            GameObject textObj = new GameObject("DamageText");
            textObj.transform.position = startPos;

            Canvas canvas = textObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 60);
            textObj.transform.localScale = new Vector3(0.018f, 0.018f, 0.018f);

            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = isCritical ? 34 : 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;

            if (isCritical)
            {
                text.text = $"-{Mathf.RoundToInt(damage)} CRITICAL!";
                text.color = new Color(1f, 0.25f, 0.15f);
            }
            else
            {
                text.text = $"-{Mathf.RoundToInt(damage)}";
                text.color = new Color(1f, 0.9f, 0.3f);
            }

            Camera mainCam = Camera.main;
            float duration = 0.9f;
            float elapsed = 0f;
            Vector3 endPos = startPos + Vector3.up * 1.5f + Random.insideUnitSphere * 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                if (textObj != null)
                {
                    textObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
                    if (mainCam != null) textObj.transform.forward = mainCam.transform.forward;

                    Color c = text.color;
                    c.a = Mathf.Lerp(1f, 0f, progress * progress);
                    text.color = c;
                }
                yield return null;
            }

            Destroy(textObj);
        }
    }
}
