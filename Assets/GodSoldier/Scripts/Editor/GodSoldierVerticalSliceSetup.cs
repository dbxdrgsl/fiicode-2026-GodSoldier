using System.IO;
using System.Reflection;
using Blocks.Gameplay.Core;
using Blocks.Gameplay.Shooter;
using Blocks.Sessions.Common;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GodSoldier.Editor
{
    public static class GodSoldierVerticalSliceSetup
    {
        const string k_AutoGenerateFlag = "GodSoldier.Game.GeneratedOrRepairedV4";

        const string k_BootstrapScenePath = "Assets/GodSoldier/Scenes/GodSoldier_Bootstrap.unity";
        const string k_MainMenuScenePath = "Assets/GodSoldier/Scenes/GodSoldier_MainMenu.unity";
        const string k_LobbyScenePath = "Assets/GodSoldier/Scenes/GodSoldier_Lobby.unity";
        const string k_DescentScenePath = "Assets/GodSoldier/Scenes/GodSoldier_Descent.unity";
        const string k_WarTrialScenePath = "Assets/GodSoldier/Scenes/GodSoldier_WarTrial.unity";
        const string k_JudgmentScenePath = "Assets/GodSoldier/Scenes/GodSoldier_Judgment.unity";

        const string k_NetworkManagerPrefabPath = "Assets/Core/Prefabs/[BB] NetworkManager.prefab";
        const string k_GameManagerPrefabPath = "Assets/Core/Prefabs/[BB] GameManager.prefab";
        const string k_UnityServicesPrefabPath = "Assets/Blocks/CommonSession/Prefabs/UnityServicesWithName.prefab";
        const string k_ShooterPlayerPrefabPath = "Assets/Shooter/Prefabs/[BB] ShooterPlayer.prefab";
        const string k_PanelSettingsPath = "Assets/Blocks/Common/BlocksPanelSettings.asset";
        const string k_MainMenuUxmlPath = "Assets/GodSoldier/UI/MainMenu.uxml";
        const string k_LobbyUxmlPath = "Assets/GodSoldier/UI/Lobby.uxml";
        const string k_NotificationEventPath = "Assets/Core/GameEvents/Player/NotificationEvent.asset";
        const string k_OnPrimaryActionPath = "Assets/Core/GameEvents/Input/OnPrimaryActionPressed.asset";
        const string k_MissionCatalogPath = "Assets/GodSoldier/Settings/GodSoldierMissionCatalog.asset";
        const string k_SessionBrowserUxmlPath = "Assets/Blocks/MultiplayerSession/UI/SessionBrowser.uxml";
        const string k_JoinByCodeUxmlPath = "Assets/Blocks/MultiplayerSession/UI/JoinSessionByCode.uxml";
        const string k_CurrentSessionUxmlPath = "Assets/Blocks/CommonSession/UI/CurrentSession.uxml";

        const string k_DescentPublicSessionSettingsPath = "Assets/GodSoldier/Settings/Networking/Descent_Public.asset";
        const string k_DescentPrivateSessionSettingsPath = "Assets/GodSoldier/Settings/Networking/Descent_Private.asset";
        const string k_WarTrialPublicSessionSettingsPath = "Assets/GodSoldier/Settings/Networking/WarTrial_Public.asset";
        const string k_WarTrialPrivateSessionSettingsPath = "Assets/GodSoldier/Settings/Networking/WarTrial_Private.asset";
        const string k_JudgmentPublicSessionSettingsPath = "Assets/GodSoldier/Settings/Networking/Judgment_Public.asset";
        const string k_JudgmentPrivateSessionSettingsPath = "Assets/GodSoldier/Settings/Networking/Judgment_Private.asset";

        const string k_ReviveActionId = "revive_ritual";
        const string k_RevealClueActionId = "reveal_clue";
        const string k_PushObstacleActionId = "push_obstacle";
        const string k_PlatformAActionId = "platform_a";
        const string k_PlatformBActionId = "platform_b";
        const string k_PlatformCActionId = "platform_c";
        const string k_CraftExplosiveActionId = "craft_explosive";

        static readonly MethodInfo s_NetworkObjectOnValidate =
            typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo s_Hash32String =
            typeof(NetworkObject).Assembly.GetType("Unity.Netcode.XXHash")?.GetMethod(
                "Hash32",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);
        static readonly FieldInfo s_GlobalObjectIdHashField =
            typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem("God Soldier/Regenerate Game Skeleton")]
        public static void RegenerateVerticalSlice()
        {
            EnsureFolders();
            ConfigureShooterPlayerPrefab();
            EnsureMissionCatalogAndSessionSettings();
            CreateBootstrapScene();
            CreateMainMenuScene();
            CreateLobbyScene();
            CreateDescentScene();
            CreateWarTrialScene();
            CreateJudgmentScene();
            UpdateBuildSettings();
            RefreshGeneratedScenesOnDisk();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [InitializeOnLoadMethod]
        static void ScheduleAutoGeneration()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += TryAutoGenerateInOpenEditor;
        }

        static void TryAutoGenerateInOpenEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                return;
            }

            if (SessionState.GetBool(k_AutoGenerateFlag, false))
            {
                return;
            }

            SessionState.SetBool(k_AutoGenerateFlag, true);
            if (GameNeedsRepair())
            {
                RegenerateVerticalSlice();
                Debug.Log("God Soldier game skeleton assets regenerated in the active editor session.");
            }
        }

        static bool GameNeedsRepair()
        {
            if (!File.Exists(k_BootstrapScenePath) ||
                !File.Exists(k_MainMenuScenePath) ||
                !File.Exists(k_LobbyScenePath) ||
                !File.Exists(k_DescentScenePath) ||
                !File.Exists(k_WarTrialScenePath) ||
                !File.Exists(k_JudgmentScenePath) ||
                !File.Exists(k_MissionCatalogPath))
            {
                return true;
            }

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_ShooterPlayerPrefabPath);
            if (playerPrefab == null ||
                playerPrefab.GetComponent<GodSoldierRoleController>() == null ||
                playerPrefab.GetComponent<GodSoldierHudExtension>() == null)
            {
                return true;
            }

            return EditorBuildSettings.scenes.Length != 6;
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/GodSoldier", "Scenes");
            EnsureFolder("Assets/GodSoldier", "Settings");
            EnsureFolder("Assets/GodSoldier/Settings", "Networking");
        }

        static void EnsureFolder(string parent, string child)
        {
            var combined = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(combined))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        static void ConfigureShooterPlayerPrefab()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(k_ShooterPlayerPrefabPath);
            try
            {
                var roleController = prefabRoot.GetComponent<GodSoldierRoleController>() ?? prefabRoot.AddComponent<GodSoldierRoleController>();
                if (prefabRoot.GetComponent<GodSoldierHudExtension>() == null)
                {
                    prefabRoot.AddComponent<GodSoldierHudExtension>();
                }

                var serialized = new SerializedObject(roleController);
                serialized.FindProperty("corePlayerState").objectReferenceValue = prefabRoot.GetComponent<CorePlayerState>();
                serialized.FindProperty("coreMovement").objectReferenceValue = prefabRoot.GetComponent<CoreMovement>();
                serialized.FindProperty("corePlayerManager").objectReferenceValue = prefabRoot.GetComponent<CorePlayerManager>();
                serialized.FindProperty("shooterAddon").objectReferenceValue = prefabRoot.GetComponent<ShooterAddon>();
                serialized.FindProperty("shooterInputHandler").objectReferenceValue = prefabRoot.GetComponent<ShooterInputHandler>();
                serialized.FindProperty("weaponController").objectReferenceValue = prefabRoot.GetComponent<WeaponController>();
                serialized.FindProperty("aimController").objectReferenceValue = prefabRoot.GetComponent<AimController>();
                serialized.FindProperty("onPrimaryActionPressed").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameEvent>(k_OnPrimaryActionPath);
                serialized.FindProperty("onNotification").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NotificationEvent>(k_NotificationEventPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, k_ShooterPlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        static void EnsureMissionCatalogAndSessionSettings()
        {
            var descentPublic = EnsureSessionSettings(k_DescentPublicSessionSettingsPath, "Descent Public", "godsoldier-descent", false);
            var descentPrivate = EnsureSessionSettings(k_DescentPrivateSessionSettingsPath, "Descent Private", "godsoldier-descent", true);
            var warTrialPublic = EnsureSessionSettings(k_WarTrialPublicSessionSettingsPath, "War Trial Public", "godsoldier-wartrial", false);
            var warTrialPrivate = EnsureSessionSettings(k_WarTrialPrivateSessionSettingsPath, "War Trial Private", "godsoldier-wartrial", true);
            var judgmentPublic = EnsureSessionSettings(k_JudgmentPublicSessionSettingsPath, "Judgment Public", "godsoldier-judgment", false);
            var judgmentPrivate = EnsureSessionSettings(k_JudgmentPrivateSessionSettingsPath, "Judgment Private", "godsoldier-judgment", true);

            var catalog = AssetDatabase.LoadAssetAtPath<GodSoldierMissionCatalog>(k_MissionCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GodSoldierMissionCatalog>();
                AssetDatabase.CreateAsset(catalog, k_MissionCatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var missions = serialized.FindProperty("missions");
            missions.arraySize = 3;

            ConfigureMissionDefinition(
                missions.GetArrayElementAtIndex(0),
                "descent",
                "Descent",
                "The bond is forged in a dying trench.",
                "The God descends, revives the Soldier, and the pair learn to move as one through ruin, dream-space, and the first breach of war.",
                1,
                GodSoldierSceneNames.Descent,
                "This mission is the beginning of the campaign and the strongest place to start.",
                new Color(0.82f, 0.61f, 0.36f),
                descentPublic,
                descentPrivate);

            ConfigureMissionDefinition(
                missions.GetArrayElementAtIndex(1),
                "war-trial",
                "War Trial",
                "A corrupted mirror pair descends to break the bond.",
                "The God and Soldier face a scripted raid-like duel against a false God and a corrupted Soldier, with attack phases and lesser war spirits between them.",
                2,
                GodSoldierSceneNames.WarTrial,
                "War Trial lands harder once Descent has introduced the bond and its rhythm.",
                new Color(0.67f, 0.34f, 0.30f),
                warTrialPublic,
                warTrialPrivate);

            ConfigureMissionDefinition(
                missions.GetArrayElementAtIndex(2),
                "judgment",
                "Judgment",
                "The war slows down long enough to demand a final choice.",
                "The pair enter a narrative judgment space where choices, chambers, and an assassination climax decide whether peace is imposed or earned.",
                3,
                GodSoldierSceneNames.Judgment,
                "Judgment is written as the end of the early campaign timeline.",
                new Color(0.41f, 0.53f, 0.83f),
                judgmentPublic,
                judgmentPrivate);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static SessionSettings EnsureSessionSettings(string path, string sessionName, string sessionType, bool isPrivate)
        {
            var settings = AssetDatabase.LoadAssetAtPath<SessionSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<SessionSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.maxPlayers = 2;
            settings.sessionName = sessionName;
            settings.sessionType = sessionType;
            settings.isPrivate = isPrivate;
            settings.usePlayerName = true;
            settings.createNetworkSession = true;
            settings.networkType = Unity.Services.Multiplayer.NetworkType.Relay;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        static void ConfigureMissionDefinition(
            SerializedProperty property,
            string missionId,
            string displayName,
            string headline,
            string description,
            int recommendedOrder,
            string sceneName,
            string warning,
            Color accentColor,
            SessionSettings publicSettings,
            SessionSettings privateSettings)
        {
            property.FindPropertyRelative("missionId").stringValue = missionId;
            property.FindPropertyRelative("displayName").stringValue = displayName;
            property.FindPropertyRelative("headline").stringValue = headline;
            property.FindPropertyRelative("description").stringValue = description;
            property.FindPropertyRelative("recommendedOrder").intValue = recommendedOrder;
            property.FindPropertyRelative("sceneName").stringValue = sceneName;
            property.FindPropertyRelative("outOfOrderWarning").stringValue = warning;
            property.FindPropertyRelative("allowPublicMatch").boolValue = true;
            property.FindPropertyRelative("allowPrivateMatch").boolValue = true;
            property.FindPropertyRelative("maxHumanPlayers").intValue = 2;
            property.FindPropertyRelative("accentColor").colorValue = accentColor;
            property.FindPropertyRelative("publicSessionSettings").objectReferenceValue = publicSettings;
            property.FindPropertyRelative("privateSessionSettings").objectReferenceValue = privateSettings;
        }

        static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBootstrapperObject();
            SaveGeneratedScene(scene, k_BootstrapScenePath);
        }

        static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBootstrapperObject();
            CreateCoreDirectorService();
            CreateCamera("Main Menu Camera", new Vector3(0f, 7.5f, -20f), new Vector3(18f, 0f, 0f), true, true);
            CreateDirectionalLight();
            CreateEventSystem();
            CreateMenuBackdrop();
            CreateMenuUIDocument();
            SaveGeneratedScene(scene, k_MainMenuScenePath);
        }

        static void CreateLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBootstrapperObject();
            CreateCoreDirectorService();
            CreateCamera("Lobby Camera", new Vector3(0f, 8f, -18f), new Vector3(18f, 0f, 0f), true, true);
            CreateDirectionalLight();
            CreateEventSystem();
            CreateLobbyBackdrop();
            CreateLobbyUIDocument();
            SaveGeneratedScene(scene, k_LobbyScenePath);
        }

        static void CreateDescentScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBootstrapperObject();
            CreateCoreDirectorService();
            CreateCamera("Descent Scene Camera", new Vector3(10f, 14f, -28f), new Vector3(24f, 18f, 0f), false, true);
            CreateDirectionalLight();

            CreateBlock("RoomFloor", new Vector3(-18f, -0.5f, 0f), new Vector3(12f, 1f, 12f), new Color(0.17f, 0.18f, 0.20f));
            CreateBlock("RoomBackWall", new Vector3(-23.5f, 2f, 0f), new Vector3(1f, 5f, 12f), new Color(0.13f, 0.14f, 0.15f));
            CreateBlock("CorridorFloor", new Vector3(-7f, -0.5f, 0f), new Vector3(10f, 1f, 6f), new Color(0.20f, 0.19f, 0.18f));
            CreateBlock("DreamLedge", new Vector3(3f, -0.75f, 0f), new Vector3(4f, 0.5f, 8f), new Color(0.22f, 0.24f, 0.30f));
            CreateBlock("DreamAnchor", new Vector3(18f, -0.25f, 0f), new Vector3(6f, 1f, 10f), new Color(0.20f, 0.24f, 0.31f));
            CreateBlock("CombatYard", new Vector3(28f, -0.5f, 0f), new Vector3(16f, 1f, 14f), new Color(0.23f, 0.22f, 0.20f));
            CreateBlock("ForgePedestal", new Vector3(24f, 0.75f, 5f), new Vector3(2f, 1.5f, 2f), new Color(0.32f, 0.24f, 0.18f));
            CreateBlock("FinalApron", new Vector3(37f, -0.5f, 0f), new Vector3(8f, 1f, 10f), new Color(0.22f, 0.22f, 0.24f));

            var soldierSpawn = CreateMarker("SoldierSpawn", new Vector3(-19f, 0.6f, -1.4f), new Vector3(0f, 25f, 0f), new Color(0.67f, 0.35f, 0.26f));
            var godSpawn = CreateMarker("GodSpawn", new Vector3(-15.5f, 1.9f, 2.2f), new Vector3(0f, -35f, 0f), new Color(0.42f, 0.58f, 0.83f));

            CreateActionTarget("ReviveRitual", k_ReviveActionId, new Vector3(-18f, 0.9f, 0f), new Vector3(1.4f, 0.2f, 1.4f), GodSoldierPlayerRole.None);
            CreateActionTarget("RevealClueAltar", k_RevealClueActionId, new Vector3(-14f, 0.9f, 3f), new Vector3(0.8f, 1.4f, 0.8f), GodSoldierPlayerRole.God);

            var obstacleBarrier = CreateBlock("ObstacleBarrier", new Vector3(-1.5f, 1.4f, 0f), new Vector3(1.2f, 3f, 6f), new Color(0.18f, 0.18f, 0.19f));
            var obstacleActivator = CreateActivator("ObstacleBarrierActivator", new[] { obstacleBarrier }, true);
            CreateActionTarget("PushObstaclePad", k_PushObstacleActionId, new Vector3(-5.5f, 0.9f, 0f), new Vector3(1.6f, 0.2f, 1.6f), GodSoldierPlayerRole.None);
            CreateTriggerZone("RoomExitTrigger", "room_exit", new Vector3(-3.4f, 1f, 0f), new Vector3(2f, 2f, 4f), GodSoldierPlayerRole.Soldier, false);

            var spiritPlatformA = CreateBlock("SpiritPlatform_A", new Vector3(6f, -0.15f, -1f), new Vector3(3f, 0.5f, 3f), new Color(0.35f, 0.44f, 0.56f));
            var spiritPlatformB = CreateBlock("SpiritPlatform_B", new Vector3(12f, 0.45f, 1f), new Vector3(3f, 0.5f, 3f), new Color(0.35f, 0.44f, 0.56f));
            var spiritPlatformC = CreateBlock("SpiritPlatform_C", new Vector3(18f, 0.15f, -0.6f), new Vector3(3f, 0.5f, 3f), new Color(0.35f, 0.44f, 0.56f));
            spiritPlatformA.SetActive(false);
            spiritPlatformB.SetActive(false);
            spiritPlatformC.SetActive(false);
            var platformActivatorA = CreateActivator("SpiritPlatformActivator_A", new[] { spiritPlatformA }, false);
            var platformActivatorB = CreateActivator("SpiritPlatformActivator_B", new[] { spiritPlatformB }, false);
            var platformActivatorC = CreateActivator("SpiritPlatformActivator_C", new[] { spiritPlatformC }, false);
            CreateActionTarget("PlatformAction_A", k_PlatformAActionId, new Vector3(4f, 0.9f, -2.4f), new Vector3(0.9f, 1.2f, 0.9f), GodSoldierPlayerRole.God);
            CreateActionTarget("PlatformAction_B", k_PlatformBActionId, new Vector3(10f, 1.4f, 2.6f), new Vector3(0.9f, 1.2f, 0.9f), GodSoldierPlayerRole.God);
            CreateActionTarget("PlatformAction_C", k_PlatformCActionId, new Vector3(16f, 1.1f, -2.4f), new Vector3(0.9f, 1.2f, 0.9f), GodSoldierPlayerRole.God);
            CreateTriggerZone("RealmEntryTrigger", "realm_entry", new Vector3(20f, 1.1f, 0f), new Vector3(2f, 2f, 5f), GodSoldierPlayerRole.Soldier, false);

            CreateBlock("CombatCover_A", new Vector3(26f, 0.8f, -4.5f), new Vector3(2f, 1.6f, 1f), new Color(0.18f, 0.20f, 0.18f));
            CreateBlock("CombatCover_B", new Vector3(29f, 0.8f, 0f), new Vector3(2f, 1.6f, 1f), new Color(0.18f, 0.20f, 0.18f));
            CreateBlock("CombatCover_C", new Vector3(26f, 0.8f, 4.5f), new Vector3(2f, 1.6f, 1f), new Color(0.18f, 0.20f, 0.18f));
            var combatTargetA = CreateShootableTarget("CombatTarget_A", "combat_target_a", new Vector3(31f, 1.2f, -4.5f), new Vector3(1.2f, 1.2f, 1.2f), 3);
            var combatTargetB = CreateShootableTarget("CombatTarget_B", "combat_target_b", new Vector3(33f, 1.2f, 0f), new Vector3(1.2f, 1.2f, 1.2f), 3);
            var combatTargetC = CreateShootableTarget("CombatTarget_C", "combat_target_c", new Vector3(31f, 1.2f, 4.5f), new Vector3(1.2f, 1.2f, 1.2f), 3);

            var resourceTriggerA = CreateTriggerZone("ExplosiveResource_1", "resource_1", new Vector3(23f, 0.9f, -5.3f), new Vector3(1.5f, 1.5f, 1.5f), GodSoldierPlayerRole.Soldier, true);
            var resourceTriggerB = CreateTriggerZone("ExplosiveResource_2", "resource_2", new Vector3(25.5f, 0.9f, -1.1f), new Vector3(1.5f, 1.5f, 1.5f), GodSoldierPlayerRole.Soldier, true);
            var resourceTriggerC = CreateTriggerZone("ExplosiveResource_3", "resource_3", new Vector3(27.2f, 0.9f, 3.8f), new Vector3(1.5f, 1.5f, 1.5f), GodSoldierPlayerRole.Soldier, true);
            CreateActionTarget("CraftExplosiveForge", k_CraftExplosiveActionId, new Vector3(24f, 1.7f, 5f), new Vector3(1f, 1.5f, 1f), GodSoldierPlayerRole.God);

            var finalBarrier = CreateBlock("FinalBreachBarrier", new Vector3(34f, 1.8f, 0f), new Vector3(1f, 3.5f, 8f), new Color(0.19f, 0.17f, 0.18f));
            var finalBreachActivator = CreateActivator("FinalBreachActivator", new[] { finalBarrier }, true);
            CreateTriggerZone("FinalBreachTrigger", "final_breach", new Vector3(38f, 1f, 0f), new Vector3(2f, 2f, 6f), GodSoldierPlayerRole.Soldier, false);

            var directorObject = new GameObject("DescentDirector");
            directorObject.AddComponent<NetworkObject>();
            var director = directorObject.AddComponent<GodSoldierDescentDirector>();
            var directorSerialized = new SerializedObject(director);
            ConfigureDirectorBase(directorSerialized, "descent", "Descent", new[] { soldierSpawn.transform }, new[] { godSpawn.transform });
            directorSerialized.FindProperty("obstacleBarrier").objectReferenceValue = obstacleActivator;
            directorSerialized.FindProperty("finalBreachBarrier").objectReferenceValue = finalBreachActivator;
            AssignObjectArray(directorSerialized.FindProperty("spiritPlatforms"), new Object[] { platformActivatorA, platformActivatorB, platformActivatorC });
            AssignObjectArray(directorSerialized.FindProperty("combatTargets"), new Object[] { combatTargetA, combatTargetB, combatTargetC });
            AssignObjectArray(directorSerialized.FindProperty("explosiveResourceTriggers"), new Object[] { resourceTriggerA, resourceTriggerB, resourceTriggerC });
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();

            SaveGeneratedScene(scene, k_DescentScenePath);
        }

        static void CreateWarTrialScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBootstrapperObject();
            CreateCoreDirectorService();
            CreateCamera("War Trial Scene Camera", new Vector3(0f, 16f, -26f), new Vector3(24f, 0f, 0f), false, true);
            CreateDirectionalLight();

            CreateBlock("ArenaFloor", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), new Color(0.19f, 0.19f, 0.21f));
            CreateBlock("ArenaRimNorth", new Vector3(0f, 1f, 14.5f), new Vector3(30f, 3f, 1f), new Color(0.14f, 0.14f, 0.15f));
            CreateBlock("ArenaRimSouth", new Vector3(0f, 1f, -14.5f), new Vector3(30f, 3f, 1f), new Color(0.14f, 0.14f, 0.15f));
            CreateBlock("ArenaPillar_A", new Vector3(-12f, 3f, 0f), new Vector3(2f, 6f, 2f), new Color(0.18f, 0.18f, 0.20f));
            CreateBlock("ArenaPillar_B", new Vector3(12f, 3f, 0f), new Vector3(2f, 6f, 2f), new Color(0.18f, 0.18f, 0.20f));

            var soldierSpawn = CreateMarker("SoldierSpawn", new Vector3(-8f, 0.6f, -6f), new Vector3(0f, 30f, 0f), new Color(0.67f, 0.35f, 0.26f));
            var godSpawn = CreateMarker("GodSpawn", new Vector3(-10f, 1.5f, -10f), new Vector3(0f, 30f, 0f), new Color(0.42f, 0.58f, 0.83f));

            var soldierBossShield = CreateBlock("SoldierBossShield", new Vector3(0f, 2f, -6f), new Vector3(4f, 4f, 4f), new Color(0.54f, 0.24f, 0.22f));
            var soldierBossShieldActivator = CreateActivator("SoldierBossShieldActivator", new[] { soldierBossShield }, true);
            var soldierBossCore = CreateShootableTarget("SoldierBossCore", "soldier_boss_core", new Vector3(0f, 1.2f, -6f), new Vector3(1.6f, 1.6f, 1.6f), 4);
            CreateTriggerZone("AnchorSoldierBoss", "anchor_soldier_boss", new Vector3(-4f, 1f, -6f), new Vector3(2f, 2f, 2f), GodSoldierPlayerRole.Soldier, false);
            CreateActionTarget("ShatterSoldierWard", "shatter_soldier_ward", new Vector3(4f, 1f, -6f), new Vector3(1.1f, 1.3f, 1.1f), GodSoldierPlayerRole.God);

            var waveTargetA = CreateShootableTarget("WaveTarget_A", "wave_a", new Vector3(-2.5f, 1f, 4f), new Vector3(1.2f, 1.2f, 1.2f), 2);
            var waveTargetB = CreateShootableTarget("WaveTarget_B", "wave_b", new Vector3(0f, 1f, 6.2f), new Vector3(1.2f, 1.2f, 1.2f), 2);
            var waveTargetC = CreateShootableTarget("WaveTarget_C", "wave_c", new Vector3(2.5f, 1f, 4f), new Vector3(1.2f, 1.2f, 1.2f), 2);
            var waveGroupActivator = CreateActivator("WaveGroupActivator", new[] { waveTargetA.gameObject, waveTargetB.gameObject, waveTargetC.gameObject }, false);

            var godBossShield = CreateBlock("GodBossShield", new Vector3(0f, 2f, 8f), new Vector3(4f, 4f, 4f), new Color(0.28f, 0.34f, 0.62f));
            var godBossShieldActivator = CreateActivator("GodBossShieldActivator", new[] { godBossShield }, true);
            var godBossCore = CreateShootableTarget("GodBossCore", "god_boss_core", new Vector3(0f, 1.2f, 8f), new Vector3(1.6f, 1.6f, 1.6f), 4);
            CreateTriggerZone("AnchorGodBoss", "anchor_god_boss", new Vector3(-4f, 1f, 8f), new Vector3(2f, 2f, 2f), GodSoldierPlayerRole.Soldier, false);
            CreateActionTarget("SealGodBoss", "seal_god_boss", new Vector3(4f, 1f, 8f), new Vector3(1.1f, 1.3f, 1.1f), GodSoldierPlayerRole.God);

            var soldierTelegraphA = CreateBlock("SoldierTelegraph_A", new Vector3(-8f, 0.05f, 0f), new Vector3(3f, 0.1f, 3f), new Color(0.65f, 0.27f, 0.21f));
            var soldierTelegraphB = CreateBlock("SoldierTelegraph_B", new Vector3(8f, 0.05f, 0f), new Vector3(3f, 0.1f, 3f), new Color(0.65f, 0.27f, 0.21f));
            soldierTelegraphA.SetActive(false);
            soldierTelegraphB.SetActive(false);
            var soldierTelegraphActivatorA = CreateActivator("SoldierTelegraphActivator_A", new[] { soldierTelegraphA }, false);
            var soldierTelegraphActivatorB = CreateActivator("SoldierTelegraphActivator_B", new[] { soldierTelegraphB }, false);

            var godTelegraphA = CreateBlock("GodTelegraph_A", new Vector3(-8f, 0.05f, 12f), new Vector3(3f, 0.1f, 3f), new Color(0.30f, 0.42f, 0.77f));
            var godTelegraphB = CreateBlock("GodTelegraph_B", new Vector3(8f, 0.05f, 12f), new Vector3(3f, 0.1f, 3f), new Color(0.30f, 0.42f, 0.77f));
            godTelegraphA.SetActive(false);
            godTelegraphB.SetActive(false);
            var godTelegraphActivatorA = CreateActivator("GodTelegraphActivator_A", new[] { godTelegraphA }, false);
            var godTelegraphActivatorB = CreateActivator("GodTelegraphActivator_B", new[] { godTelegraphB }, false);

            var directorObject = new GameObject("WarTrialDirector");
            directorObject.AddComponent<NetworkObject>();
            var director = directorObject.AddComponent<GodSoldierWarTrialDirector>();
            var directorSerialized = new SerializedObject(director);
            ConfigureDirectorBase(directorSerialized, "war-trial", "War Trial", new[] { soldierSpawn.transform }, new[] { godSpawn.transform });
            directorSerialized.FindProperty("soldierBossShield").objectReferenceValue = soldierBossShieldActivator;
            directorSerialized.FindProperty("godBossShield").objectReferenceValue = godBossShieldActivator;
            directorSerialized.FindProperty("soldierBossCore").objectReferenceValue = soldierBossCore;
            directorSerialized.FindProperty("godBossCore").objectReferenceValue = godBossCore;
            directorSerialized.FindProperty("waveGroupActivator").objectReferenceValue = waveGroupActivator;
            AssignObjectArray(directorSerialized.FindProperty("waveTargets"), new Object[] { waveTargetA, waveTargetB, waveTargetC });
            AssignObjectArray(directorSerialized.FindProperty("soldierAttackTelegraphs"), new Object[] { soldierTelegraphActivatorA, soldierTelegraphActivatorB });
            AssignObjectArray(directorSerialized.FindProperty("godAttackTelegraphs"), new Object[] { godTelegraphActivatorA, godTelegraphActivatorB });
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();

            SaveGeneratedScene(scene, k_WarTrialScenePath);
        }

        static void CreateJudgmentScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBootstrapperObject();
            CreateCoreDirectorService();
            CreateCamera("Judgment Scene Camera", new Vector3(14f, 14f, -20f), new Vector3(30f, -18f, 0f), false, true);
            CreateDirectionalLight();

            CreateBlock("JudgmentHall_One", new Vector3(0f, -0.5f, 0f), new Vector3(10f, 1f, 12f), new Color(0.21f, 0.22f, 0.24f));
            CreateBlock("JudgmentHall_Two", new Vector3(14f, -0.5f, 0f), new Vector3(10f, 1f, 12f), new Color(0.20f, 0.21f, 0.24f));
            CreateBlock("JudgmentHall_Three", new Vector3(28f, -0.5f, 0f), new Vector3(10f, 1f, 12f), new Color(0.20f, 0.20f, 0.24f));
            CreateBlock("JudgmentMonolith_A", new Vector3(0f, 3f, -5f), new Vector3(1.5f, 6f, 1.5f), new Color(0.17f, 0.18f, 0.24f));
            CreateBlock("JudgmentMonolith_B", new Vector3(14f, 3f, 5f), new Vector3(1.5f, 6f, 1.5f), new Color(0.17f, 0.18f, 0.24f));
            CreateBlock("JudgmentMonolith_C", new Vector3(28f, 3f, -5f), new Vector3(1.5f, 6f, 1.5f), new Color(0.17f, 0.18f, 0.24f));

            var soldierSpawn = CreateMarker("SoldierSpawn", new Vector3(-2f, 0.6f, 2f), new Vector3(0f, 20f, 0f), new Color(0.67f, 0.35f, 0.26f));
            var godSpawn = CreateMarker("GodSpawn", new Vector3(-4f, 1.5f, -2f), new Vector3(0f, 20f, 0f), new Color(0.42f, 0.58f, 0.83f));

            CreateTriggerZone("Choice1_Order", "choice_1_order", new Vector3(0f, 1f, -3f), new Vector3(2f, 2f, 2f), GodSoldierPlayerRole.None, false);
            CreateTriggerZone("Choice1_Agency", "choice_1_agency", new Vector3(0f, 1f, 3f), new Vector3(2f, 2f, 2f), GodSoldierPlayerRole.None, false);
            var secondGate = CreateBlock("SecondChamberGate", new Vector3(7f, 1.8f, 0f), new Vector3(1f, 3.5f, 8f), new Color(0.18f, 0.18f, 0.22f));
            var secondGateActivator = CreateActivator("SecondChamberGateActivator", new[] { secondGate }, true);

            CreateTriggerZone("Choice2_Order", "choice_2_order", new Vector3(14f, 1f, -3f), new Vector3(2f, 2f, 2f), GodSoldierPlayerRole.None, false);
            CreateTriggerZone("Choice2_Agency", "choice_2_agency", new Vector3(14f, 1f, 3f), new Vector3(2f, 2f, 2f), GodSoldierPlayerRole.None, false);
            var finalGate = CreateBlock("FinalChamberGate", new Vector3(21f, 1.8f, 0f), new Vector3(1f, 3.5f, 8f), new Color(0.18f, 0.18f, 0.22f));
            var finalGateActivator = CreateActivator("FinalChamberGateActivator", new[] { finalGate }, true);

            CreateTriggerZone("Final_Assassinate", "final_assassinate", new Vector3(28f, 1f, -3f), new Vector3(2.2f, 2f, 2.2f), GodSoldierPlayerRole.None, false);
            CreateTriggerZone("Final_Spare", "final_spare", new Vector3(28f, 1f, 3f), new Vector3(2.2f, 2f, 2.2f), GodSoldierPlayerRole.None, false);

            var directorObject = new GameObject("JudgmentDirector");
            directorObject.AddComponent<NetworkObject>();
            var director = directorObject.AddComponent<GodSoldierJudgmentDirector>();
            var directorSerialized = new SerializedObject(director);
            ConfigureDirectorBase(directorSerialized, "judgment", "Judgment", new[] { soldierSpawn.transform }, new[] { godSpawn.transform });
            directorSerialized.FindProperty("secondChamberGate").objectReferenceValue = secondGateActivator;
            directorSerialized.FindProperty("finalChamberGate").objectReferenceValue = finalGateActivator;
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();

            SaveGeneratedScene(scene, k_JudgmentScenePath);
        }

        static void CreateBootstrapperObject()
        {
            var bootstrapperObject = new GameObject("GodSoldier_Bootstrapper");
            var bootstrapper = bootstrapperObject.AddComponent<GodSoldierBootstrapper>();
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("networkManagerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(k_NetworkManagerPrefabPath);
            serialized.FindProperty("unityServicesPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(k_UnityServicesPrefabPath);
            serialized.FindProperty("missionCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GodSoldierMissionCatalog>(k_MissionCatalogPath);
            serialized.FindProperty("menuSceneName").stringValue = GodSoldierSceneNames.MainMenu;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateCoreDirectorService()
        {
            var gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_GameManagerPrefabPath);
            if (gameManagerPrefab == null)
            {
                new GameObject("GodSoldier_CoreDirector").AddComponent<CoreDirector>();
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(gameManagerPrefab) as GameObject;
            if (instance == null)
            {
                new GameObject("GodSoldier_CoreDirector").AddComponent<CoreDirector>();
                return;
            }

            instance.name = "GodSoldier_CoreServices";
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

            foreach (var gameManager in instance.GetComponentsInChildren<GameManager>(true))
            {
                Object.DestroyImmediate(gameManager);
            }

            if (instance.GetComponentInChildren<CoreDirector>(true) == null)
            {
                instance.AddComponent<CoreDirector>();
            }
        }

        static void CreateMenuBackdrop()
        {
            CreateBlock("CloudShelf", new Vector3(8f, -1f, 14f), new Vector3(18f, 1.2f, 16f), new Color(0.26f, 0.27f, 0.31f));
            CreateBlock("BrokenArch_Left", new Vector3(7f, 3f, 11f), new Vector3(2f, 8f, 2f), new Color(0.17f, 0.18f, 0.20f));
            CreateBlock("BrokenArch_Right", new Vector3(17f, 3f, 12f), new Vector3(2f, 8f, 2f), new Color(0.17f, 0.18f, 0.20f));
            CreateBlock("WarMonolith_A", new Vector3(3f, 4f, 18f), new Vector3(2f, 10f, 2f), new Color(0.20f, 0.21f, 0.26f));
            CreateBlock("WarMonolith_B", new Vector3(22f, 4f, 16f), new Vector3(2f, 10f, 2f), new Color(0.27f, 0.20f, 0.18f));
            CreateBlock("GlowPedestal_A", new Vector3(10f, 0.4f, 8f), new Vector3(3f, 0.8f, 3f), new Color(0.31f, 0.34f, 0.43f));
            CreateBlock("GlowPedestal_B", new Vector3(18f, 0.5f, 18f), new Vector3(4f, 1f, 4f), new Color(0.43f, 0.31f, 0.26f));
        }

        static void CreateLobbyBackdrop()
        {
            CreateBlock("MapFloor", new Vector3(0f, -0.5f, 10f), new Vector3(20f, 1f, 18f), new Color(0.20f, 0.20f, 0.22f));
            CreateBlock("StrategyTable", new Vector3(0f, 1.1f, 12f), new Vector3(6f, 2f, 4f), new Color(0.28f, 0.23f, 0.20f));
            CreateBlock("WarBanner_Left", new Vector3(-8f, 5f, 15f), new Vector3(1f, 10f, 4f), new Color(0.24f, 0.18f, 0.17f));
            CreateBlock("WarBanner_Right", new Vector3(8f, 5f, 15f), new Vector3(1f, 10f, 4f), new Color(0.18f, 0.21f, 0.29f));
            CreateBlock("StagingMonolith_A", new Vector3(-10f, 3.5f, 5f), new Vector3(2f, 7f, 2f), new Color(0.16f, 0.17f, 0.18f));
            CreateBlock("StagingMonolith_B", new Vector3(10f, 3.5f, 7f), new Vector3(2f, 7f, 2f), new Color(0.16f, 0.17f, 0.18f));
        }

        static void CreateMenuUIDocument()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_MainMenuUxmlPath);
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(k_PanelSettingsPath);
            var uiObject = new GameObject("GodSoldier_MainMenuUI");
            var document = uiObject.AddComponent<UIDocument>();
            uiObject.AddComponent<GodSoldierMenuController>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTreeAsset;
            document.sortingOrder = 100;
        }

        static void CreateLobbyUIDocument()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_LobbyUxmlPath);
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(k_PanelSettingsPath);
            var uiObject = new GameObject("GodSoldier_LobbyUI");
            var document = uiObject.AddComponent<UIDocument>();
            var controller = uiObject.AddComponent<GodSoldierLobbyController>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTreeAsset;
            document.sortingOrder = 100;

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("publicMatchTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_SessionBrowserUxmlPath);
            serialized.FindProperty("privateMatchTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_JoinByCodeUxmlPath);
            serialized.FindProperty("currentSessionTemplate").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_CurrentSessionUxmlPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        static Camera CreateCamera(string name, Vector3 position, Vector3 eulerAngles, bool tagMainCamera, bool addAudioListener)
        {
            var cameraObject = new GameObject(name);
            cameraObject.transform.position = position;
            cameraObject.transform.eulerAngles = eulerAngles;
            if (tagMainCamera)
            {
                cameraObject.tag = "MainCamera";
            }

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;

            if (addAudioListener)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            return camera;
        }

        static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(45f, -32f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.89f);
        }

        static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Color tint)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            TintRenderer(block.GetComponent<Renderer>(), tint);
            return block;
        }

        static GameObject CreateMarker(string name, Vector3 position, Vector3 eulerAngles, Color tint)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.position = position;
            marker.transform.eulerAngles = eulerAngles;
            marker.transform.localScale = new Vector3(0.75f, 0.25f, 0.75f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            TintRenderer(marker.GetComponent<Renderer>(), tint);
            return marker;
        }

        static GodSoldierMissionActionTarget CreateActionTarget(string name, string actionId, Vector3 position, Vector3 scale, GodSoldierPlayerRole requiredRole)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            target.name = name;
            target.transform.position = position;
            target.transform.localScale = scale;
            var component = target.AddComponent<GodSoldierMissionActionTarget>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("actionId").stringValue = actionId;
            serialized.FindProperty("requiredRole").enumValueIndex = (int)requiredRole;
            serialized.FindProperty("targetRenderer").objectReferenceValue = target.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        static GodSoldierMissionTriggerRelay CreateTriggerZone(string name, string triggerId, Vector3 position, Vector3 scale, GodSoldierPlayerRole requiredRole, bool hideAfterTrigger)
        {
            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = name;
            trigger.transform.position = position;
            trigger.transform.localScale = scale;
            TintRenderer(trigger.GetComponent<Renderer>(), new Color(0.62f, 0.56f, 0.34f));
            trigger.AddComponent<NetworkObject>();
            var relay = trigger.AddComponent<GodSoldierMissionTriggerRelay>();
            var serialized = new SerializedObject(relay);
            serialized.FindProperty("triggerId").stringValue = triggerId;
            serialized.FindProperty("requiredRole").enumValueIndex = (int)requiredRole;
            serialized.FindProperty("hideAfterTrigger").boolValue = hideAfterTrigger;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return relay;
        }

        static GodSoldierMissionShootableTarget CreateShootableTarget(string name, string targetId, Vector3 position, Vector3 scale, int health)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = name;
            target.transform.position = position;
            target.transform.localScale = scale;
            TintRenderer(target.GetComponent<Renderer>(), new Color(0.66f, 0.28f, 0.24f));
            target.AddComponent<NetworkObject>();
            var shootable = target.AddComponent<GodSoldierMissionShootableTarget>();
            var serialized = new SerializedObject(shootable);
            serialized.FindProperty("targetId").stringValue = targetId;
            serialized.FindProperty("health").intValue = health;
            serialized.FindProperty("targetRenderer").objectReferenceValue = target.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return shootable;
        }

        static GodSoldierReplicatedActivator CreateActivator(string name, GameObject[] targets, bool initialState)
        {
            var activatorObject = new GameObject(name);
            activatorObject.AddComponent<NetworkObject>();
            var activator = activatorObject.AddComponent<GodSoldierReplicatedActivator>();
            var serialized = new SerializedObject(activator);
            AssignObjectArray(serialized.FindProperty("targets"), targets);
            serialized.FindProperty("initialState").boolValue = initialState;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activator;
        }

        static void ConfigureDirectorBase(SerializedObject serialized, string missionId, string missionName, Transform[] soldierSpawns, Transform[] godSpawns)
        {
            serialized.FindProperty("missionId").stringValue = missionId;
            serialized.FindProperty("missionName").stringValue = missionName;
            AssignObjectArray(serialized.FindProperty("soldierSpawnPoints"), soldierSpawns);
            AssignObjectArray(serialized.FindProperty("godSpawnPoints"), godSpawns);
            serialized.FindProperty("onNotification").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NotificationEvent>(k_NotificationEventPath);
        }

        static void AssignObjectArray(SerializedProperty property, Object[] objects)
        {
            property.arraySize = objects != null ? objects.Length : 0;
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }
        }

        static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(k_BootstrapScenePath, true),
                new EditorBuildSettingsScene(k_MainMenuScenePath, true),
                new EditorBuildSettingsScene(k_LobbyScenePath, true),
                new EditorBuildSettingsScene(k_DescentScenePath, true),
                new EditorBuildSettingsScene(k_WarTrialScenePath, true),
                new EditorBuildSettingsScene(k_JudgmentScenePath, true)
            };
        }

        static void RefreshGeneratedScenesOnDisk()
        {
            var scenePaths = new[]
            {
                k_BootstrapScenePath,
                k_MainMenuScenePath,
                k_LobbyScenePath,
                k_DescentScenePath,
                k_WarTrialScenePath,
                k_JudgmentScenePath
            };

            foreach (var scenePath in scenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                RefreshSceneNetworkObjects(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        static void SaveGeneratedScene(Scene scene, string path)
        {
            RefreshSceneNetworkObjects(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        static void RefreshSceneNetworkObjects(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var networkObject in root.GetComponentsInChildren<NetworkObject>(true))
                {
                    ForceSceneNetworkObjectHash(networkObject);
                    s_NetworkObjectOnValidate?.Invoke(networkObject, null);
                    EditorUtility.SetDirty(networkObject);
                }
            }
        }

        static void ForceSceneNetworkObjectHash(NetworkObject networkObject)
        {
            if (networkObject == null || s_Hash32String == null || s_GlobalObjectIdHashField == null)
            {
                return;
            }

            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(networkObject);
            if (globalId.identifierType == 0)
            {
                return;
            }

            var hashObject = s_Hash32String.Invoke(null, new object[] { globalId.ToString() });
            if (hashObject is uint hash && hash != 0)
            {
                s_GlobalObjectIdHashField.SetValue(networkObject, hash);
            }
        }

        static void TintRenderer(Renderer renderer, Color tint)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            var material = new Material(renderer.sharedMaterial);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            renderer.sharedMaterial = material;
        }
    }
}
