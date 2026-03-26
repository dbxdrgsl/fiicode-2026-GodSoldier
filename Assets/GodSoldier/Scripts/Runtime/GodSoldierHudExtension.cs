using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GodSoldier
{
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(CorePlayerState))]
    public class GodSoldierHudExtension : NetworkBehaviour
    {
        UIDocument m_Document;
        CorePlayerState m_PlayerState;
        Label m_RoleLabel;
        Label m_MissionLabel;
        Label m_ObjectiveLabel;
        Label m_StatusLabel;
        VisualElement m_StoryOverlay;
        Label m_StoryTitle;
        Label m_StoryBody;
        VisualElement m_Root;

        void Awake()
        {
            m_Document = GetComponent<UIDocument>();
            m_PlayerState = GetComponent<CorePlayerState>();
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
                if (m_StoryOverlay != null)
                {
                    m_StoryOverlay.style.display = DisplayStyle.None;
                }

                return;
            }

            bool needsOverlay = m_Root != root || m_RoleLabel == null || m_RoleLabel.panel == null;
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

            var sidePanel = new VisualElement();
            sidePanel.name = "godsoldier-side-panel";
            sidePanel.style.position = Position.Absolute;
            sidePanel.style.top = 18;
            sidePanel.style.right = 18;
            sidePanel.style.width = 360;
            sidePanel.style.paddingTop = 14;
            sidePanel.style.paddingBottom = 14;
            sidePanel.style.paddingLeft = 16;
            sidePanel.style.paddingRight = 16;
            sidePanel.style.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 0.82f);
            sidePanel.style.borderTopLeftRadius = 16;
            sidePanel.style.borderTopRightRadius = 16;
            sidePanel.style.borderBottomLeftRadius = 16;
            sidePanel.style.borderBottomRightRadius = 16;

            m_RoleLabel = new Label();
            m_RoleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_RoleLabel.style.fontSize = 18;
            m_RoleLabel.style.color = new Color(0.95f, 0.95f, 0.95f);

            m_MissionLabel = new Label();
            m_MissionLabel.style.marginTop = 4;
            m_MissionLabel.style.color = new Color(0.98f, 0.85f, 0.63f);

            m_ObjectiveLabel = new Label();
            m_ObjectiveLabel.style.whiteSpace = WhiteSpace.Normal;
            m_ObjectiveLabel.style.marginTop = 6;
            m_ObjectiveLabel.style.color = new Color(0.85f, 0.87f, 0.92f);

            m_StatusLabel = new Label();
            m_StatusLabel.style.marginTop = 8;
            m_StatusLabel.style.whiteSpace = WhiteSpace.Normal;
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
            m_StoryOverlay.style.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 0.84f);
            m_StoryOverlay.pickingMode = PickingMode.Ignore;

            var storyCard = new VisualElement();
            storyCard.style.width = 720;
            storyCard.style.maxWidth = new Length(80, LengthUnit.Percent);
            storyCard.style.paddingTop = 28;
            storyCard.style.paddingBottom = 28;
            storyCard.style.paddingLeft = 32;
            storyCard.style.paddingRight = 32;
            storyCard.style.backgroundColor = new Color(0.1f, 0.11f, 0.14f, 0.95f);
            storyCard.style.borderTopLeftRadius = 24;
            storyCard.style.borderTopRightRadius = 24;
            storyCard.style.borderBottomLeftRadius = 24;
            storyCard.style.borderBottomRightRadius = 24;

            m_StoryTitle = new Label();
            m_StoryTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_StoryTitle.style.fontSize = 28;
            m_StoryTitle.style.color = new Color(0.98f, 0.9f, 0.71f);

            m_StoryBody = new Label();
            m_StoryBody.style.whiteSpace = WhiteSpace.Normal;
            m_StoryBody.style.marginTop = 10;
            m_StoryBody.style.fontSize = 18;
            m_StoryBody.style.color = new Color(0.93f, 0.94f, 0.96f);

            storyCard.Add(m_StoryTitle);
            storyCard.Add(m_StoryBody);
            m_StoryOverlay.Add(storyCard);

            root.Add(sidePanel);
            root.Add(m_StoryOverlay);
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
                m_StatusLabel.text = "Status: Select a mission from the timeline.";
                m_StoryOverlay.style.display = DisplayStyle.None;
                return;
            }

            var snapshot = director.GetHudSnapshot();
            m_MissionLabel.text = $"Mission: {snapshot.missionName}";
            m_ObjectiveLabel.text = $"Objective: {snapshot.objectiveText}";
            m_StatusLabel.text = string.IsNullOrWhiteSpace(snapshot.statusText)
                ? "Status: Stay coordinated."
                : $"Status: {snapshot.statusText}";

            m_StoryOverlay.style.display = snapshot.storyVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (snapshot.storyVisible)
            {
                m_StoryTitle.text = snapshot.storyTitle;
                m_StoryBody.text = snapshot.storyBody;
            }
        }

        void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            TryEnsureOverlay();
            Refresh();
        }

        static bool IsGameplayScene(string sceneName)
        {
            return sceneName != GodSoldierSceneNames.Bootstrap &&
                   sceneName != GodSoldierSceneNames.MainMenu &&
                   sceneName != GodSoldierSceneNames.Lobby;
        }
    }
}
