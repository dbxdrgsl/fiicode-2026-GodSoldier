using System;
using System.Linq;
using System.Threading.Tasks;
using Blocks.Gameplay.Core;
using Blocks.Sessions.Common;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GodSoldier
{
    [RequireComponent(typeof(UIDocument))]
    public class GodSoldierLobbyController : MonoBehaviour
    {
        enum MatchMode
        {
            Public,
            Private
        }

        [SerializeField] VisualTreeAsset publicMatchTemplate;
        [SerializeField] VisualTreeAsset privateMatchTemplate;
        [SerializeField] VisualTreeAsset currentSessionTemplate;

        UIDocument m_Document;
        GodSoldierMissionDefinition m_SelectedMission;
        MatchMode m_MatchMode = MatchMode.Private;

        VisualElement m_RoomStage;
        VisualElement m_CharacterStage;
        Label m_MissionTitleLabel;
        Label m_MissionHeadlineLabel;
        Label m_MissionDescriptionLabel;
        Label m_RecommendedOrderLabel;
        Label m_RoomStatusLabel;
        Label m_RoomPlayerCountLabel;
        Label m_CharacterMissionLabel;
        Label m_CharacterHeadlineLabel;
        Label m_CharacterStatusLabel;
        Label m_CharacterHintLabel;
        Label m_PlayerRolesLabel;
        Label m_CharacterCustomizationLabel;
        VisualElement m_MatchPanelRoot;
        VisualElement m_CurrentSessionRoot;
        Button m_PublicMatchButton;
        Button m_PrivateMatchButton;
        Button m_RoleGodButton;
        Button m_RoleSoldierButton;
        Button m_StartButton;
        Button m_RoomBackButton;
        Button m_CharacterLeaveButton;

        GodSoldierPlayerRole m_PendingRoleRequest = GodSoldierPlayerRole.None;

        void Awake()
        {
            m_Document = GetComponent<UIDocument>();
        }

        void Start()
        {
            var root = m_Document.rootVisualElement;
            QueryUi(root);
            RegisterUiCallbacks();
            ResolveSelectedMission();
            BuildMissionMatchUi();
            RefreshUi();
        }

        void Update()
        {
            TryApplyPendingRole();
            RefreshUi();
        }

        void QueryUi(VisualElement root)
        {
            m_RoomStage = root.Q<VisualElement>("room-stage");
            m_CharacterStage = root.Q<VisualElement>("character-stage");

            m_MissionTitleLabel = root.Q<Label>("mission-title-label");
            m_MissionHeadlineLabel = root.Q<Label>("mission-headline-label");
            m_MissionDescriptionLabel = root.Q<Label>("mission-description-label");
            m_RecommendedOrderLabel = root.Q<Label>("recommended-order-label");
            m_RoomStatusLabel = root.Q<Label>("room-status-label");
            m_RoomPlayerCountLabel = root.Q<Label>("room-player-count-label");

            m_CharacterMissionLabel = root.Q<Label>("character-mission-label");
            m_CharacterHeadlineLabel = root.Q<Label>("character-headline-label");
            m_CharacterStatusLabel = root.Q<Label>("character-status-label");
            m_CharacterHintLabel = root.Q<Label>("character-hint-label");
            m_PlayerRolesLabel = root.Q<Label>("player-roles-label");
            m_CharacterCustomizationLabel = root.Q<Label>("character-customization-label");

            m_MatchPanelRoot = root.Q<VisualElement>("match-panel-root");
            m_CurrentSessionRoot = root.Q<VisualElement>("current-session-root");

            m_PublicMatchButton = root.Q<Button>("public-match-button");
            m_PrivateMatchButton = root.Q<Button>("private-match-button");
            m_RoleGodButton = root.Q<Button>("role-god-button");
            m_RoleSoldierButton = root.Q<Button>("role-soldier-button");
            m_StartButton = root.Q<Button>("start-button");
            m_RoomBackButton = root.Q<Button>("room-back-button");
            m_CharacterLeaveButton = root.Q<Button>("character-leave-button");
        }

        void RegisterUiCallbacks()
        {
            m_PublicMatchButton?.RegisterCallback<ClickEvent>(_ => SetMatchMode(MatchMode.Public));
            m_PrivateMatchButton?.RegisterCallback<ClickEvent>(_ => SetMatchMode(MatchMode.Private));
            m_RoleGodButton?.RegisterCallback<ClickEvent>(_ => AssignLocalRole(GodSoldierPlayerRole.God));
            m_RoleSoldierButton?.RegisterCallback<ClickEvent>(_ => AssignLocalRole(GodSoldierPlayerRole.Soldier));
            m_StartButton?.RegisterCallback<ClickEvent>(_ => TryStartMission());
            m_RoomBackButton?.RegisterCallback<ClickEvent>(_ => ReturnToMenu());
            m_CharacterLeaveButton?.RegisterCallback<ClickEvent>(_ => ReturnToMenu());
        }

        void ResolveSelectedMission()
        {
            var flowState = GodSoldierGameFlowState.Instance;
            m_SelectedMission = flowState?.SelectedMission ?? flowState?.GetRecommendedMission();

            if (m_SelectedMission == null && flowState != null)
            {
                m_SelectedMission = flowState.GetMissionsInOrder().FirstOrDefault();
            }

            if (m_SelectedMission == null)
            {
                return;
            }

            if (!m_SelectedMission.AllowPrivateMatch && m_SelectedMission.AllowPublicMatch)
            {
                m_MatchMode = MatchMode.Public;
            }
            else if (!m_SelectedMission.AllowPublicMatch && m_SelectedMission.AllowPrivateMatch)
            {
                m_MatchMode = MatchMode.Private;
            }
        }

        void BuildMissionMatchUi()
        {
            string missionName = m_SelectedMission?.DisplayName ?? "Mission";
            string headline = m_SelectedMission?.Headline ?? "Create or join a room.";
            string description = m_SelectedMission?.Description ?? "Build the room first, then select characters when the full pair is present.";
            string orderLabel = m_SelectedMission != null
                ? $"Timeline position: Mission {m_SelectedMission.RecommendedOrder}"
                : "Timeline position unavailable";

            if (m_MissionTitleLabel != null) m_MissionTitleLabel.text = missionName;
            if (m_MissionHeadlineLabel != null) m_MissionHeadlineLabel.text = headline;
            if (m_MissionDescriptionLabel != null) m_MissionDescriptionLabel.text = description;
            if (m_RecommendedOrderLabel != null) m_RecommendedOrderLabel.text = orderLabel;

            if (m_CharacterMissionLabel != null) m_CharacterMissionLabel.text = missionName;
            if (m_CharacterHeadlineLabel != null) m_CharacterHeadlineLabel.text = headline;
            if (m_CharacterCustomizationLabel != null)
            {
                m_CharacterCustomizationLabel.text =
                    "Future updates: loadouts, attached relics, weapon choices, and cosmetic customization will live here. For now, lock one God and one Soldier.";
            }

            RebuildMatchPanels();
            UpdateMatchModeButtons(NetworkManager.Singleton);
        }

        void RebuildMatchPanels()
        {
            if (m_MatchPanelRoot != null)
            {
                m_MatchPanelRoot.Clear();
                var activeTemplate = GetActiveMatchTemplate();
                var sessionSettings = GetActiveSessionSettings();
                if (activeTemplate != null)
                {
                    var matchPanel = new VisualElement();
                    activeTemplate.CloneTree(matchPanel);
                    matchPanel.dataSource = sessionSettings;
                    m_MatchPanelRoot.Add(matchPanel);
                }
            }

            if (m_CurrentSessionRoot != null)
            {
                m_CurrentSessionRoot.Clear();
                var sessionSettings = GetActiveSessionSettings();
                if (currentSessionTemplate != null)
                {
                    var currentSessionPanel = new VisualElement();
                    currentSessionTemplate.CloneTree(currentSessionPanel);
                    currentSessionPanel.dataSource = sessionSettings;
                    m_CurrentSessionRoot.Add(currentSessionPanel);
                }
            }
        }

        void SetMatchMode(MatchMode mode)
        {
            var manager = NetworkManager.Singleton;
            if (manager != null && manager.IsListening)
            {
                return;
            }

            if (!IsModeAllowed(mode))
            {
                return;
            }

            if (m_MatchMode == mode && m_MatchPanelRoot != null && m_MatchPanelRoot.childCount > 0)
            {
                return;
            }

            m_MatchMode = mode;
            RebuildMatchPanels();
            UpdateMatchModeButtons(manager);
            RefreshUi();
        }

        void RefreshUi()
        {
            var manager = NetworkManager.Singleton;
            var localPlayer = GetLocalPlayerState(manager);

            bool showCharacterStage = IsCharacterStageUnlocked(manager);
            SetActiveStage(showCharacterStage);
            UpdateMatchModeButtons(manager);
            UpdateRoomStage(manager);
            UpdateCharacterStage(manager, localPlayer);
        }

        void SetActiveStage(bool showCharacterStage)
        {
            if (m_RoomStage != null)
            {
                m_RoomStage.style.display = showCharacterStage ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (m_CharacterStage != null)
            {
                m_CharacterStage.style.display = showCharacterStage ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void UpdateRoomStage(NetworkManager manager)
        {
            int connectedPlayers = GetConnectedPlayerCount(manager);
            int maxPlayers = GetExpectedHumanPlayers();

            if (m_RoomStatusLabel != null)
            {
                m_RoomStatusLabel.text = BuildRoomStatus(manager, connectedPlayers, maxPlayers);
            }

            if (m_RoomPlayerCountLabel != null)
            {
                m_RoomPlayerCountLabel.text = $"Players in room: {connectedPlayers} / {maxPlayers}";
            }
        }

        void UpdateCharacterStage(NetworkManager manager, CorePlayerState localPlayer)
        {
            if (m_CharacterStatusLabel != null)
            {
                m_CharacterStatusLabel.text = BuildCharacterStatus(manager, localPlayer);
            }

            if (m_CharacterHintLabel != null)
            {
                m_CharacterHintLabel.text = BuildCharacterHint(manager, localPlayer);
            }

            if (m_PlayerRolesLabel != null)
            {
                m_PlayerRolesLabel.text = BuildPlayerRolesSummary(manager, localPlayer);
            }

            UpdateRoleButtons(manager, localPlayer);
            UpdateStartButton(manager);
        }

        void UpdateMatchModeButtons(NetworkManager manager)
        {
            bool roomLocked = manager != null && manager.IsListening;

            if (m_PublicMatchButton != null)
            {
                bool isSelected = m_MatchMode == MatchMode.Public;
                m_PublicMatchButton.EnableInClassList("match-mode-button--selected", isSelected);
                m_PublicMatchButton.SetEnabled(!roomLocked && IsModeAllowed(MatchMode.Public));
            }

            if (m_PrivateMatchButton != null)
            {
                bool isSelected = m_MatchMode == MatchMode.Private;
                m_PrivateMatchButton.EnableInClassList("match-mode-button--selected", isSelected);
                m_PrivateMatchButton.SetEnabled(!roomLocked && IsModeAllowed(MatchMode.Private));
            }
        }

        void UpdateRoleButtons(NetworkManager manager, CorePlayerState localPlayer)
        {
            UpdateRoleButton(m_RoleGodButton, GodSoldierPlayerRole.God, manager, localPlayer);
            UpdateRoleButton(m_RoleSoldierButton, GodSoldierPlayerRole.Soldier, manager, localPlayer);
        }

        void UpdateRoleButton(Button button, GodSoldierPlayerRole role, NetworkManager manager, CorePlayerState localPlayer)
        {
            if (button == null)
            {
                return;
            }

            bool stageUnlocked = IsCharacterStageUnlocked(manager);
            bool selected = localPlayer != null && localPlayer.PlayerRole == role;
            bool pending = m_PendingRoleRequest == role && !selected;
            bool takenByOther = IsRoleTakenByOther(manager, localPlayer, role);

            button.EnableInClassList("role-button--selected", selected);
            button.EnableInClassList("role-button--pending", pending);
            button.EnableInClassList("role-button--taken", takenByOther);
            button.SetEnabled(stageUnlocked && !takenByOther);

            button.text = BuildRoleButtonText(role, selected, pending, takenByOther);
        }

        void UpdateStartButton(NetworkManager manager)
        {
            if (m_StartButton == null)
            {
                return;
            }

            bool sessionReady = ArePlayersReady(manager);
            bool canStart = manager != null && manager.IsListening && manager.IsHost && sessionReady;

            m_StartButton.SetEnabled(canStart);

            if (manager == null || !manager.IsListening)
            {
                m_StartButton.text = "Start Mission";
            }
            else if (!manager.IsHost)
            {
                m_StartButton.text = sessionReady ? "Waiting For Host" : "Waiting For Roles";
            }
            else
            {
                m_StartButton.text = sessionReady ? "Start Mission" : "Lock Roles To Start";
            }
        }

        void AssignLocalRole(GodSoldierPlayerRole role)
        {
            var manager = NetworkManager.Singleton;
            if (!IsCharacterStageUnlocked(manager))
            {
                return;
            }

            var localPlayer = GetLocalPlayerState(manager);
            if (IsRoleTakenByOther(manager, localPlayer, role))
            {
                return;
            }

            m_PendingRoleRequest = role;

            if (localPlayer != null)
            {
                localPlayer.SetPlayerRole(role);
            }

            RefreshUi();
        }

        void TryApplyPendingRole()
        {
            if (m_PendingRoleRequest == GodSoldierPlayerRole.None)
            {
                return;
            }

            var manager = NetworkManager.Singleton;
            if (!IsCharacterStageUnlocked(manager))
            {
                return;
            }

            var localPlayer = GetLocalPlayerState(manager);
            if (localPlayer == null)
            {
                return;
            }

            if (IsRoleTakenByOther(manager, localPlayer, m_PendingRoleRequest))
            {
                m_PendingRoleRequest = GodSoldierPlayerRole.None;
                return;
            }

            if (localPlayer.PlayerRole == m_PendingRoleRequest)
            {
                m_PendingRoleRequest = GodSoldierPlayerRole.None;
                return;
            }

            localPlayer.SetPlayerRole(m_PendingRoleRequest);
        }

        bool ArePlayersReady(NetworkManager manager)
        {
            if (!IsCharacterStageUnlocked(manager))
            {
                return false;
            }

            bool hasGod = false;
            bool hasSoldier = false;

            foreach (var playerState in EnumerateConnectedPlayerStates(manager))
            {
                switch (playerState.PlayerRole)
                {
                    case GodSoldierPlayerRole.God:
                        hasGod = true;
                        break;
                    case GodSoldierPlayerRole.Soldier:
                        hasSoldier = true;
                        break;
                    default:
                        return false;
                }
            }

            return hasGod && hasSoldier;
        }

        bool IsCharacterStageUnlocked(NetworkManager manager)
        {
            return manager != null &&
                manager.IsListening &&
                GetConnectedPlayerCount(manager) >= GetExpectedHumanPlayers();
        }

        bool IsRoleTakenByOther(NetworkManager manager, CorePlayerState localPlayer, GodSoldierPlayerRole role)
        {
            foreach (var playerState in EnumerateConnectedPlayerStates(manager))
            {
                if (playerState.PlayerRole != role)
                {
                    continue;
                }

                if (localPlayer != null && playerState.OwnerClientId == localPlayer.OwnerClientId)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        string BuildRoomStatus(NetworkManager manager, int connectedPlayers, int maxPlayers)
        {
            if (manager == null || !manager.IsListening)
            {
                return $"Create or join a {(m_MatchMode == MatchMode.Public ? "public" : "private")} room for {m_SelectedMission?.DisplayName ?? "the mission"}. Character selection opens after both players are inside the same room.";
            }

            if (connectedPlayers < maxPlayers)
            {
                return "Room is live. Waiting for the full two-player bond before character selection opens.";
            }

            return "Both players are connected. Moving into character selection.";
        }

        string BuildCharacterStatus(NetworkManager manager, CorePlayerState localPlayer)
        {
            if (manager == null || !manager.IsListening)
            {
                return "Join or host a room before selecting a class.";
            }

            if (GetConnectedPlayerCount(manager) < GetExpectedHumanPlayers())
            {
                return "Character selection unlocks once both players are in the room.";
            }

            if (m_PendingRoleRequest != GodSoldierPlayerRole.None && localPlayer == null)
            {
                return "Role selection is queued while your network avatar finishes spawning.";
            }

            if (!ArePlayersReady(manager))
            {
                return "Both players are in. Lock one God and one Soldier to continue.";
            }

            return manager.IsHost
                ? "Roles are locked. Host can start the mission."
                : "Roles are locked. Waiting for the host to start the mission.";
        }

        string BuildCharacterHint(NetworkManager manager, CorePlayerState localPlayer)
        {
            if (manager == null || !manager.IsListening)
            {
                return "The room is resolved first. Character selection only appears after the room is active.";
            }

            if (GetConnectedPlayerCount(manager) < GetExpectedHumanPlayers())
            {
                return "Stay in the room. When the second player joins, the game opens character selection automatically.";
            }

            if (localPlayer == null)
            {
                return "Your player object is still syncing. You can already queue a class selection.";
            }

            if (localPlayer.PlayerRole == GodSoldierPlayerRole.None)
            {
                return "Choose a class. This screen is also where future loadouts, attachments, and visual customization will live.";
            }

            return localPlayer.PlayerRole == GodSoldierPlayerRole.God
                ? "You are locked as God. Support, reveal, crafting, and future relic customization will branch from this slot."
                : "You are locked as Soldier. Weapons, attachments, and physical loadout upgrades will branch from this slot.";
        }

        string BuildPlayerRolesSummary(NetworkManager manager, CorePlayerState localPlayer)
        {
            if (manager == null || !manager.IsListening)
            {
                return "Connected players\n- No room active";
            }

            var lines = EnumerateConnectedPlayerStates(manager)
                .OrderBy(state => state.OwnerClientId)
                .Select(state =>
                {
                    string ownerLabel = state.OwnerClientId == manager.LocalClientId ? "You" : $"Player {state.OwnerClientId}";
                    string hostLabel = state.OwnerClientId == NetworkManager.ServerClientId ? "Host" : "Guest";
                    string roleLabel = state.PlayerRole == GodSoldierPlayerRole.None ? "Unselected" : state.PlayerRole.ToString();
                    return $"- {ownerLabel} ({hostLabel}): {roleLabel}";
                })
                .ToList();

            if (manager.ConnectedClientsList.Count() > lines.Count)
            {
                lines.Add("- A player is still syncing into character selection...");
            }

            return "Connected players\n" + string.Join("\n", lines);
        }

        string BuildRoleButtonText(GodSoldierPlayerRole role, bool selected, bool pending, bool takenByOther)
        {
            string roleName = role == GodSoldierPlayerRole.God ? "God" : "Soldier";

            if (selected)
            {
                return $"{roleName} Locked";
            }

            if (pending)
            {
                return $"{roleName} Pending";
            }

            if (takenByOther)
            {
                return $"{roleName} Taken";
            }

            return $"Select {roleName}";
        }

        async void ReturnToMenu()
        {
            var sessionSettings = GetActiveSessionSettings();
            if (sessionSettings != null)
            {
                try
                {
                    var session = MultiplayerService.Instance?.Sessions[sessionSettings.sessionType];
                    if (session != null)
                    {
                        await session.LeaveAsync();
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[GodSoldierLobby] Failed to leave session cleanly: {exception.Message}");
                }
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            SceneManager.LoadScene(GodSoldierSceneNames.MainMenu);
        }

        void TryStartMission()
        {
            var manager = NetworkManager.Singleton;
            if (m_SelectedMission == null || manager == null || !manager.IsListening || !manager.IsHost || !ArePlayersReady(manager))
            {
                return;
            }

            manager.SceneManager.LoadScene(m_SelectedMission.SceneName, LoadSceneMode.Single);
        }

        SessionSettings GetActiveSessionSettings()
        {
            if (m_SelectedMission == null)
            {
                return null;
            }

            return m_MatchMode == MatchMode.Public
                ? m_SelectedMission.PublicSessionSettings
                : m_SelectedMission.PrivateSessionSettings;
        }

        VisualTreeAsset GetActiveMatchTemplate()
        {
            return m_MatchMode == MatchMode.Public ? publicMatchTemplate : privateMatchTemplate;
        }

        bool IsModeAllowed(MatchMode mode)
        {
            if (m_SelectedMission == null)
            {
                return true;
            }

            return mode == MatchMode.Public ? m_SelectedMission.AllowPublicMatch : m_SelectedMission.AllowPrivateMatch;
        }

        int GetExpectedHumanPlayers()
        {
            return Mathf.Max(1, m_SelectedMission?.MaxHumanPlayers ?? 2);
        }

        int GetConnectedPlayerCount(NetworkManager manager)
        {
            return manager != null && manager.IsListening ? manager.ConnectedClientsList.Count() : 0;
        }

        CorePlayerState GetLocalPlayerState(NetworkManager manager)
        {
            return manager != null &&
                manager.LocalClient != null &&
                manager.LocalClient.PlayerObject != null
                ? manager.LocalClient.PlayerObject.GetComponent<CorePlayerState>()
                : null;
        }

        System.Collections.Generic.IEnumerable<CorePlayerState> EnumerateConnectedPlayerStates(NetworkManager manager)
        {
            if (manager == null || !manager.IsListening)
            {
                yield break;
            }

            foreach (var client in manager.ConnectedClientsList)
            {
                if (client?.PlayerObject == null)
                {
                    continue;
                }

                var playerState = client.PlayerObject.GetComponent<CorePlayerState>();
                if (playerState != null)
                {
                    yield return playerState;
                }
            }
        }
    }
}
