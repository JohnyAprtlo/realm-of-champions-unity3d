using System.Collections;
using UnityEngine;
using MobaGame.Core;

namespace MobaGame.Combat
{
    public class CombatVFXManager : MonoBehaviour
    {
        public static CombatVFXManager Instance { get; private set; }

        private Material vfxAdditiveMat;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitVFXMaterial();
        }

        private void InitVFXMaterial()
        {
            Shader shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            vfxAdditiveMat = new Material(shader);
        }

        // 1. Basic Attack Hit Sparks & Slash Burst
        public void PlayHitImpact(Vector3 pos, Color sparkColor)
        {
            StartCoroutine(SpawnHitImpactCoroutine(pos, sparkColor));
        }

        private IEnumerator SpawnHitImpactCoroutine(Vector3 pos, Color sparkColor)
        {
            GameObject fxObj = new GameObject("HitImpactFX");
            fxObj.transform.position = pos;

            // Flash sphere
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.parent = fxObj.transform;
            flash.transform.localPosition = Vector3.zero;
            flash.transform.localScale = Vector3.one * 0.4f;
            Destroy(flash.GetComponent<Collider>());

            Material flashMat = new Material(Shader.Find("Unlit/Color"));
            flashMat.color = sparkColor;
            flash.GetComponent<MeshRenderer>().sharedMaterial = flashMat;

            // Debris/Sparks using primitive sparks
            for (int i = 0; i < 6; i++)
            {
                GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.transform.parent = fxObj.transform;
                spark.transform.localPosition = Random.insideUnitSphere * 0.2f;
                spark.transform.localScale = Vector3.one * 0.12f;
                Destroy(spark.GetComponent<Collider>());
                spark.GetComponent<MeshRenderer>().sharedMaterial = flashMat;

                Vector3 flyDir = (Random.onUnitSphere + Vector3.up * 0.5f).normalized;
                StartCoroutine(MoveAndFadeSpark(spark, flyDir, 0.25f));
            }

            // Expand & fade flash
            float elapsed = 0f;
            float dur = 0.2f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                if (flash != null) flash.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.2f, t);
                yield return null;
            }

            Destroy(fxObj);
        }

        private IEnumerator MoveAndFadeSpark(GameObject spark, Vector3 dir, float dur)
        {
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                if (spark != null)
                {
                    spark.transform.position += dir * (8f * Time.deltaTime);
                    spark.transform.localScale *= (1f - Time.deltaTime * 3f);
                }
                yield return null;
            }
        }

        // 2. Skill 1: Radiant Dash Trail & Shockwave
        public void PlayDashFX(Vector3 startPos, Vector3 endPos)
        {
            StartCoroutine(SpawnDashTrail(startPos, endPos));
        }

        private IEnumerator SpawnDashTrail(Vector3 start, Vector3 end)
        {
            GameObject trailObj = new GameObject("DashTrailFX");
            LineRenderer lr = trailObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start + Vector3.up * 1f);
            lr.SetPosition(1, end + Vector3.up * 1f);
            lr.startWidth = 1.2f;
            lr.endWidth = 0.2f;

            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            lr.material = mat;

            // Shockwave ring at arrival
            PlayGroundShockwave(end, 3.5f, new Color(0.2f, 0.8f, 1f));

            float dur = 0.35f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / dur);
                if (lr != null)
                {
                    lr.startWidth = Mathf.Lerp(1.2f, 0f, elapsed / dur);
                }
                yield return null;
            }

            Destroy(trailObj);
        }

        // 3. Skill 2: Solar Flare AOE & Shield Bubble
        public void PlaySolarFlareAOE(Vector3 centerPos, float radius)
        {
            PlayGroundShockwave(centerPos, radius, new Color(1f, 0.85f, 0.2f));
        }

        public void PlayShieldBubble(Transform parent, float duration)
        {
            StartCoroutine(SpawnShieldBubbleCoroutine(parent, duration));
        }

        private IEnumerator SpawnShieldBubbleCoroutine(Transform parent, float duration)
        {
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shield.name = "ShieldAuraFX";
            shield.transform.parent = parent;
            shield.transform.localPosition = new Vector3(0, 0.15f, 0);
            shield.transform.localScale = new Vector3(3.2f, 0.05f, 3.2f);
            Destroy(shield.GetComponent<Collider>());

            Material sMat = new Material(Shader.Find("Unlit/Color"));
            sMat.color = new Color(0.2f, 0.85f, 1f, 1f);
            shield.GetComponent<MeshRenderer>().sharedMaterial = sMat;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (shield != null)
                {
                    float pulse = 1f + Mathf.Sin(elapsed * 8f) * 0.1f;
                    shield.transform.localScale = new Vector3(3.2f * pulse, 0.05f, 3.2f * pulse);
                }
                yield return null;
            }

            Destroy(shield);
        }

        // 4. Skill 3 / Ultimate: Celestial Judgement (Sky Sword + Massive Crater Shockwave)
        public void PlayCelestialJudgementFX(Vector3 targetPos)
        {
            StartCoroutine(SpawnCelestialJudgementCoroutine(targetPos));
        }

        private IEnumerator SpawnCelestialJudgementCoroutine(Vector3 targetPos)
        {
            // Camera Shake on Ultimate!
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake(0.45f, 0.55f);
            }

            GameObject ultObj = new GameObject("CelestialJudgementFX");
            ultObj.transform.position = targetPos;

            // Giant Beam of Light falling from sky (Cylinder)
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "SkyBeam";
            beam.transform.parent = ultObj.transform;
            beam.transform.position = targetPos + Vector3.up * 10f;
            beam.transform.localScale = new Vector3(3f, 12f, 3f);
            Destroy(beam.GetComponent<Collider>());

            Material beamMat = new Material(Shader.Find("Unlit/Color"));
            beamMat.color = new Color(1f, 0.92f, 0.4f, 0.9f);
            beam.GetComponent<MeshRenderer>().sharedMaterial = beamMat;

            // Golden Ground Shockwave
            PlayGroundShockwave(targetPos, 8f, new Color(1f, 0.8f, 0.1f));

            // Giant Sword falling animation
            GameObject giantSword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            giantSword.name = "CelestialSword";
            giantSword.transform.parent = ultObj.transform;
            giantSword.transform.position = targetPos + Vector3.up * 18f;
            giantSword.transform.localScale = new Vector3(0.6f, 7f, 1.2f);
            Destroy(giantSword.GetComponent<Collider>());
            giantSword.GetComponent<MeshRenderer>().sharedMaterial = beamMat;

            float dur = 0.5f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                if (giantSword != null)
                {
                    giantSword.transform.position = Vector3.Lerp(targetPos + Vector3.up * 18f, targetPos + Vector3.up * 3.5f, t * t);
                }
                if (beam != null)
                {
                    beam.transform.localScale = new Vector3(Mathf.Lerp(3f, 0.1f, t), 12f, Mathf.Lerp(3f, 0.1f, t));
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
            Destroy(ultObj);
        }

        // 5. Enemy Malakar: Shadow Burst AOE
        public void PlayShadowBurstFX(Vector3 centerPos, float radius)
        {
            PlayGroundShockwave(centerPos, radius, new Color(0.75f, 0.05f, 0.25f));
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake(0.25f, 0.3f);
            }
        }

        // Shared Expanding Shockwave Cylinder/Disk
        public void PlayGroundShockwave(Vector3 center, float maxRadius, Color color)
        {
            StartCoroutine(SpawnShockwaveCoroutine(center, maxRadius, color));
        }

        private IEnumerator SpawnShockwaveCoroutine(Vector3 center, float maxRadius, Color color)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ShockwaveRing";
            ring.transform.position = center + Vector3.up * 0.1f;
            ring.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
            Destroy(ring.GetComponent<Collider>());

            Material ringMat = new Material(Shader.Find("Unlit/Color"));
            ringMat.color = color;
            ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat;

            float dur = 0.4f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                if (ring != null)
                {
                    float currentR = Mathf.Lerp(0.5f, maxRadius * 2f, t);
                    ring.transform.localScale = new Vector3(currentR, 0.04f, currentR);
                }
                yield return null;
            }

            Destroy(ring);
        }
    }
}
