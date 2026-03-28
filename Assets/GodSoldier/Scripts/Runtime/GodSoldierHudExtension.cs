using Blocks.Gameplay.Core;
using Blocks.Gameplay.Shooter;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace GodSoldier
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(CorePlayerState))]
    public class GodSoldierHudExtension : NetworkBehaviour
    {
        UIDocument m_Document;
        CorePlayerState m_PlayerState;
        CorePlayerManager m_PlayerManager;
        ShooterInputHandler m_ShooterInputHandler;

        Label m_RoleLabel;
        Label m_MissionLabel;
        Label m_ObjectiveLabel;
        Label m_StatusLabel;
        VisualElement m_StoryOverlay;
        Label m_StoryTitle;
        Label m_StoryBody;
        VisualElement m_PauseOverlay;
        Label m_PauseStatusLabel;
        VisualElement m_Root;

        bool m_IsPaused;

        void Awake()
        {
            m_Document = GetComponent<UIDocument>();
            m_PlayerState = GetComponent<CorePlayerState>();
            m_PlayerManager = GetComponent<CorePlayerManager>();
            m_ShooterInputHandler = GetComponent<ShooterInputHandler>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner || m_Document == null)
            {
                enabled = false;
                return;
            }

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            TryEnsureOverlay();
        }

        public override void OnNetworkDespawn()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            base.OnNetworkDespawn();
        }

        void Update()
        {
            if (!IsOwner || m_Document == null)
            {
                return;
            }

            if (IsGameplayScene(SceneManager.GetActiveScene().name) &&
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetPaused(!m_IsPaused);
            }

            TryEnsureOverlay();
            Refresh();
        }

        void TryEnsureOverlay()
        {
            var root = m_Document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            bool shouldShow = IsGameplayScene(SceneManager.GetActiveScene().name);
            root.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;

            if (!shouldShow)
            {
                if (m_IsPaused)
                {
                    SetPaused(false);
                }

                if (m_StoryOverlay != null)
                {
                    m_StoryOverlay.style.display = DisplayStyle.None;
                }

                if (m_PauseOverlay != null)
                {
                    m_PauseOverlay.style.display = DisplayStyle.None;
                }

                return;
            }

            bool needsOverlay = m_Root != root || m_RoleLabel == null || m_RoleLabel.panel == null || m_PauseOverlay == null;
            if (!needsOverlay)
            {
                return;
            }

            m_Root = root;
            CreateOverlay(root);
        }

        void CreateOverlay(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.Q<VisualElement>("godsoldier-side-panel")?.RemoveFromHierarchy();
            root.Q<VisualElement>("godsoldier-story-overlay")?.RemoveFromHierarchy();
            root.Q<VisualElement>("godsoldier-pause-overlay")?.RemoveFromHierarchy();

            var sidePanel = new VisualElement();
            sidePanel.name = "godsoldier-side-panel";
            sidePanel.style.position = Position.Absolute;
            sidePanel.style.top = 14;
            sidePanel.style.right = 14;
            sidePanel.style.width = 282;
            sidePanel.style.paddingTop = 10;
            sidePanel.style.paddingBottom = 10;
            sidePanel.style.paddingLeft = 12;
            sidePanel.style.paddingRight = 12;
            sidePanel.style.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 0.68f);
            sidePanel.style.borderTopLeftRadius = 14;
            sidePanel.style.borderTopRightRadius = 14;
            sidePanel.style.borderBottomLeftRadius = 14;
            sidePanel.style.borderBottomRightRadius = 14;

            m_RoleLabel = new Label();
            m_RoleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_RoleLabel.style.fontSize = 13;
            m_RoleLabel.style.color = new Color(0.96f, 0.96f, 0.96f);

            m_MissionLabel = new Label();
            m_MissionLabel.style.marginTop = 3;
            m_MissionLabel.style.fontSize = 12;
            m_MissionLabel.style.color = new Color(0.98f, 0.85f, 0.63f);

            m_ObjectiveLabel = new Label();
            m_ObjectiveLabel.style.whiteSpace = WhiteSpace.Normal;
            m_ObjectiveLabel.style.marginTop = 5;
            m_ObjectiveLabel.style.fontSize = 12;
            m_ObjectiveLabel.style.color = new Color(0.84f, 0.87f, 0.92f);

            m_StatusLabel = new Label();
            m_StatusLabel.style.marginTop = 6;
            m_StatusLabel.style.whiteSpace = WhiteSpace.Normal;
            m_StatusLabel.style.fontSize = 11;
            m_StatusLabel.style.color = new Color(0.73f, 0.84f, 0.98f);

            sidePanel.Add(m_RoleLabel);
            sidePanel.Add(m_MissionLabel);
            sidePanel.Add(m_ObjectiveLabel);
            sidePanel.Add(m_StatusLabel);

            m_StoryOverlay = new VisualElement();
            m_StoryOverlay.name = "godsoldier-story-overlay";
            m_StoryOverlay.style.position = Position.Absolute;
            m_StoryOverlay.style.left = 0;
            m_StoryOverlay.style.right = 0;
            m_StoryOverlay.style.top = 0;
            m_StoryOverlay.style.bottom = 0;
            m_StoryOverlay.style.display = DisplayStyle.None;
            m_StoryOverlay.style.justifyContent = Justify.Center;
            m_StoryOverlay.style.alignItems = Align.Center;
            m_StoryOverlay.style.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 0.72f);
            m_StoryOverlay.pickingMode = PickingMode.Ignore;

            var storyCard = new VisualElement();
            storyCard.style.width = 560;
            storyCard.style.maxWidth = new Length(74, LengthUnit.Percent);
            storyCard.style.paddingTop = 20;
            storyCard.style.paddingBottom = 20;
            storyCard.style.paddingLeft = 24;
            storyCard.style.paddingRight = 24;
            storyCard.style.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 0.92f);
            storyCard.style.borderTopLeftRadius = 20;
            storyCard.style.borderTopRightRadius = 20;
            storyCard.style.borderBottomLeftRadius = 20;
            storyCard.style.borderBottomRightRadius = 20;

            m_StoryTitle = new Label();
            m_StoryTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_StoryTitle.style.fontSize = 22;
            m_StoryTitle.style.color = new Color(0.98f, 0.9f, 0.71f);

            m_StoryBody = new Label();
            m_StoryBody.style.whiteSpace = WhiteSpace.Normal;
            m_StoryBody.style.marginTop = 8;
            m_StoryBody.style.fontSize = 14;
            m_StoryBody.style.color = new Color(0.93f, 0.94f, 0.96f);

            storyCard.Add(m_StoryTitle);
            storyCard.Add(m_StoryBody);
            m_StoryOverlay.Add(storyCard);

            m_PauseOverlay = new VisualElement();
            m_PauseOverlay.name = "godsoldier-pause-overlay";
            m_PauseOverlay.style.position = Position.Absolute;
            m_PauseOverlay.style.left = 0;
            m_PauseOverlay.style.right = 0;
            m_PauseOverlay.style.top = 0;
            m_PauseOverlay.style.bottom = 0;
            m_PauseOverlay.style.display = DisplayStyle.None;
            m_PauseOverlay.style.justifyContent = Justify.Center;
            m_PauseOverlay.style.alignItems = Align.Center;
            m_PauseOverlay.style.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 0.78f);

            var pauseCard = new VisualElement();
            pauseCard.style.width = 320;
            pauseCard.style.maxWidth = new Length(72, LengthUnit.Percent);
            pauseCard.style.paddingTop = 18;
            pauseCard.style.paddingBottom = 18;
            pauseCard.style.paddingLeft = 20;
            pauseCard.style.paddingRight = 20;
            pauseCard.style.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 0.96f);
            pauseCard.style.borderTopLeftRadius = 18;
            pauseCard.style.borderTopRightRadius = 18;
            pauseCard.style.borderBottomLeftRadius = 18;
            pauseCard.style.borderBottomRightRadius = 18;

            var pauseTitle = new Label("Paused");
            pauseTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            pauseTitle.style.fontSize = 22;
            pauseTitle.style.color = new Color(0.98f, 0.9f, 0.71f);

            m_PauseStatusLabel = new Label("Gameplay input is suspended.");
            m_PauseStatusLabel.style.marginTop = 8;
            m_PauseStatusLabel.style.whiteSpace = WhiteSpace.Normal;
            m_PauseStatusLabel.style.fontSize = 12;
            m_PauseStatusLabel.style.color = new Color(0.90f, 0.92f, 0.96f);

            var pauseHint = new Label("Press Esc to resume.");
            pauseHint.style.marginTop = 6;
            pauseHint.style.fontSize = 11;
            pauseHint.style.color = new Color(0.72f, 0.78f, 0.86f);

            var resumeButton = CreatePauseButton("Resume");
            resumeButton.style.marginTop = 14;
            resumeButton.clicked += () => SetPaused(false);

            var menuButton = CreatePauseButton("Return To Menu");
            menuButton.style.marginTop = 8;
            menuButton.clicked += ReturnToMenu;

            pauseCard.Add(pauseTitle);
            pauseCard.Add(m_PauseStatusLabel);
            pauseCard.Add(pauseHint);
            pauseCard.Add(resumeButton);
            pauseCard.Add(menuButton);
            m_PauseOverlay.Add(pauseCard);

            root.Add(sidePanel);
            root.Add(m_StoryOverlay);
            root.Add(m_PauseOverlay);

            UpdatePauseOverlay();
        }

        Button CreatePauseButton(string text)
        {
            var button = new Button
            {
                text = text
            };
            button.style.minHeight = 36;
            button.style.borderTopLeftRadius = 12;
            button.style.borderTopRightRadius = 12;
            button.style.borderBottomLeftRadius = 12;
            button.style.borderBottomRightRadius = 12;
            button.style.backgroundColor = new Color(0.90f, 0.68f, 0.42f, 0.95f);
            button.style.color = new Color(0.08f, 0.09f, 0.12f, 1f);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            return button;
        }

        void Refresh()
        {
            if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            if (m_PlayerState == null || m_RoleLabel == null)
            {
                return;
            }

            m_RoleLabel.text = m_PlayerState.PlayerRole switch
            {
                GodSoldierPlayerRole.God => "Role: God",
                GodSoldierPlayerRole.Soldier => "Role: Soldier",
                _ => "Role: Unassigned"
            };

            var director = GodSoldierMissionDirectorBase.Current;
            if (director == null)
            {
                m_MissionLabel.text = "Mission: Awaiting deployment";
                m_ObjectiveLabel.text = "Objective: Join a session and wait for the game to begin.";
                m_StatusLabel.text = "Status: Stand by.";
                if (m_StoryOverlay != null)
                {
                    m_StoryOverlay.style.display = DisplayStyle.None;
                }

                UpdatePauseOverlay();
                return;
            }

            var snapshot = director.GetHudSnapshot();
            m_MissionLabel.text = $"Mission: {snapshot.missionName}";
            m_ObjectiveLabel.text = $"Objective: {snapshot.objectiveText}";
            m_StatusLabel.text = string.IsNullOrWhiteSpace(snapshot.statusText)
                ? "Status: Keep moving."
                : $"Status: {snapshot.statusText}";

            if (m_PauseStatusLabel != null)
            {
                m_PauseStatusLabel.text = string.IsNullOrWhiteSpace(snapshot.missionName)
                    ? "Gameplay input is suspended."
                    : $"{snapshot.missionName}\nInput is suspended for this client.";
            }

            if (m_StoryOverlay != null)
            {
                m_StoryOverlay.style.display = snapshot.storyVisible && !m_IsPaused ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (snapshot.storyVisible)
            {
                m_StoryTitle.text = snapshot.storyTitle;
                m_StoryBody.text = snapshot.storyBody;
            }

            UpdatePauseOverlay();
        }

        void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (!IsGameplayScene(nextScene.name) && m_IsPaused)
            {
                SetPaused(false);
            }

            TryEnsureOverlay();
            Refresh();
        }

        void SetPaused(bool paused)
        {
            if (m_IsPaused == paused)
            {
                return;
            }

            m_IsPaused = paused;
            ApplyPauseState();
            UpdatePauseOverlay();
        }

        void ApplyPauseState()
        {
            bool shouldEnableGameplay = !m_IsPaused &&
                IsGameplayScene(SceneManager.GetActiveScene().name) &&
                m_PlayerState != null &&
                m_PlayerState.IsActive;

            if (m_PlayerManager != null)
            {
                m_PlayerManager.SetMovementInputEnabled(shouldEnableGameplay);

                if (m_PlayerManager.CoreInput != null)
                {
                    m_PlayerManager.CoreInput.enabled = shouldEnableGameplay;
                }

                if (m_PlayerManager.CoreCamera != null)
                {
                    m_PlayerManager.CoreCamera.enabled = shouldEnableGameplay;
                    if (shouldEnableGameplay && m_PlayerManager.CoreCamera.ActiveCameraMode != null)
                    {
                        m_PlayerManager.CoreCamera.SwitchCameraMode(m_PlayerManager.CoreCamera.ActiveCameraMode.ModeName);
                    }
                }

                if (m_PlayerManager.CoreMovement != null)
                {
                    m_PlayerManager.CoreMovement.IsMovementEnabled = shouldEnableGameplay;
                    if (!shouldEnableGameplay)
                    {
                        m_PlayerManager.CoreMovement.ResetMovementForces();
                    }
                    else if (m_PlayerManager.CoreCamera != null)
                    {
                        m_PlayerManager.CoreMovement.PlayerRotationMode = m_PlayerManager.CoreCamera.CurrentPlayerRotationMode;
                    }
                }
            }

            if (m_ShooterInputHandler != null)
            {
                m_ShooterInputHandler.enabled = shouldEnableGameplay;
            }

            Cursor.lockState = shouldEnableGameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldEnableGameplay;
        }

        void UpdatePauseOverlay()
        {
            if (m_PauseOverlay != null)
            {
                m_PauseOverlay.style.display = m_IsPaused ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void ReturnToMenu()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            SceneManager.LoadScene(GodSoldierSceneNames.MainMenu);
        }

        static bool IsGameplayScene(string sceneName)
        {
            return sceneName != GodSoldierSceneNames.Bootstrap &&
                   sceneName != GodSoldierSceneNames.MainMenu &&
                   sceneName != GodSoldierSceneNames.Lobby;
        }
    }
}
