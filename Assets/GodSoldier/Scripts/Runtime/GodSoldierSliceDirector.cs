using System.Collections;
using System.Collections.Generic;
using Blocks.Gameplay.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    public class GodSoldierSliceDirector : NetworkBehaviour
    {
        public static GodSoldierSliceDirector Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private Transform[] soldierSpawnPoints;
        [SerializeField] private Transform[] godSpawnPoints;
        [SerializeField] private NotificationEvent onNotification;

        [Header("Crafting Costs")]
        [SerializeField] private int bridgeScrapCost = 2;
        [SerializeField] private int bridgeEssenceCost = 1;
        [SerializeField] private int sigilScrapCost = 1;
        [SerializeField] private int sigilEssenceCost = 2;

        readonly NetworkVariable<GodSoldierObjectiveStage> m_ObjectiveStage = new(
            GodSoldierObjectiveStage.Lobby,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> m_Scrap = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> m_Essence = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> m_OrderScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<int> m_AgencyScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<bool> m_BridgeCrafted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<bool> m_SigilCrafted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<bool> m_StoryVisible = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<FixedString128Bytes> m_ObjectiveText = new(default(FixedString128Bytes), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<FixedString128Bytes> m_StoryTitle = new(default(FixedString128Bytes), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        readonly NetworkVariable<FixedString512Bytes> m_StoryBody = new(default(FixedString512Bytes), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        readonly HashSet<string> m_ResolvedChoices = new();

        public bool BridgeCrafted => m_BridgeCrafted.Value;
        public bool SigilCrafted => m_SigilCrafted.Value;
        public GodSoldierObjectiveStage ObjectiveStage => m_ObjectiveStage.Value;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                StartCoroutine(BeginSliceRoutine());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnNetworkDespawn();
        }

        public GodSoldierSliceSnapshot GetSnapshot()
        {
            return new GodSoldierSliceSnapshot
            {
                objectiveStage = m_ObjectiveStage.Value,
                scrap = m_Scrap.Value,
                essence = m_Essence.Value,
                orderScore = m_OrderScore.Value,
                agencyScore = m_AgencyScore.Value,
                bridgeCrafted = m_BridgeCrafted.Value,
                sigilCrafted = m_SigilCrafted.Value,
                objectiveText = m_ObjectiveText.Value.ToString(),
                storyTitle = m_StoryTitle.Value.ToString(),
                storyBody = m_StoryBody.Value.ToString(),
                storyVisible = m_StoryVisible.Value
            };
        }

        public string GetHintForRole(GodSoldierPlayerRole role)
        {
            return role switch
            {
                GodSoldierPlayerRole.God => m_ObjectiveStage.Value switch
                {
                    GodSoldierObjectiveStage.GatherResources => "Guide the Soldier to the glowing scrap and essence. The shrine will answer once the offerings are gathered.",
                    GodSoldierObjectiveStage.CraftBridge => "Stand within the shrine and channel the Bridge Blessing.",
                    GodSoldierObjectiveStage.CrossBridge => "The spirit bridge is open. Lead the Soldier across the breach.",
                    GodSoldierObjectiveStage.CraftSigil => "Return to the shrine and forge the War Sigil.",
                    GodSoldierObjectiveStage.ArtilleryChoice => "Decide whether divine power feeds the cannon or the ward.",
                    GodSoldierObjectiveStage.FinalChoice => "The war engine waits. Decide whether peace should be imposed or earned.",
                    _ => m_ObjectiveText.Value.ToString()
                },
                GodSoldierPlayerRole.Soldier => m_ObjectiveText.Value.ToString(),
                _ => "Choose a role in the lobby to begin."
            };
        }

        public bool TryAddResource(GodSoldierResourceType resourceType, int amount, ulong playerId)
        {
            if (!IsServer || !PlayerHasRole(playerId, GodSoldierPlayerRole.Soldier))
            {
                return false;
            }

            switch (resourceType)
            {
                case GodSoldierResourceType.Scrap:
                    m_Scrap.Value += amount;
                    break;
                case GodSoldierResourceType.Essence:
                    m_Essence.Value += amount;
                    break;
            }

            if (m_ObjectiveStage.Value == GodSoldierObjectiveStage.GatherResources &&
                m_Scrap.Value >= bridgeScrapCost &&
                m_Essence.Value >= bridgeEssenceCost)
            {
                SetObjective(GodSoldierObjectiveStage.CraftBridge, "The altar is fed. The God can now craft the Bridge Blessing.");
            }
            else if (m_BridgeCrafted.Value &&
                     !m_SigilCrafted.Value &&
                     m_ObjectiveStage.Value == GodSoldierObjectiveStage.CrossBridge &&
                     m_Scrap.Value >= sigilScrapCost &&
                     m_Essence.Value >= sigilEssenceCost)
            {
                SetObjective(GodSoldierObjectiveStage.CraftSigil, "The war yard yields enough power. The God can now forge the War Sigil.");
            }

            BroadcastNotification($"{resourceType} secured. Team stockpile: Scrap {m_Scrap.Value} | Essence {m_Essence.Value}");
            return true;
        }

        public bool TryCraftBlessing(ulong playerId)
        {
            if (!IsServer || !PlayerHasRole(playerId, GodSoldierPlayerRole.God))
            {
                return false;
            }

            if (!m_BridgeCrafted.Value && m_Scrap.Value >= bridgeScrapCost && m_Essence.Value >= bridgeEssenceCost)
            {
                m_Scrap.Value -= bridgeScrapCost;
                m_Essence.Value -= bridgeEssenceCost;
                m_BridgeCrafted.Value = true;
                SetObjective(GodSoldierObjectiveStage.CrossBridge, "The spirit bridge answers your bond. Cross the breach and gather enough power for a war sigil.");
                BroadcastNotification("Bridge Blessing forged.");
                return true;
            }

            if (m_BridgeCrafted.Value && !m_SigilCrafted.Value && m_Scrap.Value >= sigilScrapCost && m_Essence.Value >= sigilEssenceCost)
            {
                m_Scrap.Value -= sigilScrapCost;
                m_Essence.Value -= sigilEssenceCost;
                m_SigilCrafted.Value = true;
                SetObjective(GodSoldierObjectiveStage.ArtilleryChoice, "The War Sigil is ready. Decide whether divine power serves the cannon or the civilian ward.");
                BroadcastNotification("War Sigil forged.");
                return true;
            }

            return false;
        }

        public bool RegisterChoice(string choiceId, GodSoldierAlignmentChoice alignment, string summary)
        {
            if (!IsServer || string.IsNullOrEmpty(choiceId) || m_ResolvedChoices.Contains(choiceId))
            {
                return false;
            }

            m_ResolvedChoices.Add(choiceId);

            if (alignment == GodSoldierAlignmentChoice.Order)
            {
                m_OrderScore.Value++;
            }
            else if (alignment == GodSoldierAlignmentChoice.Agency)
            {
                m_AgencyScore.Value++;
            }

            BroadcastNotification(summary);

            if (choiceId == "artillery")
            {
                SetObjective(GodSoldierObjectiveStage.FinalChoice, "The path to the war engine is open. Decide whether peace must be imposed or earned.");
            }
            else if (choiceId == "final")
            {
                ResolveEnding(alignment);
            }

            return true;
        }

        IEnumerator BeginSliceRoutine()
        {
            const float roleWaitTimeout = 2f;
            float elapsed = 0f;
            while (!AllConnectedPlayersHaveRoles() && elapsed < roleWaitTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            TeleportPlayersToRoleSpawns();
            yield return null;
            TogglePlayerMovement(false);

            SetObjective(GodSoldierObjectiveStage.Intro, "War rips the earth apart.");
            yield return PlayStoryCard("Above the clouds", "The gods wage strategy in the heavens while the earth below drowns in artillery and ash.", 4.2f);
            yield return PlayStoryCard("At the brink of death", "A dying soldier is chosen by the Strategic God, not for faith, but for resolve.", 4.2f);
            yield return PlayStoryCard("Two realms, one cause", "The Soldier must act in the physical world. The God must guide, reveal, and craft the bond that keeps them alive.", 5f);
            yield return PlayStoryCard("Your first task", "Gather lost scrap and fallen essence. Without them, the bridge to the war yard will never form.", 4.2f);

            m_StoryVisible.Value = false;
            TogglePlayerMovement(true);
            SetObjective(GodSoldierObjectiveStage.GatherResources, "The Soldier must gather 2 Scrap and 1 Essence while the God scouts the fallen shrine.");
            BroadcastNotification("The bond is sealed. Move.");
        }

        IEnumerator PlayStoryCard(string title, string body, float duration)
        {
            m_StoryTitle.Value = title;
            m_StoryBody.Value = body;
            m_StoryVisible.Value = true;
            yield return new WaitForSeconds(duration);
        }

        void TeleportPlayersToRoleSpawns()
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

                if (target == null)
                {
                    if (soldierSpawnPoints != null && soldierSpawnPoints.Length > 0)
                    {
                        target = soldierSpawnPoints[0];
                    }
                    else if (godSpawnPoints != null && godSpawnPoints.Length > 0)
                    {
                        target = godSpawnPoints[0];
                    }
                }

                if (target != null)
                {
                    roleController.TeleportTo(target.position, target.rotation);
                }
            }
        }

        void TogglePlayerMovement(bool enabled)
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

        void ResolveEnding(GodSoldierAlignmentChoice finalAlignment)
        {
            var imposedPeace = finalAlignment == GodSoldierAlignmentChoice.Order && m_OrderScore.Value >= m_AgencyScore.Value;

            TogglePlayerMovement(false);
            m_ObjectiveStage.Value = GodSoldierObjectiveStage.Ending;
            m_ObjectiveText.Value = imposedPeace
                ? "Ending reached: Imposed Peace"
                : "Ending reached: Earned Freedom";
            m_StoryVisible.Value = true;

            if (imposedPeace)
            {
                m_StoryTitle.Value = "Imposed Peace";
                m_StoryBody.Value = "The Strategic God forces a ceasefire through divine design. The guns fall silent, but the Soldier knows the silence was chosen for humanity, not by it.";
            }
            else
            {
                m_StoryTitle.Value = "Earned Freedom";
                m_StoryBody.Value = "The war engine is denied its divine master. Humanity keeps the burden of choice, and the pair leave the battlefield knowing peace must be built, not imposed.";
            }

            BroadcastNotification("The final choice has reshaped the war.");
        }

        void SetObjective(GodSoldierObjectiveStage stage, string objectiveText)
        {
            m_ObjectiveStage.Value = stage;
            m_ObjectiveText.Value = objectiveText;
        }

        void BroadcastNotification(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                BroadcastNotificationRpc(message);
            }
        }

        bool PlayerHasRole(ulong playerId, GodSoldierPlayerRole role)
        {
            var player = NetworkManager.Singleton?.SpawnManager?.GetPlayerNetworkObject(playerId);
            if (player == null)
            {
                return false;
            }

            var state = player.GetComponent<CorePlayerState>();
            return state != null && state.PlayerRole == role;
        }

        bool AllConnectedPlayersHaveRoles()
        {
            if (NetworkManager.Singleton == null)
            {
                return true;
            }

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null)
                {
                    return false;
                }

                var state = client.PlayerObject.GetComponent<CorePlayerState>();
                if (state == null || state.PlayerRole == GodSoldierPlayerRole.None)
                {
                    return false;
                }
            }

            return true;
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
    }
}
