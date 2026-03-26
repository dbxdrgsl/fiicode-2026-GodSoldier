using System.Collections.Generic;
using System.Collections;
using Blocks.Gameplay.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    public abstract class GodSoldierMissionDirectorBase : NetworkBehaviour
    {
        public static GodSoldierMissionDirectorBase Current { get; private set; }

        [Header("Mission Identity")]
        [SerializeField] protected string missionId = "descent";
        [SerializeField] protected string missionName = "Mission";

        [Header("Scene References")]
        [SerializeField] protected Transform[] soldierSpawnPoints;
        [SerializeField] protected Transform[] godSpawnPoints;
        [SerializeField] protected NotificationEvent onNotification;

        protected readonly NetworkVariable<FixedString64Bytes> m_MissionName = new(
            default(FixedString64Bytes),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        protected readonly NetworkVariable<FixedString512Bytes> m_ObjectiveText = new(
            default(FixedString512Bytes),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        protected readonly NetworkVariable<FixedString512Bytes> m_StatusText = new(
            default(FixedString512Bytes),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        protected readonly NetworkVariable<FixedString128Bytes> m_StoryTitle = new(
            default(FixedString128Bytes),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        protected readonly NetworkVariable<FixedString512Bytes> m_StoryBody = new(
            default(FixedString512Bytes),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        protected readonly NetworkVariable<bool> m_StoryVisible = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        protected virtual void Awake()
        {
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Current = this;

            if (IsServer)
            {
                m_MissionName.Value = missionName;
                StartCoroutine(DeferredMissionStart());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Current == this)
            {
                Current = null;
            }

            base.OnNetworkDespawn();
        }

        public GodSoldierMissionHudSnapshot GetHudSnapshot()
        {
            return new GodSoldierMissionHudSnapshot
            {
                missionName = m_MissionName.Value.ToString(),
                objectiveText = m_ObjectiveText.Value.ToString(),
                statusText = m_StatusText.Value.ToString(),
                storyTitle = m_StoryTitle.Value.ToString(),
                storyBody = m_StoryBody.Value.ToString(),
                storyVisible = m_StoryVisible.Value
            };
        }

        public virtual string GetHintForRole(GodSoldierPlayerRole role)
        {
            return m_ObjectiveText.Value.ToString();
        }

        public void RequestAction(string actionId, ulong playerId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            if (IsServer)
            {
                HandleAction(actionId, playerId);
            }
            else
            {
                SubmitActionRpc(actionId, playerId);
            }
        }

        public void RequestTrigger(string triggerId, ulong playerId)
        {
            if (string.IsNullOrWhiteSpace(triggerId))
            {
                return;
            }

            if (IsServer)
            {
                HandleTrigger(triggerId, playerId);
            }
            else
            {
                SubmitTriggerRpc(triggerId, playerId);
            }
        }

        public void NotifyShootableResolved(string targetId, ulong playerId)
        {
            if (!IsServer || string.IsNullOrWhiteSpace(targetId))
            {
                return;
            }

            HandleShootableResolved(targetId, playerId);
        }

        protected abstract void OnMissionNetworkSpawn();

        IEnumerator DeferredMissionStart()
        {
            yield return null;
            OnMissionNetworkSpawn();
        }

        protected virtual void HandleAction(string actionId, ulong playerId)
        {
        }

        protected virtual void HandleTrigger(string triggerId, ulong playerId)
        {
        }

        protected virtual void HandleShootableResolved(string targetId, ulong playerId)
        {
        }

        protected void SetObjective(string objectiveText, string statusText = null)
        {
            m_ObjectiveText.Value = objectiveText;
            m_StatusText.Value = statusText ?? string.Empty;
        }

        protected void SetStatus(string statusText)
        {
            m_StatusText.Value = statusText ?? string.Empty;
        }

        protected void ShowStory(string title, string body)
        {
            m_StoryTitle.Value = title;
            m_StoryBody.Value = body;
            m_StoryVisible.Value = true;
        }

        protected void HideStory()
        {
            m_StoryVisible.Value = false;
        }

        protected void BroadcastNotification(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            BroadcastNotificationRpc(message);
        }

        protected void MarkMissionCompleted()
        {
            MarkMissionCompletedRpc(missionId);
        }

        protected void TeleportPlayersToRoleSpawns()
        {
            if (NetworkManager.Singleton == null)
            {
                return;
            }

            int soldierIndex = 0;
            int godIndex = 0;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    continue;
                }

                var state = client.PlayerObject.GetComponent<CorePlayerState>();
                var roleController = client.PlayerObject.GetComponent<GodSoldierRoleController>();
                if (state == null || roleController == null)
                {
                    continue;
                }

                Transform target = null;
                switch (state.PlayerRole)
                {
                    case GodSoldierPlayerRole.God when godSpawnPoints != null && godSpawnPoints.Length > 0:
                        target = godSpawnPoints[Mathf.Min(godIndex, godSpawnPoints.Length - 1)];
                        godIndex++;
                        break;
                    case GodSoldierPlayerRole.Soldier when soldierSpawnPoints != null && soldierSpawnPoints.Length > 0:
                        target = soldierSpawnPoints[Mathf.Min(soldierIndex, soldierSpawnPoints.Length - 1)];
                        soldierIndex++;
                        break;
                }

                if (target == null && soldierSpawnPoints != null && soldierSpawnPoints.Length > 0)
                {
                    target = soldierSpawnPoints[0];
                }
                else if (target == null && godSpawnPoints != null && godSpawnPoints.Length > 0)
                {
                    target = godSpawnPoints[0];
                }

                if (target != null)
                {
                    roleController.TeleportTo(target.position, target.rotation);
                }
            }
        }

        protected void TogglePlayerMovement(bool enabled)
        {
            if (NetworkManager.Singleton == null)
            {
                return;
            }

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    continue;
                }

                var manager = client.PlayerObject.GetComponent<CorePlayerManager>();
                if (manager != null)
                {
                    manager.SetMovementInputEnabled(enabled);
                }
            }
        }

        protected bool PlayerHasRole(ulong playerId, GodSoldierPlayerRole role)
        {
            var playerState = GetPlayerState(playerId);
            return playerState != null && playerState.PlayerRole == role;
        }

        protected CorePlayerState GetPlayerState(ulong playerId)
        {
            var player = NetworkManager.Singleton?.SpawnManager?.GetPlayerNetworkObject(playerId);
            return player != null ? player.GetComponent<CorePlayerState>() : null;
        }

        protected IEnumerable<CorePlayerState> GetConnectedPlayerStates()
        {
            if (NetworkManager.Singleton == null)
            {
                yield break;
            }

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    continue;
                }

                var state = client.PlayerObject.GetComponent<CorePlayerState>();
                if (state != null)
                {
                    yield return state;
                }
            }
        }

        [Rpc(SendTo.Authority)]
        void SubmitActionRpc(FixedString128Bytes actionId, ulong playerId)
        {
            HandleAction(actionId.ToString(), playerId);
        }

        [Rpc(SendTo.Authority)]
        void SubmitTriggerRpc(FixedString128Bytes triggerId, ulong playerId)
        {
            HandleTrigger(triggerId.ToString(), playerId);
        }

        [Rpc(SendTo.Everyone)]
        void BroadcastNotificationRpc(FixedString128Bytes message)
        {
            if (onNotification != null)
            {
                onNotification.Raise(new NotificationPayload
                {
                    clientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0,
                    message = message.ToString()
                });
            }
        }

        [Rpc(SendTo.Everyone)]
        void MarkMissionCompletedRpc(FixedString64Bytes completedMissionId)
        {
            GodSoldierGameFlowState.Instance?.MarkMissionCompleted(completedMissionId.ToString());
        }
    }
}
