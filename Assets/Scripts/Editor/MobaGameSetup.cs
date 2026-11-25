#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Linq;
using MobaGame.Core;
using MobaGame.Player;
using MobaGame.Enemy;
using MobaGame.Combat;
using MobaGame.UI;

namespace MobaGame.EditorTools
{
    public class MobaGameSetup : MonoBehaviour
    {
        [MenuItem("MOBA Game/Generate Complete Game")]
        public static void BuildProject()
        {
            try
            {
                BuildProjectInternal();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("MOBA Setup Error", ex.GetType().Name + ": " + ex.Message, "OK");
            }
        }

        [MenuItem("MOBA Game/Build Windows (.exe)")]
        public static void BuildWindowsExe()
        {
            try
            {
                string mainMenuPath = "Assets/Scenes/MainMenu.unity";
                string battlePath = "Assets/Scenes/BattleArena.unity";
                if (!File.Exists(mainMenuPath) || !File.Exists(battlePath))
                {
                    BuildProjectInternal();
                }

                if (!Application.isBatchMode)
                {
                    EditorSceneManager.SaveOpenScenes();
                }

                PlayerSettings.productName = "Realm of Champions";
                PlayerSettings.companyName = "RealmOfChampions";
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;

                string buildDir = "Builds";
                if (!Directory.Exists(buildDir))
                    Directory.CreateDirectory(buildDir);

                string exePath = Path.Combine(buildDir, "RealmOfChampions.exe");

                BuildPlayerOptions opts = new BuildPlayerOptions
                {
                    scenes = new[] { mainMenuPath, battlePath },
                    locationPathName = exePath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    string fullOut = Path.GetFullPath(exePath);
                    Debug.Log($"[MOBA Build] BERHASIL! File .exe dibuat di: {fullOut} ({summary.totalSize / (1024f * 1024f):F1} MB)");
                    if (!Application.isBatchMode)
                    {
                        EditorUtility.DisplayDialog("Build .exe Berhasil!", $"Game berhasil dibuild ke:\n{fullOut}", "Buka Folder");
                        EditorUtility.RevealInFinder(fullOut);
                    }
                }
                else
                {
                    Debug.LogError($"[MOBA Build] GAGAL! Result: {summary.result}, Errors: {summary.totalErrors}");
                    if (!Application.isBatchMode)
                        EditorUtility.DisplayDialog("Build Gagal", $"Build gagal dengan {summary.totalErrors} error. Cek Console.", "OK");
                    else
                        EditorApplication.Exit(1);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                if (!Application.isBatchMode)
                    EditorUtility.DisplayDialog("Build Error", ex.GetType().Name + ": " + ex.Message, "OK");
                else
                    EditorApplication.Exit(1);
            }
        }

        private static void BuildProjectInternal()
        {
            Debug.Log("[MOBA Setup] Memulai pembuatan project game Mobile Legends 5v5 Map...");

            EnsureTags();
            EnsureDirectories();

            // 1. Configure Mixamo FBX Models & Animations
            ConfigureMixamoFBX("Assets/Downloads/Maria WProp J J Ong.fbx", false, true);
            ConfigureMixamoFBX("Assets/Downloads/Mutant.fbx", false, true);
            AssetDatabase.Refresh();

            Avatar mariaAvatar = AssetDatabase.LoadAllAssetsAtPath("Assets/Downloads/Maria WProp J J Ong.fbx")
                .OfType<Avatar>().FirstOrDefault();

            ConfigureMixamoFBX("Assets/Downloads/Idle.fbx", true, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Idle To Fight Idle.fbx", true, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Walking.fbx", true, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Run Forward.fbx", true, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Jump.fbx", false, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Sword Inward Slash.fbx", false, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Stable Sword Outward Slash.fbx", false, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Sword And Shield Impact.fbx", false, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Spell Casting.fbx", false, false, mariaAvatar);
            ConfigureMixamoFBX("Assets/Downloads/Death.fbx", false, false, mariaAvatar);

            AssetDatabase.Refresh();

            // 2. Build Animator Controllers
            RuntimeAnimatorController heroAnimCtrl = BuildAnimatorController("Assets/Animations/HeroAnimator.controller", false);
            RuntimeAnimatorController enemyAnimCtrl = BuildAnimatorController("Assets/Animations/EnemyAnimator.controller", true);

            // 3. Load Best Imported Asset Store PBR Textures (Albedo + Normal) with Procedural Fallback
            Texture2D grassTex = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Grass_AlbedoTransparency.png")
                ?? GenerateProceduralTexture("Assets/Textures/Tex_Grass.png", new Color(0.14f, 0.34f, 0.16f), new Color(0.24f, 0.48f, 0.22f), 16f, false);
            Texture2D grassNorm = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Grass_Normal.png");

            Texture2D laneTex = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Road_AlbedoTransparency.png")
                ?? GenerateProceduralTexture("Assets/Textures/Tex_Lane.png", new Color(0.42f, 0.38f, 0.32f), new Color(0.58f, 0.52f, 0.44f), 24f, true);
            Texture2D laneNorm = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Road_Normal.png");

            Texture2D waterTex = LoadBestTexture("Assets/Stylize Water Texture/Textures/Vol_36_5_Base_Color.png")
                ?? GenerateProceduralTexture("Assets/Textures/Tex_Water.png", new Color(0.06f, 0.38f, 0.65f), new Color(0.22f, 0.68f, 0.92f), 12f, false);
            Texture2D waterNorm = LoadBestTexture("Assets/Stylize Water Texture/Textures/Vol_36_5_Normal.png");

            Texture2D wallTex = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Rockwall_AlbedoTransparency.png")
                ?? GenerateProceduralTexture("Assets/Textures/Tex_Stone.png", new Color(0.26f, 0.28f, 0.32f), new Color(0.42f, 0.45f, 0.48f), 18f, true);
            Texture2D wallNorm = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Rockwall_Normal.png");

            Texture2D bridgeTex = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/RockSmooth_AlbedoTransparency.png") ?? wallTex;
            Texture2D bridgeNorm = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/RockSmooth_Normal.png") ?? wallNorm;

            Texture2D pitTex = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Sand_Pabbles_AlbedoTransparency.png") ?? laneTex;
            Texture2D pitNorm = LoadBestTexture("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Sand_Pabbles_Normal.png") ?? laneNorm;

            Material groundMat = CreateTexturedMaterial("Assets/Materials/Mat_Ground.mat", new Color(0.92f, 0.96f, 0.92f), grassTex, grassNorm, new Vector2(28, 28), 0.12f);
            Material laneMat = CreateTexturedMaterial("Assets/Materials/Mat_Lane.mat", Color.white, laneTex, laneNorm, new Vector2(3, 32), 0.22f);
            Material bridgeMat = CreateTexturedMaterial("Assets/Materials/Mat_Bridge.mat", new Color(0.9f, 0.9f, 0.92f), bridgeTex, bridgeNorm, new Vector2(3, 4), 0.32f);
            Material riverMat = CreateWaterMaterial("Assets/Materials/Mat_River.mat", new Color(0.75f, 0.92f, 1f, 0.95f), waterTex, waterNorm, new Vector2(3, 28));
            Material wallMat = CreateTexturedMaterial("Assets/Materials/Mat_Wall.mat", Color.white, wallTex, wallNorm, new Vector2(18, 2), 0.25f);
            Material bushMat = CreateTexturedMaterial("Assets/Materials/Mat_Bush.mat", new Color(0.55f, 0.85f, 0.45f), grassTex, grassNorm, new Vector2(2, 2), 0.1f);
            Material pitMat = CreateTexturedMaterial("Assets/Materials/Mat_Pit.mat", new Color(0.92f, 0.88f, 0.82f), pitTex, pitNorm, new Vector2(4, 4), 0.2f);
            Material heroMat = CreateColoredMaterial("Assets/Materials/Mat_Hero.mat", new Color(0.15f, 0.65f, 1f));
            Material heroAccentMat = CreateColoredMaterial("Assets/Materials/Mat_HeroAccent.mat", new Color(1.0f, 0.82f, 0.15f));
            Material enemyMat = CreateColoredMaterial("Assets/Materials/Mat_Enemy.mat", new Color(0.85f, 0.12f, 0.18f));
            Material enemyAccentMat = CreateColoredMaterial("Assets/Materials/Mat_EnemyAccent.mat", new Color(0.38f, 0.08f, 0.48f));
            Material playerTowerMat = CreateColoredMaterial("Assets/Materials/Mat_PlayerTower.mat", new Color(0.15f, 0.78f, 1f));
            Material enemyTowerMat = CreateColoredMaterial("Assets/Materials/Mat_EnemyTower.mat", new Color(1f, 0.2f, 0.28f));

            // 4. Generate Scenes
            string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            string battlePath = "Assets/Scenes/BattleArena.unity";

            BuildMainMenuScene(mainMenuPath, heroAnimCtrl, bridgeMat, playerTowerMat);
            BuildBattleArenaScene(
                battlePath, heroAnimCtrl, enemyAnimCtrl,
                groundMat, laneMat, bridgeMat, riverMat, wallMat, bushMat, pitMat,
                heroMat, heroAccentMat, enemyMat, enemyAccentMat, playerTowerMat, enemyTowerMat
            );

            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[2];
            buildScenes[0] = new EditorBuildSettingsScene(mainMenuPath, true);
            buildScenes[1] = new EditorBuildSettingsScene(battlePath, true);
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Always open MainMenu (Dashboard) as the starting scene
            EditorSceneManager.OpenScene(mainMenuPath);

            Debug.Log("[MOBA Setup] BERHASIL! Map 5v5 Mobile Legends HD, karakter 3D terpasang rapi, dan sistem HP telah diperbaiki.");
        }

        // =============================== TAGS ===============================
        private static void EnsureTags()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null) return;

            string[] neededTags = new string[] { "Enemy", "Wall", "Obstacle", "Bush" };
            foreach (string t in neededTags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(t))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = t;
                }
            }
            tagManager.ApplyModifiedProperties();
        }

        // =============================== DIRECTORIES ===============================
        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
            if (!Directory.Exists("Assets/Animations")) Directory.CreateDirectory("Assets/Animations");
            if (!Directory.Exists("Assets/Textures")) Directory.CreateDirectory("Assets/Textures");
        }

        // =============================== PROCEDURAL TEXTURES ===============================
        private static Texture2D GenerateProceduralTexture(string path, Color colorA, Color colorB, float scale, bool cobblestone)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size * scale;
                    float v = (float)y / size * scale;
                    float n = Mathf.PerlinNoise(u, v);

                    if (cobblestone)
                    {
                        float brickX = Mathf.Abs(Mathf.Sin(u * Mathf.PI));
                        float brickY = Mathf.Abs(Mathf.Sin(v * Mathf.PI));
                        float mortar = Mathf.Min(brickX, brickY);
                        n = n * 0.6f + (mortar > 0.12f ? 0.4f : 0.0f);
                    }

                    tex.SetPixel(x, y, Color.Lerp(colorA, colorB, n));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D LoadBestTexture(string path)
        {
            if (!File.Exists(path)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateTexturedMaterial(string path, Color tint, Texture2D tex, Texture2D normalTex, Vector2 tiling, float smoothness)
        {
            Material mat = CreateColoredMaterial(path, tint);
            mat.mainTexture = tex;
            mat.mainTextureScale = tiling;
            if (normalTex != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.SetTextureScale("_BumpMap", tiling);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateWaterMaterial(string path, Color tint, Texture2D tex, Texture2D normalTex, Vector2 tiling)
        {
            Material mat = CreateColoredMaterial(path, tint);
            mat.mainTexture = tex;
            mat.mainTextureScale = tiling;
            if (normalTex != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.SetTextureScale("_BumpMap", tiling);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.92f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // =============================== MIXAMO FBX CONFIG ===============================
        private static void ConfigureMixamoFBX(string assetPath, bool loopAnim, bool extractTextures = false, Avatar referenceAvatar = null)
        {
            if (!File.Exists(assetPath)) return;
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (extractTextures)
            {
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    changed = true;
                }
                string texFolder = "Assets/Downloads/Textures";
                if (!Directory.Exists(texFolder)) Directory.CreateDirectory(texFolder);
                importer.ExtractTextures(texFolder);
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                changed = true;
            }
            else
            {
                if (referenceAvatar != null &&
                    (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther || importer.sourceAvatar != referenceAvatar))
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = referenceAvatar;
                    changed = true;
                }
                if (importer.materialImportMode == ModelImporterMaterialImportMode.None)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    changed = true;
                }
            }

            bool isDeath = assetPath.Contains("Death");
            var clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = loopAnim;
                    clips[i].loopPose = loopAnim;
                    clips[i].lockRootRotation = true;
                    clips[i].lockRootHeightY = true;
                    clips[i].lockRootPositionXZ = true;
                    clips[i].keepOriginalOrientation = false;
                    clips[i].keepOriginalPositionY = isDeath;
                    clips[i].keepOriginalPositionXZ = false;
                    clips[i].heightFromFeet = !isDeath;

                    // Trim intro transition on "Idle To Fight Idle.fbx" so it loops only the settled fight stance
                    if (assetPath.Contains("Idle To Fight Idle"))
                    {
                        float len = clips[i].lastFrame - clips[i].firstFrame;
                        if (len > 15f)
                        {
                            clips[i].firstFrame = clips[i].firstFrame + len * 0.72f;
                        }
                    }
                }
                importer.clipAnimations = clips;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            // Explicit Mixamo Humanoid bone mapping for Mutant
            if (extractTextures && assetPath.Contains("Mutant"))
            {
                HumanDescription desc = importer.humanDescription;
                var map = new (string human, string bone)[]
                {
                    ("Hips", "mixamorig:Hips"),
                    ("Spine", "mixamorig:Spine"),
                    ("Chest", "mixamorig:Spine1"),
                    ("UpperChest", "mixamorig:Spine2"),
                    ("Neck", "mixamorig:Neck"),
                    ("Head", "mixamorig:Head"),
                    ("LeftShoulder", "mixamorig:LeftShoulder"),
                    ("LeftUpperArm", "mixamorig:LeftArm"),
                    ("LeftLowerArm", "mixamorig:LeftForeArm"),
                    ("LeftHand", "mixamorig:LeftHand"),
                    ("RightShoulder", "mixamorig:RightShoulder"),
                    ("RightUpperArm", "mixamorig:RightArm"),
                    ("RightLowerArm", "mixamorig:RightForeArm"),
                    ("RightHand", "mixamorig:RightHand"),
                    ("LeftUpperLeg", "mixamorig:LeftUpLeg"),
                    ("LeftLowerLeg", "mixamorig:LeftLeg"),
                    ("LeftFoot", "mixamorig:LeftFoot"),
                    ("LeftToes", "mixamorig:LeftToeBase"),
                    ("RightUpperLeg", "mixamorig:RightUpLeg"),
                    ("RightLowerLeg", "mixamorig:RightLeg"),
                    ("RightFoot", "mixamorig:RightFoot"),
                    ("RightToes", "mixamorig:RightToeBase")
                };

                HumanBone[] humanBones = new HumanBone[map.Length];
                for (int i = 0; i < map.Length; i++)
                {
                    humanBones[i] = new HumanBone
                    {
                        humanName = map[i].human,
                        boneName = map[i].bone,
                        limit = new HumanLimit { useDefaultValues = true }
                    };
                }
                desc.human = humanBones;
                importer.humanDescription = desc;
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip LoadClipFromFBX(string fbxPath)
        {
            if (!File.Exists(fbxPath)) return null;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object obj in assets)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        // =============================== ANIMATOR CONTROLLER ===============================
        private static RuntimeAnimatorController BuildAnimatorController(string controllerPath, bool isEnemy)
        {
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            var animParams = controller.parameters;
            for (int i = 0; i < animParams.Length; i++)
            {
                if (animParams[i].name == "IsGrounded")
                {
                    animParams[i].defaultBool = true;
                    break;
                }
            }
            controller.parameters = animParams;
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            if (!isEnemy)
            {
                controller.AddParameter("Skill1", AnimatorControllerParameterType.Trigger);
                controller.AddParameter("Skill2", AnimatorControllerParameterType.Trigger);
                controller.AddParameter("Ultimate", AnimatorControllerParameterType.Trigger);
            }
            else
            {
                controller.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
            }
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Respawn", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

            AnimationClip dedicatedIdleClip = LoadClipFromFBX("Assets/Downloads/Idle.fbx");
            AnimationClip idleClip = dedicatedIdleClip ?? LoadClipFromFBX("Assets/Downloads/Idle To Fight Idle.fbx");
            AnimationClip walkClip = LoadClipFromFBX("Assets/Downloads/Walking.fbx") ?? LoadClipFromFBX("Assets/Downloads/Run Forward.fbx");
            AnimationClip runClip = LoadClipFromFBX("Assets/Downloads/Run Forward.fbx");
            AnimationClip jumpClip = LoadClipFromFBX("Assets/Downloads/Jump.fbx") ?? idleClip;
            AnimationClip attackClip = !isEnemy
                ? (LoadClipFromFBX("Assets/Downloads/Stable Sword Outward Slash.fbx") ?? LoadClipFromFBX("Assets/Downloads/Sword Inward Slash.fbx"))
                : (LoadClipFromFBX("Assets/Downloads/Sword Inward Slash.fbx") ?? LoadClipFromFBX("Assets/Downloads/Stable Sword Outward Slash.fbx"));
            AnimationClip skill2Clip = LoadClipFromFBX("Assets/Downloads/Sword And Shield Impact.fbx");
            AnimationClip spellClip = LoadClipFromFBX("Assets/Downloads/Spell Casting.fbx");
            AnimationClip deathClip = LoadClipFromFBX("Assets/Downloads/Death.fbx");

            AnimatorState idleState = rootSM.AddState("Idle");
            idleState.motion = idleClip;
            if (dedicatedIdleClip == null) idleState.speed = 0.55f;
            rootSM.defaultState = idleState;

            AnimatorState walkState = rootSM.AddState("Walk");
            walkState.motion = walkClip;

            AnimatorState runState = rootSM.AddState("Run");
            runState.motion = runClip;

            AnimatorState jumpState = rootSM.AddState("Jump");
            jumpState.motion = jumpClip;

            AnimatorState attackState = rootSM.AddState("Attack");
            attackState.motion = attackClip;

            AnimatorState skillState = rootSM.AddState("SkillCast");
            skillState.motion = spellClip != null ? spellClip : attackClip;

            AnimatorState skill2State = !isEnemy ? rootSM.AddState("Skill2") : null;
            if (skill2State != null)
            {
                skill2State.motion = skill2Clip ?? (spellClip != null ? spellClip : attackClip);
            }

            AnimatorState deathState = rootSM.AddState("Death");
            deathState.motion = deathClip;

            // Idle <-> Walk <-> Run
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.12f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.12f;

            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.75f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.14f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.75f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.14f;

            // AnyState -> Jump
            var anyToJump = rootSM.AddAnyStateTransition(jumpState);
            anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            anyToJump.canTransitionToSelf = false;
            anyToJump.duration = 0.08f;

            var jumpToRun = jumpState.AddTransition(runState);
            jumpToRun.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToRun.AddCondition(AnimatorConditionMode.Greater, 0.75f, "Speed");
            jumpToRun.hasExitTime = false;
            jumpToRun.duration = 0.1f;

            var jumpToWalk = jumpState.AddTransition(walkState);
            jumpToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            jumpToWalk.hasExitTime = false;
            jumpToWalk.duration = 0.1f;

            var jumpToIdle = jumpState.AddTransition(idleState);
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.11f, "Speed");
            jumpToIdle.hasExitTime = false;
            jumpToIdle.duration = 0.12f;

            // AnyState -> Attack
            var anyToAtk = rootSM.AddAnyStateTransition(attackState);
            anyToAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            anyToAtk.canTransitionToSelf = false;
            anyToAtk.duration = 0.08f;
            AddCombatExitTransitions(attackState, runState, walkState, idleState);

            // AnyState -> Skill
            if (!isEnemy)
            {
                var t1 = rootSM.AddAnyStateTransition(skillState);
                t1.AddCondition(AnimatorConditionMode.If, 0, "Skill1");
                t1.canTransitionToSelf = false;
                t1.duration = 0.08f;

                var t2 = rootSM.AddAnyStateTransition(skill2State);
                t2.AddCondition(AnimatorConditionMode.If, 0, "Skill2");
                t2.canTransitionToSelf = false;
                t2.duration = 0.08f;
                AddCombatExitTransitions(skill2State, runState, walkState, idleState);

                var t3 = rootSM.AddAnyStateTransition(skillState);
                t3.AddCondition(AnimatorConditionMode.If, 0, "Ultimate");
                t3.canTransitionToSelf = false;
                t3.duration = 0.08f;
            }
            else
            {
                var ts = rootSM.AddAnyStateTransition(skillState);
                ts.AddCondition(AnimatorConditionMode.If, 0, "Skill");
                ts.canTransitionToSelf = false;
                ts.duration = 0.08f;
            }

            AddCombatExitTransitions(skillState, runState, walkState, idleState);

            // AnyState -> Death
            var anyToDie = rootSM.AddAnyStateTransition(deathState);
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            anyToDie.canTransitionToSelf = false;
            anyToDie.duration = 0.1f;

            var deathToIdle = deathState.AddTransition(idleState);
            deathToIdle.AddCondition(AnimatorConditionMode.If, 0, "Respawn");
            deathToIdle.hasExitTime = false;
            deathToIdle.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void AddCombatExitTransitions(AnimatorState fromState, AnimatorState runState, AnimatorState walkState, AnimatorState idleState)
        {
            var toRun = fromState.AddTransition(runState);
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.75f, "Speed");
            toRun.hasExitTime = true;
            toRun.exitTime = 0.85f;
            toRun.duration = 0.1f;

            var toWalk = fromState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toWalk.hasExitTime = true;
            toWalk.exitTime = 0.85f;
            toWalk.duration = 0.1f;

            var toIdle = fromState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.85f;
            toIdle.duration = 0.12f;
        }

        // =============================== MATERIAL HELPERS ===============================
        private static Material CreateColoredMaterial(string path, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader selectedShader = null;
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                {
                    selectedShader = Shader.Find("Universal Render Pipeline/Lit");
                }
                if (selectedShader == null) selectedShader = Shader.Find("Standard");
                if (selectedShader == null) selectedShader = Shader.Find("Unlit/Color");

                mat = new Material(selectedShader);
                mat.color = color;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null && mat.shader != null && mat.shader.name.Contains("Universal"))
                {
                    Shader std = Shader.Find("Standard");
                    if (std != null) mat.shader = std;
                }
                mat.color = color;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        // =============================== MAIN MENU SCENE (DASHBOARD) ===============================
        private static void BuildMainMenuScene(string scenePath, RuntimeAnimatorController heroAnimCtrl, Material stoneMat, Material glowMat)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.58f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.34f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.14f, 0.20f);

            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.05f, 0.10f);
            cam.fieldOfView = 42f;
            camObj.transform.position = new Vector3(0f, 1.35f, -4.4f);
            camObj.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            camObj.AddComponent<AudioListener>();

            // Lobby Studio Lighting
            GameObject keyLightObj = new GameObject("LobbyKeyLight");
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.5f;
            keyLight.color = new Color(1f, 0.95f, 0.88f);
            keyLight.shadows = LightShadows.Soft;
            keyLightObj.transform.rotation = Quaternion.Euler(32f, -25f, 0f);

            GameObject rimLightObj = new GameObject("LobbyRimLight");
            Light rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.color = new Color(0.2f, 0.72f, 1f);
            rimLight.intensity = 5f;
            rimLight.range = 8f;
            rimLightObj.transform.position = new Vector3(-1.8f, 2.2f, 1.4f);

            // 3D Lobby Pedestal & Live Hero Showcase
            GameObject lobbyStage = new GameObject("LobbyStage");
            GameObject pedGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedGlow.name = "PedestalGlow";
            pedGlow.transform.parent = lobbyStage.transform;
            pedGlow.transform.position = new Vector3(0f, -0.02f, 0f);
            pedGlow.transform.localScale = new Vector3(2.35f, 0.03f, 2.35f);
            pedGlow.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

            GameObject pedBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedBase.name = "PedestalStone";
            pedBase.transform.parent = lobbyStage.transform;
            pedBase.transform.position = new Vector3(0f, -0.10f, 0f);
            pedBase.transform.localScale = new Vector3(2.05f, 0.10f, 2.05f);
            pedBase.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

            GameObject mariaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Downloads/Maria WProp J J Ong.fbx");
            if (mariaPrefab != null)
            {
                GameObject lobbyHero = (GameObject)PrefabUtility.InstantiatePrefab(mariaPrefab);
                lobbyHero.name = "Lobby_Maria";
                lobbyHero.transform.parent = lobbyStage.transform;
                lobbyHero.transform.position = new Vector3(0f, 0.02f, 0f);
                lobbyHero.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
                lobbyHero.transform.localScale = Vector3.one * 1.15f;

                Animator anim = lobbyHero.GetComponent<Animator>();
                if (anim == null) anim = lobbyHero.AddComponent<Animator>();
                if (anim.avatar == null)
                {
                    anim.avatar = AssetDatabase.LoadAllAssetsAtPath("Assets/Downloads/Maria WProp J J Ong.fbx").OfType<Avatar>().FirstOrDefault();
                }
                anim.runtimeAnimatorController = heroAnimCtrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            GameObject soundObj = new GameObject("SoundManager");
            soundObj.AddComponent<SoundManager>();

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            MainMenuManager menuManager = canvasObj.AddComponent<MainMenuManager>();

            // Transparent Dashboard Root (lets 3D hero showcase shine in center)
            GameObject bgPanel = CreateUIPanel(canvasObj, "BGPanel", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Top Header Bar
            GameObject headerBanner = CreateUIPanel(bgPanel, "HeaderBanner", new Color(0.04f, 0.07f, 0.14f, 0.88f), new Vector2(0.5f, 0.91f), new Vector2(0.5f, 0.91f), new Vector2(1820, 106), Vector2.zero);
            AddGoldBorder(headerBanner, new Color(0.92f, 0.74f, 0.24f), 2.5f);

            GameObject titleObj = CreateUIText(headerBanner, "TitleText", "REALM OF CHAMPIONS", 46, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.3f));
            SetRectTransform(titleObj, new Vector2(0.32f, 0.64f), new Vector2(0.32f, 0.64f), new Vector2(1050, 54), Vector2.zero);

            GameObject subtitleObj = CreateUIText(headerBanner, "SubtitleText", "DAWN OF CELESTIA  •  5V5 MOBA BATTLEFIELD ARENA", 19, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.58f, 0.85f, 1f));
            SetRectTransform(subtitleObj, new Vector2(0.32f, 0.24f), new Vector2(0.32f, 0.24f), new Vector2(1050, 32), Vector2.zero);

            GameObject modeBadge = CreateUIPanel(headerBanner, "ModeBadge", new Color(0.08f, 0.24f, 0.42f, 0.95f), new Vector2(0.87f, 0.5f), new Vector2(0.87f, 0.5f), new Vector2(360, 56), Vector2.zero);
            AddGoldBorder(modeBadge, new Color(0.35f, 0.82f, 1f), 2f);
            CreateUIText(modeBadge, "ModeTxt", "MODE: 1V1 HERO DUEL", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.97f, 1f));

            // Left Dashboard Card: Player Hero & Skills
            GameObject leftCard = CreateUIPanel(bgPanel, "HeroCard", new Color(0.04f, 0.07f, 0.14f, 0.86f), new Vector2(0.16f, 0.49f), new Vector2(0.16f, 0.49f), new Vector2(460, 540), Vector2.zero);
            AddGoldBorder(leftCard, new Color(0.25f, 0.75f, 1f), 2.5f);

            GameObject heroHeader = CreateUIText(leftCard, "HeroHeader", "HERO TERPILIH", 16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.45f, 0.85f, 1f));
            SetRectTransform(heroHeader, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(400, 26), Vector2.zero);

            GameObject heroTitle = CreateUIText(leftCard, "HeroTitle", "MARIA", 38, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.3f));
            SetRectTransform(heroTitle, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(400, 44), Vector2.zero);

            GameObject heroRole = CreateUIText(leftCard, "HeroRole", "The Dawnblade  •  Fighter / Assassin", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.85f, 0.92f, 1f));
            SetRectTransform(heroRole, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(400, 28), Vector2.zero);

            Slider previewHp = CreateUISlider(leftCard, "PreviewHp", new Color(0.15f, 0.85f, 0.32f));
            previewHp.interactable = false;
            SetRectTransform(previewHp.gameObject, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(400, 24), Vector2.zero);
            CreateUIText(previewHp.gameObject, "Txt", "HP: 1000", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            Slider previewMana = CreateUISlider(leftCard, "PreviewMana", new Color(0.15f, 0.55f, 0.98f));
            previewMana.interactable = false;
            SetRectTransform(previewMana.gameObject, new Vector2(0.5f, 0.59f), new Vector2(0.5f, 0.59f), new Vector2(400, 22), Vector2.zero);
            CreateUIText(previewMana.gameObject, "Txt", "MANA: 400", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            GameObject skillBox = CreateUIPanel(leftCard, "SkillSummary", new Color(0.06f, 0.11f, 0.20f, 0.92f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(410, 230), Vector2.zero);
            AddGoldBorder(skillBox, new Color(0.25f, 0.55f, 0.85f, 0.7f), 1.5f);
            GameObject skillTxt = CreateUIText(skillBox, "SkillTxt",
                "KEMAMPUAN & SKILL:\n\n" +
                "[F / Klik]  Basic Slash (65 DMG)\n" +
                "[1]  Radiant Dash (140 DMG)\n" +
                "[2]  Solar Flare + Shield (210 DMG)\n" +
                "[3]  Celestial Judgement (450 DMG)",
                18, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.9f, 0.95f, 1f));
            SetRectPadding(skillTxt, 20, 16);

            // Right Dashboard Panel: Boss Target & Main Actions
            GameObject rightCard = CreateUIPanel(bgPanel, "CommandCard", new Color(0.04f, 0.07f, 0.14f, 0.86f), new Vector2(0.84f, 0.49f), new Vector2(0.84f, 0.49f), new Vector2(460, 540), Vector2.zero);
            AddGoldBorder(rightCard, new Color(0.92f, 0.74f, 0.24f), 2.5f);

            GameObject bossBox = CreateUIPanel(rightCard, "EnemyPreviewBox", new Color(0.20f, 0.06f, 0.09f, 0.92f), new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), new Vector2(400, 145), Vector2.zero);
            AddGoldBorder(bossBox, new Color(1f, 0.28f, 0.32f), 2f);
            GameObject bossTxt = CreateUIText(bossBox, "BossInfoTxt",
                "TARGET LAWAN:\n" +
                "MUTANT - The Void Reaver\n" +
                "HP Awal: 1000 (+600/Evolusi)\n" +
                "Misi: Hancurkan 6 Turret Void!",
                18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.88f));
            SetRectPadding(bossTxt, 14, 10);

            GameObject playBtnObj = CreateStylizedButton(rightCard, "PlayButton", "PLAY GAME", new Color(0.12f, 0.54f, 0.92f), new Color(1f, 0.85f, 0.2f), new Vector2(0, -15), new Vector2(390, 86), 30);
            GameObject aboutBtnObj = CreateStylizedButton(rightCard, "AboutButton", "ABOUT & STORY", new Color(0.14f, 0.54f, 0.36f), new Color(0.4f, 0.92f, 0.6f), new Vector2(0, -115), new Vector2(390, 68), 22);
            GameObject quitBtnObj = CreateStylizedButton(rightCard, "QuitButton", "QUIT GAME", new Color(0.68f, 0.16f, 0.22f), new Color(1f, 0.42f, 0.42f), new Vector2(0, -200), new Vector2(390, 64), 22);

            // Bottom Controls Footer
            GameObject footerBar = CreateUIPanel(bgPanel, "ControlsFooter", new Color(0.04f, 0.07f, 0.14f, 0.86f), new Vector2(0.5f, 0.065f), new Vector2(0.5f, 0.065f), new Vector2(1260, 56), Vector2.zero);
            AddGoldBorder(footerBar, new Color(0.35f, 0.65f, 0.95f, 0.85f), 1.5f);
            CreateUIText(footerBar, "FooterTxt", "KONTROL:  WASD Jalan   •   SHIFT Lari   •   SPASI Lompat   •   1 / 2 / 3 Skill   •   F / Klik Kiri Serang   •   Klik Kanan Kamera", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.88f, 0.94f, 1f));

            menuManager.mainPanel = bgPanel;
            menuManager.playButton = playBtnObj.GetComponent<Button>();
            menuManager.aboutButton = aboutBtnObj.GetComponent<Button>();
            menuManager.quitButton = quitBtnObj.GetComponent<Button>();

            // About Modal
            GameObject aboutPanel = CreateUIPanel(canvasObj, "AboutPanel", new Color(0.04f, 0.06f, 0.12f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1150, 760), Vector2.zero);
            AddGoldBorder(aboutPanel, new Color(0.9f, 0.72f, 0.2f), 3);
            menuManager.aboutPanel = aboutPanel;

            GameObject aboutTitle = CreateUIText(aboutPanel, "AboutHeader", "REALM OF CHAMPIONS  -  INFORMASI GAME", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.3f));
            SetRectTransform(aboutTitle, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(900, 60), Vector2.zero);

            GameObject tabStoryBtn = CreateStylizedButton(aboutPanel, "TabStoryBtn", "1. Latar Belakang Cerita", new Color(0.15f, 0.28f, 0.45f), new Color(0.5f, 0.75f, 1f), new Vector2(-330, 230), new Vector2(290, 52), 18);
            GameObject tabHowBtn = CreateStylizedButton(aboutPanel, "TabHowBtn", "2. Cara Bermain", new Color(0.15f, 0.28f, 0.45f), new Color(0.5f, 0.75f, 1f), new Vector2(0, 230), new Vector2(290, 52), 18);
            GameObject tabAssetBtn = CreateStylizedButton(aboutPanel, "TabAssetBtn", "3. Sumber Asset", new Color(0.15f, 0.28f, 0.45f), new Color(0.5f, 0.75f, 1f), new Vector2(330, 230), new Vector2(290, 52), 18);

            menuManager.tabStoryButton = tabStoryBtn.GetComponent<Button>();
            menuManager.tabHowToPlayButton = tabHowBtn.GetComponent<Button>();
            menuManager.tabAssetSourceButton = tabAssetBtn.GetComponent<Button>();

            GameObject storyContent = CreateUIPanel(aboutPanel, "StoryContent", new Color(0.08f, 0.11f, 0.18f, 0.95f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(1020, 360), Vector2.zero);
            string storyText = "LATAR BELAKANG CERITA (LORE & OBJECTIVE):\n\n" +
                "Di benua suci 'Land of Dawn', pasukan Void menancapkan 6 Menara Kegelapan (Turret) untuk membuka gerbang kehampaan.\n\n" +
                "Monster mutasi 'Mutant - The Void Reaver' bangkit menjaga menara-menara tersebut. Setiap kali Mutant tumbang, energi Void akan membangkitkannya kembali (Respawn) dengan wujud lebih kuat dan HP jauh lebih besar!\n\n" +
                "Tugas Maria - The Dawnblade: Gunakan cahaya suci untuk bertahan, bangkit kembali dari Fountain jika gugur, dan HANCURKAN SEMUA 6 TURRET MUSUH!";
            GameObject storyTxtObj = CreateUIText(storyContent, "StoryText", storyText, 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.92f, 0.95f, 1f));
            SetRectPadding(storyTxtObj, 36, 28);
            menuManager.storyTab = storyContent;

            GameObject howContent = CreateUIPanel(aboutPanel, "HowToPlayContent", new Color(0.08f, 0.11f, 0.18f, 0.95f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(1020, 360), Vector2.zero);
            string howText = "CARA BERMAIN (GAMEPLAY GUIDE):\n\n" +
                "• Jalan & Lari: WASD untuk Berjalan, tahan SHIFT + WASD untuk Berlari (Sprint).\n" +
                "• Lompat & Kamera: SPASI untuk Lompat, tahan Klik Kanan untuk putar kamera, Scroll untuk Zoom.\n" +
                "• Basic Attack: Klik Kiri Mouse, tekan [F], atau klik tombol Attack.\n" +
                "• Skill 1 [1] Radiant Dash: Menerjang lurus ke depan & menebas musuh (140 DMG).\n" +
                "• Skill 2 [2] Solar Flare: Ledakan cahaya area (210 DMG) + Shield pelindung.\n" +
                "• Ultimate [3] Celestial Judgement: Pedang langit raksasa (450 Burst DMG + Knockback!).";
            GameObject howTxtObj = CreateUIText(howContent, "HowText", howText, 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.9f, 1f, 0.9f));
            SetRectPadding(howTxtObj, 36, 28);
            menuManager.howToPlayTab = howContent;

            GameObject assetContent = CreateUIPanel(aboutPanel, "AssetContent", new Color(0.08f, 0.11f, 0.18f, 0.95f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(1020, 360), Vector2.zero);
            string assetText = "SUMBER ASSET & TEKNOLOGI:\n\n" +
                "1. Adobe Mixamo: Karakter 3D Humanoid 'Maria' & 'Mutant' beserta animasi mocap asli.\n" +
                "2. Unity Asset Store (Terrain & Grass Pack, Stylize Water Texture, PolyOne Stylized Rocks): Tekstur PBR tanah, jalan, air sungai, batu, dan tebing.\n" +
                "3. Procedural Audio Synthesizer: Efek suara disintesis langsung tanpa file audio eksternal.\n" +
                "4. Map 5v5 Land of Dawn: Arena 160m x 160m dengan 3 Lanes, Sungai Simetris, Jungle, Turret, dan Spawn Fountain.";
            GameObject assetTxtObj = CreateUIText(assetContent, "AssetText", assetText, 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(1f, 0.95f, 0.8f));
            SetRectPadding(assetTxtObj, 36, 28);
            menuManager.assetSourceTab = assetContent;

            GameObject closeAboutBtn = CreateStylizedButton(aboutPanel, "CloseAboutBtn", "KEMBALI KE MENU", new Color(0.75f, 0.22f, 0.22f), Color.white, new Vector2(0, -310), new Vector2(320, 56), 20);
            menuManager.closeAboutButton = closeAboutBtn.GetComponent<Button>();

            aboutPanel.SetActive(false);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // =============================== BATTLE ARENA SCENE ===============================
        private static void BuildBattleArenaScene(
            string scenePath, RuntimeAnimatorController heroAnimCtrl, RuntimeAnimatorController enemyAnimCtrl,
            Material groundMat, Material laneMat, Material bridgeMat, Material riverMat, Material wallMat, Material bushMat, Material pitMat,
            Material heroMat, Material heroAccentMat, Material enemyMat, Material enemyAccentMat, Material playerTowerMat, Material enemyTowerMat)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Graphics & Realistic Atmosphere
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 90f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.42f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.18f);

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.45f, 0.55f, 0.65f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 165f;

            // Lighting
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(48, -35, 0);

            // Managers
            GameObject soundObj = new GameObject("SoundManager");
            soundObj.AddComponent<SoundManager>();

            GameObject dtMgr = new GameObject("DamageTextManager");
            dtMgr.AddComponent<DamageTextManager>();

            GameObject vfxMgr = new GameObject("CombatVFXManager");
            vfxMgr.AddComponent<CombatVFXManager>();

            // Map Root
            GameObject mapRoot = new GameObject("LandOfDawn_5v5_Map");

            // ==================== GROUND (160m x 160m) ====================
            GameObject mainGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mainGround.name = "Main_Ground";
            mainGround.transform.parent = mapRoot.transform;
            mainGround.transform.position = Vector3.zero;
            mainGround.transform.localScale = new Vector3(16f, 1f, 16f);
            mainGround.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // ==================== BORDER WALLS ====================
            CreateWall(mapRoot, "Border_North", new Vector3(0, 4f, 80f), new Vector3(164f, 8f, 4f), wallMat);
            CreateWall(mapRoot, "Border_South", new Vector3(0, 4f, -80f), new Vector3(164f, 8f, 4f), wallMat);
            CreateWall(mapRoot, "Border_West", new Vector3(-80f, 4f, 0), new Vector3(4f, 8f, 164f), wallMat);
            CreateWall(mapRoot, "Border_East", new Vector3(80f, 4f, 0), new Vector3(4f, 8f, 164f), wallMat);

            // ==================== RIVER (Perpendicular to Mid Lane, Corner-to-Corner Symmetrical) ====================
            GameObject riverGroup = new GameObject("River_Group");
            riverGroup.transform.parent = mapRoot.transform;
            riverGroup.transform.position = Vector3.zero;
            riverGroup.transform.rotation = Quaternion.Euler(0, -45f, 0);

            GameObject river = CreateFlatVisualSurface(riverGroup, "The_River", new Vector3(0, 0.018f, 0), new Vector3(12f, 0.02f, 212f), riverMat);
            river.transform.localRotation = Quaternion.identity;
            river.AddComponent<WaterFlow>();

            // River Banks (Parallel to river)
            GameObject bankL = CreateFlatVisualSurface(riverGroup, "RiverBank_Left", Vector3.zero, new Vector3(1.4f, 0.025f, 212f), pitMat);
            bankL.transform.localPosition = new Vector3(-6.5f, 0.022f, 0);
            bankL.transform.localRotation = Quaternion.identity;
            GameObject bankR = CreateFlatVisualSurface(riverGroup, "RiverBank_Right", Vector3.zero, new Vector3(1.4f, 0.025f, 212f), pitMat);
            bankR.transform.localPosition = new Vector3(6.5f, 0.022f, 0);
            bankR.transform.localRotation = Quaternion.identity;

            // ==================== 3 LANES ====================
            // Mid Lane (Diagonal from player base SW to enemy base NE)
            GameObject midLane = CreateFlatVisualSurface(mapRoot, "Lane_Mid", new Vector3(0, 0.03f, 0), new Vector3(11f, 0.02f, 190f), laneMat);
            midLane.transform.rotation = Quaternion.Euler(0, 45f, 0);

            // Top Lane (L-shaped: left side going up + top side going right)
            CreateFlatVisualSurface(mapRoot, "Lane_Top_Part1", new Vector3(-62f, 0.03f, 0), new Vector3(10f, 0.02f, 134f), laneMat);
            CreateFlatVisualSurface(mapRoot, "Lane_Top_Part2", new Vector3(0, 0.03f, 62f), new Vector3(134f, 0.02f, 10f), laneMat);

            // Bot Lane (L-shaped: bottom side going right + right side going up)
            CreateFlatVisualSurface(mapRoot, "Lane_Bot_Part1", new Vector3(0, 0.03f, -62f), new Vector3(134f, 0.02f, 10f), laneMat);
            CreateFlatVisualSurface(mapRoot, "Lane_Bot_Part2", new Vector3(62f, 0.03f, 0), new Vector3(10f, 0.02f, 134f), laneMat);

            // ==================== BRIDGES (3 Bridges Perpendicular to River) ====================
            CreateBridge(mapRoot, "Bridge_Mid", Vector3.zero, 12f, 16f, 45f, bridgeMat, wallMat);
            CreateBridge(mapRoot, "Bridge_Top", new Vector3(-62f, 0, 62f), 14f, 16f, 45f, bridgeMat, wallMat);
            CreateBridge(mapRoot, "Bridge_Bot", new Vector3(62f, 0, -62f), 14f, 16f, 45f, bridgeMat, wallMat);

            // ==================== BASE PLATFORMS & SPAWN FOUNTAINS ====================
            CreateBasePlatform(mapRoot, "Player_Base_Platform", new Vector3(-62f, 0.04f, -62f), new Vector3(28f, 0.05f, 28f), playerTowerMat);
            CreateBasePlatform(mapRoot, "Enemy_Base_Platform", new Vector3(62f, 0.04f, 62f), new Vector3(28f, 0.05f, 28f), enemyTowerMat);

            CreateSpawnFountain(mapRoot, "Player_Spawn_Fountain", new Vector3(-71f, 0, -71f), "Player", playerTowerMat);
            CreateSpawnFountain(mapRoot, "Enemy_Spawn_Fountain", new Vector3(71f, 0, 71f), "Enemy", enemyTowerMat);

            // ==================== BOSS PITS (Symmetrical beside the river) ====================
            CreateBossPit(mapRoot, "Lord_Pit", new Vector3(-18f, 0f, 40f), pitMat, wallMat);
            CreateBossPit(mapRoot, "Turtle_Pit", new Vector3(18f, 0f, -40f), pitMat, wallMat);

            // ==================== JUNGLE TREES ====================
            Material trunkMat = CreateColoredMaterial("Assets/Materials/Mat_TreeTrunk.mat", new Color(0.38f, 0.24f, 0.14f));
            Material leafMat = CreateColoredMaterial("Assets/Materials/Mat_TreeLeaf.mat", new Color(0.12f, 0.48f, 0.16f));

            // Player-side jungle
            CreateTree(mapRoot, new Vector3(-34f, 0, -14f), trunkMat, leafMat, 1.4f);
            CreateTree(mapRoot, new Vector3(-44f, 0, -6f), trunkMat, leafMat, 1.6f);
            CreateTree(mapRoot, new Vector3(-42f, 0, 18f), trunkMat, leafMat, 1.5f);
            CreateTree(mapRoot, new Vector3(-14f, 0, -34f), trunkMat, leafMat, 1.4f);
            CreateTree(mapRoot, new Vector3(-6f, 0, -44f), trunkMat, leafMat, 1.6f);
            CreateTree(mapRoot, new Vector3(18f, 0, -42f), trunkMat, leafMat, 1.5f);
            // Enemy-side jungle
            CreateTree(mapRoot, new Vector3(14f, 0, 34f), trunkMat, leafMat, 1.4f);
            CreateTree(mapRoot, new Vector3(6f, 0, 44f), trunkMat, leafMat, 1.6f);
            CreateTree(mapRoot, new Vector3(-18f, 0, 42f), trunkMat, leafMat, 1.5f);
            CreateTree(mapRoot, new Vector3(34f, 0, 14f), trunkMat, leafMat, 1.4f);
            CreateTree(mapRoot, new Vector3(44f, 0, 6f), trunkMat, leafMat, 1.6f);
            CreateTree(mapRoot, new Vector3(42f, 0, -18f), trunkMat, leafMat, 1.5f);

            // ==================== ORGANIC BUSHES ====================
            CreateBush(mapRoot, "Bush_River_Top", new Vector3(-12f, 0f, 12f), bushMat);
            CreateBush(mapRoot, "Bush_River_Bot", new Vector3(12f, 0f, -12f), bushMat);
            CreateBush(mapRoot, "Bush_Mid_Lane_1", new Vector3(-16f, 0f, -6f), bushMat);
            CreateBush(mapRoot, "Bush_Mid_Lane_2", new Vector3(6f, 0f, 16f), bushMat);
            CreateBush(mapRoot, "Bush_Top_Lane", new Vector3(-62f, 0f, 28f), bushMat);
            CreateBush(mapRoot, "Bush_Bot_Lane", new Vector3(28f, 0f, -62f), bushMat);

            // ==================== 3D STYLIZED ROCKS (PolyOne Asset Store) ====================
            Texture2D rockColorTex = LoadBestTexture("Assets/PolyOne/Rocks Stylized/Texture/Rocks_Stylized_Color.png");
            CreateStylizedRock(mapRoot, "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_01.prefab", new Vector3(-30f, 0, 40f), 1.8f, 15f, rockColorTex);
            CreateStylizedRock(mapRoot, "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_02.prefab", new Vector3(30f, 0, -40f), 1.8f, 195f, rockColorTex);
            CreateStylizedRock(mapRoot, "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_03.prefab", new Vector3(-48f, 0, 32f), 1.5f, 60f, rockColorTex);
            CreateStylizedRock(mapRoot, "Assets/PolyOne/Rocks Stylized/Prefabs/SM_Rocks_04.prefab", new Vector3(48f, 0, -32f), 1.5f, 240f, rockColorTex);

            // ==================== TURRETS & CRYSTAL CORES ====================
            GameObject playerCore = CreateTurret(mapRoot, "Player_Crystal_Core", new Vector3(-60f, 0, -60f), "Enemy", playerTowerMat, 2200f, 75f, 12f, 3.8f);
            GameObject enemyCore = CreateTurret(mapRoot, "Enemy_Crystal_Core", new Vector3(60f, 0, 60f), "Player", enemyTowerMat, 2200f, 75f, 12f, 3.8f);

            // Mid lane turrets
            CreateTurret(mapRoot, "Player_Mid_T1", new Vector3(-20f, 0, -20f), "Enemy", playerTowerMat, 1000f, 40f, 9f, 2.4f);
            CreateTurret(mapRoot, "Player_Mid_T2", new Vector3(-34f, 0, -34f), "Enemy", playerTowerMat, 1300f, 50f, 9f, 2.4f);
            CreateTurret(mapRoot, "Player_Mid_T3", new Vector3(-48f, 0, -48f), "Enemy", playerTowerMat, 1600f, 60f, 9f, 2.4f);

            CreateTurret(mapRoot, "Enemy_Mid_T1", new Vector3(20f, 0, 20f), "Player", enemyTowerMat, 1000f, 40f, 9f, 2.4f);
            CreateTurret(mapRoot, "Enemy_Mid_T2", new Vector3(34f, 0, 34f), "Player", enemyTowerMat, 1300f, 50f, 9f, 2.4f);
            CreateTurret(mapRoot, "Enemy_Mid_T3", new Vector3(48f, 0, 48f), "Player", enemyTowerMat, 1600f, 60f, 9f, 2.4f);

            // Top/Bot lane turrets
            CreateTurret(mapRoot, "Player_Top_T1", new Vector3(-62f, 0, 14f), "Enemy", playerTowerMat, 1000f, 40f, 9f, 2.4f);
            CreateTurret(mapRoot, "Enemy_Top_T1", new Vector3(-14f, 0, 62f), "Player", enemyTowerMat, 1000f, 40f, 9f, 2.4f);
            CreateTurret(mapRoot, "Player_Bot_T1", new Vector3(14f, 0, -62f), "Enemy", playerTowerMat, 1000f, 40f, 9f, 2.4f);
            CreateTurret(mapRoot, "Enemy_Bot_T1", new Vector3(62f, 0, -14f), "Player", enemyTowerMat, 1000f, 40f, 9f, 2.4f);

            // ==================== CHARACTERS (1000 HP vs 1000 HP) ====================
            Vector3 playerSpawnPos = new Vector3(-65f, 0.15f, -69f);
            GameObject playerObj = CreateMixamoHero(
                "Player", playerSpawnPos, "Player",
                "Assets/Downloads/Maria WProp J J Ong.fbx", heroAnimCtrl,
                1000f, true, 1.35f, heroMat, heroAccentMat
            );
            playerObj.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            PlayerController playerCtrl = playerObj.AddComponent<PlayerController>();
            HealthSystem playerHp = playerObj.GetComponent<HealthSystem>();
            playerCtrl.healthSystem = playerHp;
            playerCtrl.rb = playerObj.GetComponent<Rigidbody>();
            playerCtrl.visualModel = playerObj.transform.Find("Visual");
            playerCtrl.animator = playerObj.GetComponentInChildren<Animator>();

            Vector3 enemySpawnPos = new Vector3(68f, 0.15f, 68f);
            GameObject enemyObj = CreateMixamoHero(
                "Enemy_Malakar", enemySpawnPos, "Enemy",
                "Assets/Downloads/Mutant.fbx", enemyAnimCtrl,
                1000f, false, 1.4f, enemyMat, enemyAccentMat
            );
            EnemyAI enemyAI = enemyObj.AddComponent<EnemyAI>();
            HealthSystem enemyHp = enemyObj.GetComponent<HealthSystem>();
            enemyAI.healthSystem = enemyHp;
            enemyAI.rb = enemyObj.GetComponent<Rigidbody>();
            enemyAI.visualModel = enemyObj.transform.Find("Visual");
            enemyAI.animator = enemyObj.GetComponentInChildren<Animator>();
            enemyAI.playerTarget = playerObj.transform;

            GameObject enemyBaseWp = new GameObject("EnemyBaseWaypoint");
            enemyBaseWp.transform.position = new Vector3(65f, 0, 65f);
            GameObject centerWp = new GameObject("CenterWaypoint");
            centerWp.transform.position = Vector3.zero;

            enemyAI.baseSpawnPoint = enemyBaseWp.transform;
            enemyAI.centerPoint = centerWp.transform;

            // Camera Setup (Third Person - Behind Character)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 60;
            cam.allowMSAA = true;
            camObj.AddComponent<AudioListener>();
            CameraFollow camFollow = camObj.AddComponent<CameraFollow>();
            camFollow.target = playerObj.transform;
            camFollow.distance = 11f;
            camFollow.heightOffset = 1.8f;
            Quaternion initCamRot = Quaternion.Euler(32f, 45f, 0f);
            camObj.transform.position = playerSpawnPos + Vector3.up * 1.8f + initCamRot * (Vector3.back * 11f);
            camObj.transform.rotation = initCamRot;
            camObj.AddComponent<CameraShake>();

            // EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            // HUD
            BuildBattleHUD(playerCtrl, enemyAI, playerHp, enemyHp);

            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.playerHealth = playerHp;
            gm.enemyHealth = enemyHp;
            gm.playerCore = playerCore.GetComponent<HealthSystem>();
            gm.enemyCore = enemyCore.GetComponent<HealthSystem>();
            gm.playerController = playerCtrl;
            gm.enemyAI = enemyAI;
            gm.totalEnemyTurrets = 6;
            gm.playerSpawnPosition = playerSpawnPos;
            gm.enemySpawnPosition = enemySpawnPos;

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // =============================== MIXAMO HERO CREATION ===============================
        private static GameObject CreateMixamoHero(
            string name, Vector3 pos, string tag,
            string fbxPath, RuntimeAnimatorController animCtrl,
            float maxHp, bool isHero, float scaleFactor, Material fallbackBodyMat, Material fallbackAccentMat)
        {
            GameObject hero = new GameObject(name);
            hero.tag = tag;
            hero.transform.position = pos;

            Rigidbody rb = hero.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            CapsuleCollider col = hero.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0, 0.9f, 0);

            HealthSystem hp = hero.AddComponent<HealthSystem>();
            hp.maxHealth = maxHp;
            hp.currentHealth = maxHp;

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            GameObject visual;
            if (modelPrefab != null)
            {
                visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                visual.name = "Visual";
                visual.transform.parent = hero.transform;
                visual.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * scaleFactor;

                Animator anim = visual.GetComponent<Animator>();
                if (anim == null) anim = visual.AddComponent<Animator>();
                if (anim.avatar == null)
                {
                    anim.avatar = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
                }
                anim.runtimeAnimatorController = animCtrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else
            {
                // Fallback if FBX missing
                visual = new GameObject("Visual");
                visual.transform.parent = hero.transform;
                visual.transform.localPosition = Vector3.zero;
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.parent = visual.transform;
                body.transform.localPosition = new Vector3(0, 1.1f, 0);
                body.GetComponent<MeshRenderer>().sharedMaterial = fallbackBodyMat;
                Object.DestroyImmediate(body.GetComponent<Collider>());
            }

            CreateOverheadSliderBar(hero, isHero ? new Color(0.15f, 0.85f, 0.25f) : new Color(0.9f, 0.15f, 0.2f));
            return hero;
        }

        // =============================== OVERHEAD HP BAR ===============================
        private static void CreateOverheadSliderBar(GameObject parent, Color fillColor, float yOffset = 2.8f)
        {
            GameObject canvasObj = new GameObject("HealthBarCanvas");
            canvasObj.transform.SetParent(parent.transform, false);
            canvasObj.transform.localPosition = new Vector3(0, yOffset, 0);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 14);

            canvasObj.AddComponent<Billboard>();

            Slider slider = CreateUISlider(canvasObj, "HpSlider", fillColor);
            RectTransform sliderRt = slider.GetComponent<RectTransform>();
            sliderRt.anchorMin = Vector2.zero;
            sliderRt.anchorMax = Vector2.one;
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;
            sliderRt.localScale = Vector3.one;
            sliderRt.localPosition = Vector3.zero;

            // Scale canvas AFTER children are created
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            foreach (var img in canvasObj.GetComponentsInChildren<Image>())
            {
                img.raycastTarget = false;
            }

            HealthSystem hp = parent.GetComponent<HealthSystem>();
            if (hp != null)
            {
                slider.maxValue = hp.maxHealth;
                slider.value = hp.currentHealth;

                hp.OnHealthChanged += (current, max) =>
                {
                    if (slider != null)
                    {
                        slider.maxValue = max;
                        slider.value = current;
                    }
                };
            }
        }

        // =============================== BATTLE HUD ===============================
        private static void BuildBattleHUD(PlayerController player, EnemyAI enemy, HealthSystem playerHp, HealthSystem enemyHp)
        {
            GameObject canvasObj = new GameObject("HUDCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            BattleUIManager ui = canvasObj.AddComponent<BattleUIManager>();
            ui.player = player;
            ui.enemy = enemy;

            // Top-Left Controls Mini-Guide
            GameObject controlsGuide = CreateUIPanel(canvasObj, "ControlsGuidePanel", new Color(0.04f, 0.07f, 0.13f, 0.86f), new Vector2(0.14f, 0.90f), new Vector2(0.14f, 0.90f), new Vector2(410, 118), Vector2.zero);
            AddGoldBorder(controlsGuide, new Color(0.35f, 0.65f, 0.95f, 0.8f), 1.5f);
            GameObject guideTxt = CreateUIText(controlsGuide, "GuideText",
                "KONTROL:\nWASD: Jalan  |  SHIFT: Lari\nSPASI: Lompat  |  1-3: Skill\nKlik Kiri / F: Serang  |  Klik Kanan: Kamera",
                16, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.88f, 0.94f, 1f));
            SetRectPadding(guideTxt, 16, 8);

            // Top Bar (Turret Objective, Match Timer, Scoreboard)
            GameObject topBar = CreateUIPanel(canvasObj, "TopBar", new Color(0.04f, 0.07f, 0.13f, 0.95f), new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), new Vector2(740, 76), Vector2.zero);
            AddGoldBorder(topBar, new Color(0.92f, 0.74f, 0.24f), 2.5f);

            GameObject objTextObj = CreateUIText(topBar, "ObjectiveText", "TURRET: 0 / 6", 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.45f, 0.92f, 1f));
            SetRectTransform(objTextObj, new Vector2(0.18f, 0.5f), new Vector2(0.18f, 0.5f), new Vector2(210, 44), Vector2.zero);
            ui.objectiveText = objTextObj.GetComponent<Text>();

            GameObject timerTextObj = CreateUIText(topBar, "TimerText", "00:00", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetRectTransform(timerTextObj, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(180, 36), Vector2.zero);
            ui.timerText = timerTextObj.GetComponent<Text>();

            GameObject scoreTextObj = CreateUIText(topBar, "ScoreText", "0  VS  0", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f));
            SetRectTransform(scoreTextObj, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), new Vector2(200, 30), Vector2.zero);
            ui.scoreText = scoreTextObj.GetComponent<Text>();

            GameObject pauseBtn = CreateStylizedButton(topBar, "PauseBtn", "||", new Color(0.18f, 0.24f, 0.36f), new Color(0.92f, 0.74f, 0.24f), new Vector2(325, 0), new Vector2(52, 52), 20);
            ui.topPauseBtn = pauseBtn.GetComponent<Button>();

            // Enemy Boss HP Bar
            GameObject bossBarObj = CreateUIPanel(canvasObj, "BossBarPanel", new Color(0.06f, 0.08f, 0.12f, 0.95f), new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), new Vector2(680, 48), Vector2.zero);
            AddGoldBorder(bossBarObj, new Color(0.95f, 0.25f, 0.28f), 2.5f);
            Slider enemySlider = CreateUISlider(bossBarObj, "EnemySlider", new Color(0.9f, 0.16f, 0.22f));
            SetRectTransform(enemySlider.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(656, 30), Vector2.zero);
            ui.enemyHpSlider = enemySlider;
            GameObject enemyHpTextObj = CreateUIText(bossBarObj, "EnemyHpText", "MUTANT LV.1  •  1000 / 1000", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            ui.enemyHpText = enemyHpTextObj.GetComponent<Text>();

            // Storyline Chapter Banner
            GameObject storyBanner = CreateUIPanel(canvasObj, "StoryBannerPanel", new Color(0.04f, 0.07f, 0.15f, 0.88f), new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), new Vector2(780, 76), Vector2.zero);
            AddGoldBorder(storyBanner, new Color(1f, 0.82f, 0.28f), 2f);
            GameObject storyTitle = CreateUIText(storyBanner, "StoryTitle", "BABAK 1: INVASI VOID", 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.3f));
            SetRectTransform(storyTitle, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(740, 28), Vector2.zero);
            GameObject storyBody = CreateUIText(storyBanner, "StoryBody", "Hancurkan seluruh 6 Turret Void untuk mengakhiri invasi Mutant!", 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.9f, 0.96f, 1f));
            SetRectTransform(storyBody, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(740, 32), Vector2.zero);
            ui.storyBannerPanel = storyBanner;
            ui.storyTitleText = storyTitle.GetComponent<Text>();
            ui.storyBodyText = storyBody.GetComponent<Text>();

            // Player Respawn Overlay
            GameObject respawnPanel = CreateUIPanel(canvasObj, "RespawnPanel", new Color(0.06f, 0.04f, 0.10f, 0.90f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(580, 210), Vector2.zero);
            AddGoldBorder(respawnPanel, new Color(0.35f, 0.85f, 1f), 2.5f);
            GameObject respTitle = CreateUIText(respawnPanel, "RespawnTitle", "MARIA GUGUR", 38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.38f));
            SetRectTransform(respTitle, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(520, 48), Vector2.zero);
            GameObject respCountdown = CreateUIText(respawnPanel, "RespawnCountdown", "RESPAWN DI FOUNTAIN DALAM 5...", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.6f, 0.95f, 1f));
            SetRectTransform(respCountdown, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(520, 38), Vector2.zero);
            GameObject respTip = CreateUIText(respawnPanel, "RespawnTip", "Waspada: Mutant akan terus berevolusi lebih kuat!", 16, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.7f));
            SetRectTransform(respTip, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(520, 30), Vector2.zero);
            ui.respawnPanel = respawnPanel;
            ui.respawnCountdownText = respCountdown.GetComponent<Text>();
            respawnPanel.SetActive(false);

            // Player HUD (Bottom Left - Sleek Hero Status Card)
            GameObject playerHudPanel = CreateUIPanel(canvasObj, "PlayerHUDPanel", new Color(0.04f, 0.07f, 0.14f, 0.96f), new Vector2(0.18f, 0.13f), new Vector2(0.18f, 0.13f), new Vector2(500, 158), Vector2.zero);
            AddGoldBorder(playerHudPanel, new Color(0.92f, 0.74f, 0.24f), 2.5f);

            GameObject heroBadge = CreateUIPanel(playerHudPanel, "HeroBadge", new Color(0.1f, 0.35f, 0.65f, 1f), new Vector2(0.13f, 0.5f), new Vector2(0.13f, 0.5f), new Vector2(96, 114), Vector2.zero);
            AddGoldBorder(heroBadge, new Color(0.4f, 0.85f, 1f), 2f);
            CreateUIText(heroBadge, "BadgeTxt", "M\nHERO", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.45f));

            GameObject heroName = CreateUIText(playerHudPanel, "HeroName", "MARIA  •  The Dawnblade", 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.32f));
            SetRectTransform(heroName, new Vector2(0.60f, 0.80f), new Vector2(0.60f, 0.80f), new Vector2(340, 34), Vector2.zero);

            Slider hpSlider = CreateUISlider(playerHudPanel, "PlayerHpSlider", new Color(0.15f, 0.85f, 0.32f));
            SetRectTransform(hpSlider.gameObject, new Vector2(0.60f, 0.50f), new Vector2(0.60f, 0.50f), new Vector2(340, 30), Vector2.zero);
            ui.playerHpSlider = hpSlider;
            GameObject playerHpTxt = CreateUIText(hpSlider.gameObject, "HpText", "1000 / 1000", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            ui.playerHpText = playerHpTxt.GetComponent<Text>();

            Slider manaSlider = CreateUISlider(playerHudPanel, "PlayerManaSlider", new Color(0.15f, 0.55f, 0.98f));
            SetRectTransform(manaSlider.gameObject, new Vector2(0.60f, 0.22f), new Vector2(0.60f, 0.22f), new Vector2(340, 24), Vector2.zero);
            ui.playerManaSlider = manaSlider;
            GameObject playerManaTxt = CreateUIText(manaSlider.gameObject, "ManaText", "400 / 400", 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            ui.playerManaText = playerManaTxt.GetComponent<Text>();

            // Action Buttons
            GameObject actionPanel = CreateUIPanel(canvasObj, "ActionButtonsPanel", new Color(0, 0, 0, 0), new Vector2(0.84f, 0.18f), new Vector2(0.84f, 0.18f), new Vector2(420, 320), Vector2.zero);

            GameObject atkBtn = CreateStylizedButton(actionPanel, "AttackBtn", "ATTACK\n[F / Klik]", new Color(0.85f, 0.6f, 0.08f), new Color(1f, 0.9f, 0.4f), new Vector2(95, -45), new Vector2(140, 140), 21);
            ui.attackButton = atkBtn.GetComponent<Button>();

            GameObject s1Btn = CreateStylizedButton(actionPanel, "Skill1Btn", "DASH\n[1]", new Color(0.15f, 0.5f, 0.85f), new Color(0.5f, 0.85f, 1f), new Vector2(-65, -65), new Vector2(100, 100), 18);
            ui.skill1Button = s1Btn.GetComponent<Button>();
            CreateCooldownOverlay(s1Btn, out ui.skill1CooldownOverlay, out ui.skill1CooldownText);

            GameObject s2Btn = CreateStylizedButton(actionPanel, "Skill2Btn", "SHIELD\n[2]", new Color(0.2f, 0.65f, 0.4f), new Color(0.5f, 1f, 0.7f), new Vector2(-75, 50), new Vector2(100, 100), 18);
            ui.skill2Button = s2Btn.GetComponent<Button>();
            CreateCooldownOverlay(s2Btn, out ui.skill2CooldownOverlay, out ui.skill2CooldownText);

            GameObject ultBtn = CreateStylizedButton(actionPanel, "UltBtn", "ULTIMATE\n[3]", new Color(0.85f, 0.18f, 0.22f), new Color(1f, 0.85f, 0.3f), new Vector2(30, 100), new Vector2(115, 115), 19);
            ui.ultButton = ultBtn.GetComponent<Button>();
            CreateCooldownOverlay(ultBtn, out ui.ultCooldownOverlay, out ui.ultCooldownText);

            // Victory Panel
            GameObject victoryPanel = CreateUIPanel(canvasObj, "VictoryPanel", new Color(0.04f, 0.14f, 0.08f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760, 480), Vector2.zero);
            AddGoldBorder(victoryPanel, new Color(1f, 0.85f, 0.2f), 3);
            GameObject vicTitle = CreateUIText(victoryPanel, "VicTitle", "VICTORY!", 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
            SetRectTransform(vicTitle, new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), new Vector2(700, 80), Vector2.zero);
            GameObject vicDesc = CreateUIText(victoryPanel, "VicDesc", "Seluruh Turret Void telah hancur & Land of Dawn terselamatkan!", 22, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            SetRectTransform(vicDesc, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(680, 100), Vector2.zero);
            ui.victoryDescText = vicDesc.GetComponent<Text>();
            GameObject vicRestart = CreateStylizedButton(victoryPanel, "VicRestart", "MAIN LAGI", new Color(0.15f, 0.6f, 0.3f), Color.white, new Vector2(0, -45), new Vector2(320, 62), 24);
            GameObject vicMenu = CreateStylizedButton(victoryPanel, "VicMenu", "MENU UTAMA", new Color(0.2f, 0.45f, 0.75f), Color.white, new Vector2(0, -130), new Vector2(320, 62), 24);
            ui.victoryPanel = victoryPanel;
            ui.victoryRestartBtn = vicRestart.GetComponent<Button>();
            ui.victoryMenuBtn = vicMenu.GetComponent<Button>();
            victoryPanel.SetActive(false);

            // Defeat Panel
            GameObject defeatPanel = CreateUIPanel(canvasObj, "DefeatPanel", new Color(0.18f, 0.04f, 0.05f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760, 480), Vector2.zero);
            AddGoldBorder(defeatPanel, new Color(1f, 0.25f, 0.25f), 3);
            GameObject defTitle = CreateUIText(defeatPanel, "DefTitle", "DEFEAT", 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.25f, 0.25f));
            SetRectTransform(defTitle, new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), new Vector2(700, 80), Vector2.zero);
            GameObject defDesc = CreateUIText(defeatPanel, "DefDesc", "Kegelapan menelan Land of Dawn... Bangkit dan coba lagi!", 24, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            SetRectTransform(defDesc, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(680, 60), Vector2.zero);
            GameObject defRestart = CreateStylizedButton(defeatPanel, "DefRestart", "COBA LAGI", new Color(0.8f, 0.2f, 0.25f), Color.white, new Vector2(0, -45), new Vector2(320, 62), 24);
            GameObject defMenu = CreateStylizedButton(defeatPanel, "DefMenu", "MENU UTAMA", new Color(0.2f, 0.45f, 0.75f), Color.white, new Vector2(0, -130), new Vector2(320, 62), 24);
            ui.defeatPanel = defeatPanel;
            ui.defeatRestartBtn = defRestart.GetComponent<Button>();
            ui.defeatMenuBtn = defMenu.GetComponent<Button>();
            defeatPanel.SetActive(false);

            // Pause Panel
            GameObject pausePanel = CreateUIPanel(canvasObj, "PausePanel", new Color(0.05f, 0.08f, 0.14f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620, 440), Vector2.zero);
            AddGoldBorder(pausePanel, new Color(0.85f, 0.68f, 0.2f), 3);
            GameObject pauseTitle = CreateUIText(pausePanel, "PauseTitle", "GAME PAUSED", 46, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.3f));
            SetRectTransform(pauseTitle, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(560, 65), Vector2.zero);
            GameObject pResume = CreateStylizedButton(pausePanel, "PResume", "LANJUTKAN", new Color(0.2f, 0.6f, 0.35f), Color.white, new Vector2(0, 45), new Vector2(320, 60), 24);
            GameObject pRestart = CreateStylizedButton(pausePanel, "PRestart", "RESTART MATCH", new Color(0.8f, 0.45f, 0.15f), Color.white, new Vector2(0, -35), new Vector2(320, 60), 24);
            GameObject pMenu = CreateStylizedButton(pausePanel, "PMenu", "MENU UTAMA", new Color(0.2f, 0.45f, 0.75f), Color.white, new Vector2(0, -115), new Vector2(320, 60), 24);
            ui.pausePanel = pausePanel;
            ui.pauseResumeBtn = pResume.GetComponent<Button>();
            ui.pauseRestartBtn = pRestart.GetComponent<Button>();
            ui.pauseMenuBtn = pMenu.GetComponent<Button>();
            pausePanel.SetActive(false);
        }

        // =============================== MAP HELPER METHODS ===============================

        private static GameObject CreateFlatVisualSurface(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.parent = parent.transform;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        private static GameObject CreateBridge(GameObject parent, string name, Vector3 pos, float width, float length, float yRot, Material bridgeMat, Material curbMat)
        {
            GameObject bridge = new GameObject(name);
            bridge.transform.parent = parent.transform;
            bridge.transform.position = pos;
            bridge.transform.rotation = Quaternion.Euler(0, yRot, 0);

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.parent = bridge.transform;
            deck.transform.localPosition = new Vector3(0, 0.045f, 0);
            deck.transform.localRotation = Quaternion.identity;
            deck.transform.localScale = new Vector3(width, 0.06f, length);
            deck.GetComponent<MeshRenderer>().sharedMaterial = bridgeMat;
            Object.DestroyImmediate(deck.GetComponent<Collider>());

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = side < 0 ? "Curb_Left" : "Curb_Right";
                curb.transform.parent = bridge.transform;
                curb.transform.localPosition = new Vector3(side * (width * 0.5f + 0.25f), 0.22f, 0);
                curb.transform.localRotation = Quaternion.identity;
                curb.transform.localScale = new Vector3(0.5f, 0.42f, length);
                curb.GetComponent<MeshRenderer>().sharedMaterial = curbMat;
                Object.DestroyImmediate(curb.GetComponent<Collider>());
            }

            return bridge;
        }

        private static GameObject CreateBasePlatform(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = name;
            platform.transform.parent = parent.transform;
            platform.transform.position = pos;
            platform.transform.localScale = scale;
            platform.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(platform.GetComponent<Collider>());
            return platform;
        }

        // --- SPAWN FOUNTAIN (Healing Sanctuary behind base) ---
        private static GameObject CreateSpawnFountain(GameObject parent, string name, Vector3 pos, string targetTag, Material mat)
        {
            GameObject fountain = new GameObject(name);
            fountain.transform.parent = parent.transform;
            fountain.transform.position = pos;

            // Circular base platform
            GameObject basePlat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlat.name = "FountainBase";
            basePlat.transform.parent = fountain.transform;
            basePlat.transform.localPosition = new Vector3(0, 0.05f, 0);
            basePlat.transform.localScale = new Vector3(12f, 0.1f, 12f);
            basePlat.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(basePlat.GetComponent<Collider>());

            // Center pillar
            Material stoneMat = CreateColoredMaterial("Assets/Materials/Mat_FountainStone.mat", new Color(0.75f, 0.75f, 0.8f));
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "FountainPillar";
            pillar.transform.parent = fountain.transform;
            pillar.transform.localPosition = new Vector3(0, 1.5f, 0);
            pillar.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(pillar.GetComponent<Collider>());

            // Glowing water sphere on top
            Material glowMat = CreateColoredMaterial("Assets/Materials/Mat_FountainGlow.mat", new Color(0.3f, 0.9f, 1f));
            GameObject waterSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            waterSphere.name = "FountainWater";
            waterSphere.transform.parent = fountain.transform;
            waterSphere.transform.localPosition = new Vector3(0, 3.2f, 0);
            waterSphere.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            waterSphere.GetComponent<MeshRenderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(waterSphere.GetComponent<Collider>());

            // Healing trigger zone
            SphereCollider trigger = fountain.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 7f;
            trigger.center = new Vector3(0, 1f, 0);

            SpawnFountain sf = fountain.AddComponent<SpawnFountain>();
            sf.targetTag = targetTag;
            sf.healPerSecond = 120f;
            sf.manaPerSecond = 80f;

            return fountain;
        }

        // --- BOSS PIT (Smooth Stone Arena - NO ugly cube rocks!) ---
        private static GameObject CreateBossPit(GameObject parent, string name, Vector3 pos, Material pitMat, Material wallMat)
        {
            GameObject pit = new GameObject(name);
            pit.transform.parent = parent.transform;
            pit.transform.position = pos;

            // Circular stone floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "PitFloor";
            floor.transform.parent = pit.transform;
            floor.transform.localPosition = new Vector3(0, 0.03f, 0);
            floor.transform.localScale = new Vector3(14f, 0.05f, 14f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = pitMat;
            Object.DestroyImmediate(floor.GetComponent<Collider>());

            // Low smooth stone wall ring (Cylinder, not cubes!)
            GameObject wallRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wallRing.name = "PitWallRing";
            wallRing.transform.parent = pit.transform;
            wallRing.transform.localPosition = new Vector3(0, 0.4f, 0);
            wallRing.transform.localScale = new Vector3(15f, 0.4f, 15f);
            wallRing.GetComponent<MeshRenderer>().sharedMaterial = wallMat;
            Object.DestroyImmediate(wallRing.GetComponent<Collider>());

            // Inner rune circle (glowing flat disc)
            Material runeMat = CreateColoredMaterial("Assets/Materials/Mat_RuneGlow.mat", new Color(0.9f, 0.7f, 0.2f));
            GameObject runeCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeCircle.name = "RuneCircle";
            runeCircle.transform.parent = pit.transform;
            runeCircle.transform.localPosition = new Vector3(0, 0.08f, 0);
            runeCircle.transform.localScale = new Vector3(8f, 0.02f, 8f);
            runeCircle.GetComponent<MeshRenderer>().sharedMaterial = runeMat;
            Object.DestroyImmediate(runeCircle.GetComponent<Collider>());

            return pit;
        }

        // --- ORGANIC BUSH (Multi-sphere cluster, NOT cube!) ---
        private static GameObject CreateBush(GameObject parent, string name, Vector3 pos, Material mat)
        {
            GameObject bush = new GameObject(name);
            bush.tag = "Bush";
            bush.transform.parent = parent.transform;
            bush.transform.position = pos;

            // Create 5 overlapping spheres for organic look
            float[] offX = { 0f, -0.8f, 0.9f, -0.4f, 0.5f };
            float[] offZ = { 0f, 0.5f, -0.3f, -0.7f, 0.6f };
            float[] sizes = { 1.6f, 1.3f, 1.4f, 1.1f, 1.2f };

            Material darkBush = CreateColoredMaterial("Assets/Materials/Mat_BushDark.mat", new Color(0.06f, 0.35f, 0.1f));

            for (int i = 0; i < 5; i++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = $"BushLeaf_{i}";
                leaf.transform.parent = bush.transform;
                leaf.transform.localPosition = new Vector3(offX[i], sizes[i] * 0.4f, offZ[i]);
                leaf.transform.localScale = new Vector3(sizes[i], sizes[i] * 0.7f, sizes[i]);
                leaf.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? mat : darkBush;
                Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }

            // Single trigger collider for the whole bush
            SphereCollider col = bush.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.5f;
            col.center = new Vector3(0, 0.6f, 0);

            return bush;
        }

        // --- JUNGLE TREE ---
        private static GameObject CreateTree(GameObject parent, Vector3 pos, Material trunkMat, Material leafMat, float scale)
        {
            GameObject tree = new GameObject("JungleTree");
            tree.transform.parent = parent.transform;
            tree.transform.position = pos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0, 1.5f * scale, 0);
            trunk.transform.localScale = new Vector3(0.6f * scale, 1.5f * scale, 0.6f * scale);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMat;

            GameObject leaves1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves1.name = "LeavesLower";
            leaves1.transform.parent = tree.transform;
            leaves1.transform.localPosition = new Vector3(0, 3.2f * scale, 0);
            leaves1.transform.localScale = new Vector3(2.8f * scale, 2.0f * scale, 2.8f * scale);
            leaves1.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
            Object.DestroyImmediate(leaves1.GetComponent<Collider>());

            GameObject leaves2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves2.name = "LeavesUpper";
            leaves2.transform.parent = tree.transform;
            leaves2.transform.localPosition = new Vector3(0, 4.3f * scale, 0);
            leaves2.transform.localScale = new Vector3(2.0f * scale, 1.8f * scale, 2.0f * scale);
            leaves2.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
            Object.DestroyImmediate(leaves2.GetComponent<Collider>());

            return tree;
        }

        // --- WALL ---
        private static GameObject CreateWall(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.tag = "Wall";
            wall.transform.parent = parent.transform;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return wall;
        }

        private static void CreateStylizedRock(GameObject parent, string prefabPath, Vector3 pos, float scale, float yRot, Texture2D fallbackTex)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rock.transform.parent = parent.transform;
            rock.transform.position = pos;
            rock.transform.rotation = Quaternion.Euler(0, yRot, 0);
            rock.transform.localScale = Vector3.one * scale;
            rock.tag = "Obstacle";

            // Ensure Standard shader compatibility (no pink URP materials)
            foreach (var rend in rock.GetComponentsInChildren<MeshRenderer>())
            {
                Material m = rend.sharedMaterial;
                if (m == null || (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null &&
                    (m.shader == null || m.shader.name.Contains("Universal") || m.shader.name.Contains("Error"))))
                {
                    Material safeMat = CreateTexturedMaterial("Assets/Materials/Mat_StylizedRock.mat", Color.white, fallbackTex, null, Vector2.one, 0.2f);
                    rend.sharedMaterial = safeMat;
                }
            }
        }

        // --- TURRET (Tower with proper structure) ---
        private static GameObject CreateTurret(GameObject parent, string name, Vector3 pos, string targetTag, Material teamMat, float maxHp, float damage, float attackRange, float sizeScale)
        {
            bool isEnemy = targetTag == "Player";
            GameObject turretObj = new GameObject(name);
            if (isEnemy) turretObj.tag = "Enemy";
            turretObj.transform.parent = parent.transform;
            turretObj.transform.position = pos;

            CapsuleCollider col = turretObj.AddComponent<CapsuleCollider>();
            col.radius = sizeScale * 0.65f;
            col.height = sizeScale * 3.5f;
            col.center = new Vector3(0, sizeScale * 1.75f, 0);

            Material stoneMat = CreateColoredMaterial("Assets/Materials/Mat_TowerStone.mat", new Color(0.42f, 0.44f, 0.48f));

            // 1. Stepped Stone Base
            GameObject baseStep1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseStep1.name = "BasePedestal";
            baseStep1.transform.parent = turretObj.transform;
            baseStep1.transform.localPosition = new Vector3(0, 0.3f, 0);
            baseStep1.transform.localScale = new Vector3(sizeScale * 1.6f, 0.3f, sizeScale * 1.6f);
            baseStep1.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(baseStep1.GetComponent<Collider>());

            GameObject baseStep2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseStep2.name = "BaseTrim";
            baseStep2.transform.parent = turretObj.transform;
            baseStep2.transform.localPosition = new Vector3(0, 0.7f, 0);
            baseStep2.transform.localScale = new Vector3(sizeScale * 1.25f, 0.2f, sizeScale * 1.25f);
            baseStep2.GetComponent<MeshRenderer>().sharedMaterial = teamMat;
            Object.DestroyImmediate(baseStep2.GetComponent<Collider>());

            // 2. Tower Shaft
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "TowerShaft";
            shaft.transform.parent = turretObj.transform;
            shaft.transform.localPosition = new Vector3(0, sizeScale * 1.3f, 0);
            shaft.transform.localScale = new Vector3(sizeScale * 0.85f, sizeScale * 1.1f, sizeScale * 0.85f);
            shaft.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(shaft.GetComponent<Collider>());

            // 3. 4 Buttresses
            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                GameObject rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rib.name = $"Buttress_{i}";
                rib.transform.parent = turretObj.transform;
                rib.transform.localPosition = new Vector3(Mathf.Cos(angle) * sizeScale * 0.45f, sizeScale * 1.25f, Mathf.Sin(angle) * sizeScale * 0.45f);
                rib.transform.localScale = new Vector3(sizeScale * 0.22f, sizeScale * 2.0f, sizeScale * 0.22f);
                rib.transform.localRotation = Quaternion.Euler(0, i * 90f + 45f, 0);
                rib.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
                Object.DestroyImmediate(rib.GetComponent<Collider>());
            }

            // 4. Top Crown
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "TowerCrown";
            crown.transform.parent = turretObj.transform;
            crown.transform.localPosition = new Vector3(0, sizeScale * 2.45f, 0);
            crown.transform.localScale = new Vector3(sizeScale * 1.15f, 0.25f, sizeScale * 1.15f);
            crown.GetComponent<MeshRenderer>().sharedMaterial = teamMat;
            Object.DestroyImmediate(crown.GetComponent<Collider>());

            // 5. Floating Crystal
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystal.name = "CrystalCore";
            crystal.transform.parent = turretObj.transform;
            crystal.transform.localPosition = new Vector3(0, sizeScale * 3.2f, 0);
            crystal.transform.localScale = Vector3.one * (sizeScale * 0.55f);
            crystal.transform.localRotation = Quaternion.Euler(45f, 45f, 0);
            crystal.GetComponent<MeshRenderer>().sharedMaterial = teamMat;
            Object.DestroyImmediate(crystal.GetComponent<Collider>());

            HealthSystem hp = turretObj.AddComponent<HealthSystem>();
            hp.maxHealth = maxHp;
            hp.currentHealth = maxHp;

            Turret turretLogic = turretObj.AddComponent<Turret>();
            turretLogic.turretName = name;
            turretLogic.targetTag = targetTag;
            turretLogic.isEnemyTurret = targetTag == "Player";
            turretLogic.isCore = name.Contains("Crystal_Core");
            turretLogic.attackRadius = attackRange;
            turretLogic.attackDamage = damage;
            turretLogic.healthSystem = hp;
            turretLogic.firePoint = crystal.transform;

            LineRenderer laser = turretObj.AddComponent<LineRenderer>();
            laser.startWidth = 0.14f;
            laser.endWidth = 0.06f;
            laser.sharedMaterial = teamMat;
            laser.enabled = false;
            turretLogic.laserBeam = laser;

            CreateOverheadSliderBar(turretObj, turretLogic.isEnemyTurret ? new Color(0.95f, 0.22f, 0.26f) : new Color(0.18f, 0.78f, 1f), sizeScale * 4.1f);

            return turretObj;
        }

        // =============================== UI HELPERS ===============================

        private static void AddGoldBorder(GameObject uiObj, Color borderColor, float thickness)
        {
            Outline outline = uiObj.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(thickness, thickness);
        }

        private static GameObject CreateUIPanel(GameObject parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.parent = parent.transform;
            RectTransform rt = obj.AddComponent<RectTransform>();
            SetRectTransform(obj, anchorMin, anchorMax, size, anchoredPos);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static GameObject CreateUIText(GameObject parent, string name, string content, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.parent = parent.transform;
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Text txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = alignment;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return obj;
        }

        private static void SetRectPadding(GameObject obj, float horizontal, float vertical)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.offsetMin = new Vector2(horizontal, vertical);
            rt.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static GameObject CreateStylizedButton(GameObject parent, string name, string label, Color bgColor, Color borderColor, Vector2 anchoredPos, Vector2 size, int fontSize)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.parent = parent.transform;
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            AddGoldBorder(btnObj, borderColor, 2.5f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.75f;
            btn.colors = cb;

            CreateUIText(btnObj, "Text", label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            return btnObj;
        }

        private static Slider CreateUISlider(GameObject parent, string name, Color fillColor)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.parent = parent.transform;
            RectTransform rt = sliderObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 30);
            rt.anchoredPosition = Vector2.zero;

            Slider slider = sliderObj.AddComponent<Slider>();

            CreateUIPanel(sliderObj, "Background", new Color(0.1f, 0.1f, 0.1f, 0.9f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.parent = sliderObj.transform;
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = Vector2.zero;
            faRt.offsetMax = Vector2.zero;

            GameObject fill = CreateUIPanel(fillArea, "Fill", fillColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            return slider;
        }

        private static void CreateCooldownOverlay(GameObject btnObj, out Image overlay, out Text cdText)
        {
            GameObject ovObj = CreateUIPanel(btnObj, "CooldownOverlay", new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay = ovObj.GetComponent<Image>();
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillAmount = 0f;
            overlay.enabled = false;

            GameObject txtObj = CreateUIText(ovObj, "CdText", "", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            cdText = txtObj.GetComponent<Text>();
            cdText.enabled = false;
        }

        private static void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPos)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
        }
    }
}
#endif
