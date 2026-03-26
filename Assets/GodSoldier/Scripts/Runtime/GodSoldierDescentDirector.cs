using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blocks.Gameplay.Core;
using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierDescentDirector : GodSoldierMissionDirectorBase
    {
        enum DescentStage
        {
            Intro,
            Revival,
            RevealClue,
            PushObstacle,
            SpiritTraversal,
            FirstCombat,
            GatherResources,
            CraftExplosive,
            FinishMission,
            Complete
        }

        const string k_ReviveAction = "revive_ritual";
        const string k_RevealClueAction = "reveal_clue";
        const string k_PushAction = "push_obstacle";
        const string k_PlatformAAction = "platform_a";
        const string k_PlatformBAction = "platform_b";
        const string k_PlatformCAction = "platform_c";
        const string k_CraftExplosiveAction = "craft_explosive";

        const string k_RoomExitTrigger = "room_exit";
        const string k_RealmEntryTrigger = "realm_entry";
        const string k_FinalBreachTrigger = "final_breach";

        [Header("Mission Set Pieces")]
        [SerializeField] GodSoldierReplicatedActivator obstacleBarrier;
        [SerializeField] GodSoldierReplicatedActivator finalBreachBarrier;
        [SerializeField] GodSoldierReplicatedActivator[] spiritPlatforms;
        [SerializeField] GodSoldierMissionShootableTarget[] combatTargets;
        [SerializeField] GodSoldierMissionTriggerRelay[] explosiveResourceTriggers;

        readonly Dictionary<GodSoldierPlayerRole, float> m_LastActionTimeByRole = new();
        readonly HashSet<string> m_ActivatedPlatforms = new();
        readonly HashSet<string> m_CollectedResources = new();
        readonly HashSet<string> m_DestroyedCombatTargets = new();

        DescentStage m_Stage;
        int m_RevivalSyncs;
        int m_ObstacleSyncs;
        float m_LastSuccessfulSyncAt;
        bool m_ClueRevealed;

        protected override void OnMissionNetworkSpawn()
        {
            StartCoroutine(BeginMissionRoutine());
        }

        public override string GetHintForRole(GodSoldierPlayerRole role)
        {
            return m_Stage switch
            {
                DescentStage.Revival => "Stand in the ritual and press Primary Action in sync to restore the Soldier.",
                DescentStage.RevealClue when role == GodSoldierPlayerRole.God => "Use Primary Action at the clue altar to reveal the room's truth.",
                DescentStage.RevealClue => "Follow the God's revealed markings to the breach.",
                DescentStage.PushObstacle => "Both players must pulse Primary Action together to force the obstacle aside.",
                DescentStage.SpiritTraversal when role == GodSoldierPlayerRole.God => "Stabilize the spirit platforms one by one with Primary Action.",
                DescentStage.SpiritTraversal => "Cross the dream path as the God stabilizes each platform.",
                DescentStage.FirstCombat => "The Soldier clears the corrupted firing line while the God keeps focus.",
                DescentStage.GatherResources => "Collect every glowing resource so the God can forge an explosive.",
                DescentStage.CraftExplosive when role == GodSoldierPlayerRole.God => "Use Primary Action at the forge to build the explosive.",
                DescentStage.CraftExplosive => "Guard the forge while the God shapes the explosive.",
                DescentStage.FinishMission => "Carry the explosive to the final breach and complete the mission.",
                _ => base.GetHintForRole(role)
            };
        }

        protected override void HandleAction(string actionId, ulong playerId)
        {
            var playerState = GetPlayerState(playerId);
            if (playerState == null)
            {
                return;
            }

            switch (m_Stage)
            {
                case DescentStage.Revival when actionId == k_ReviveAction:
                    RegisterSynchronizedAction(playerState.PlayerRole, ref m_RevivalSyncs, 3,
                        "The ritual answers. Keep the rhythm.",
                        () =>
                        {
                            TogglePlayerMovement(true);
                            m_Stage = DescentStage.RevealClue;
                            SetObjective("The God reveals the chamber's truth while the Soldier searches for the breach.",
                                "Clue sealed. God must reveal the markings.");
                            BroadcastNotification("The Soldier breathes again.");
                        });
                    break;

                case DescentStage.RevealClue when actionId == k_RevealClueAction && playerState.PlayerRole == GodSoldierPlayerRole.God:
                    if (m_ClueRevealed)
                    {
                        return;
                    }

                    m_ClueRevealed = true;
                    SetObjective("The Soldier must follow the revealed breach and leave the chamber.",
                        "The chamber markings are visible. Move.");
                    BroadcastNotification("The God tears back the illusion hiding the breach.");
                    break;

                case DescentStage.PushObstacle when actionId == k_PushAction:
                    RegisterSynchronizedAction(playerState.PlayerRole, ref m_ObstacleSyncs, 4,
                        "The obstacle groans. Keep pushing together.",
                        () =>
                        {
                            obstacleBarrier?.SetState(false);
                            m_Stage = DescentStage.SpiritTraversal;
                            SetObjective("The God stabilizes the dream path while the Soldier crosses toward the physical realm.",
                                $"Spirit platforms stabilized: {m_ActivatedPlatforms.Count}/{spiritPlatforms.Length}");
                            BroadcastNotification("The path opens into the between-realm.");
                        });
                    break;

                case DescentStage.SpiritTraversal when playerState.PlayerRole == GodSoldierPlayerRole.God:
                    HandlePlatformAction(actionId);
                    break;

                case DescentStage.CraftExplosive when actionId == k_CraftExplosiveAction && playerState.PlayerRole == GodSoldierPlayerRole.God:
                    finalBreachBarrier?.SetState(false);
                    m_Stage = DescentStage.FinishMission;
                    SetObjective("The Soldier carries the forged explosive to the final breach.",
                        "Explosive forged. Reach the final breach.");
                    BroadcastNotification("The explosive is ready.");
                    break;
            }
        }

        protected override void HandleTrigger(string triggerId, ulong playerId)
        {
            switch (m_Stage)
            {
                case DescentStage.RevealClue when triggerId == k_RoomExitTrigger && PlayerHasRole(playerId, GodSoldierPlayerRole.Soldier) && m_ClueRevealed:
                    m_Stage = DescentStage.PushObstacle;
                    SetObjective("A buried obstacle blocks the route. Both players must force it aside together.",
                        $"Push rhythm secured: {m_ObstacleSyncs}/4");
                    BroadcastNotification("The breach leads into a shattered corridor.");
                    break;

                case DescentStage.SpiritTraversal when triggerId == k_RealmEntryTrigger && PlayerHasRole(playerId, GodSoldierPlayerRole.Soldier):
                    m_Stage = DescentStage.FirstCombat;
                    SetObjective("The Soldier clears the corrupted firing line in the physical realm.",
                        $"Targets remaining: {combatTargets.Length - m_DestroyedCombatTargets.Count}");
                    BroadcastNotification("The pair break into the physical realm.");
                    break;

                case DescentStage.GatherResources when triggerId.StartsWith("resource_") && PlayerHasRole(playerId, GodSoldierPlayerRole.Soldier):
                    if (m_CollectedResources.Add(triggerId))
                    {
                        SetStatus($"Explosive materials gathered: {m_CollectedResources.Count}/{explosiveResourceTriggers.Length}");
                        BroadcastNotification("Explosive material recovered.");

                        if (m_CollectedResources.Count >= explosiveResourceTriggers.Length)
                        {
                            m_Stage = DescentStage.CraftExplosive;
                            SetObjective("Return to the forge. The God can now craft the explosive.",
                                "All materials secured. God must craft the explosive.");
                        }
                    }
                    break;

                case DescentStage.FinishMission when triggerId == k_FinalBreachTrigger && PlayerHasRole(playerId, GodSoldierPlayerRole.Soldier):
                    CompleteMission();
                    break;
            }
        }

        protected override void HandleShootableResolved(string targetId, ulong playerId)
        {
            if (m_Stage != DescentStage.FirstCombat || !m_DestroyedCombatTargets.Add(targetId))
            {
                return;
            }

            SetStatus($"Targets remaining: {Mathf.Max(0, combatTargets.Length - m_DestroyedCombatTargets.Count)}");

            if (m_DestroyedCombatTargets.Count >= combatTargets.Length)
            {
                m_Stage = DescentStage.GatherResources;
                SetObjective("The Soldier searches the war yard for explosive materials.",
                    $"Explosive materials gathered: {m_CollectedResources.Count}/{explosiveResourceTriggers.Length}");
                BroadcastNotification("The firing line is broken. Gather explosive materials.");
            }
        }

        IEnumerator BeginMissionRoutine()
        {
            missionName = "Descent";
            obstacleBarrier?.SetState(true);
            finalBreachBarrier?.SetState(true);

            if (spiritPlatforms != null)
            {
                foreach (var platform in spiritPlatforms)
                {
                    platform?.SetState(false);
                }
            }

            yield return WaitForPlayersToChooseRoles();

            TeleportPlayersToRoleSpawns();
            TogglePlayerMovement(false);
            m_Stage = DescentStage.Intro;

            ShowStory("Descent", "War tears the sky open as the Strategic God descends through smoke and artillery fire.");
            yield return new WaitForSeconds(3.2f);
            ShowStory("Bond of Revival", "The fallen Soldier does not rise alone. God and Soldier must move in the same rhythm to force life back into ruined flesh.");
            yield return new WaitForSeconds(4f);
            HideStory();

            m_Stage = DescentStage.Revival;
            SetObjective("Both players pulse Primary Action together to complete the revival ritual.",
                $"Synchronized pulses: {m_RevivalSyncs}/3");
            BroadcastNotification("Revival ritual active.");
        }

        void RegisterSynchronizedAction(GodSoldierPlayerRole role, ref int syncCounter, int requiredCount, string progressMessage, System.Action onCompleted)
        {
            if (role == GodSoldierPlayerRole.None)
            {
                return;
            }

            m_LastActionTimeByRole[role] = Time.time;

            if (!m_LastActionTimeByRole.TryGetValue(GodSoldierPlayerRole.God, out var godTime) ||
                !m_LastActionTimeByRole.TryGetValue(GodSoldierPlayerRole.Soldier, out var soldierTime))
            {
                SetStatus($"Synchronized pulses: {syncCounter}/{requiredCount}");
                return;
            }

            if (Mathf.Abs(godTime - soldierTime) > 0.6f || Time.time - m_LastSuccessfulSyncAt < 0.4f)
            {
                SetStatus($"Synchronized pulses: {syncCounter}/{requiredCount}");
                return;
            }

            m_LastSuccessfulSyncAt = Time.time;
            syncCounter++;
            SetStatus($"Synchronized pulses: {syncCounter}/{requiredCount}");
            BroadcastNotification(progressMessage);

            if (syncCounter >= requiredCount)
            {
                onCompleted?.Invoke();
            }
        }

        void HandlePlatformAction(string actionId)
        {
            int index = actionId switch
            {
                k_PlatformAAction => 0,
                k_PlatformBAction => 1,
                k_PlatformCAction => 2,
                _ => -1
            };

            if (index < 0 || spiritPlatforms == null || index >= spiritPlatforms.Length)
            {
                return;
            }

            if (!m_ActivatedPlatforms.Add(actionId))
            {
                return;
            }

            spiritPlatforms[index]?.SetState(true);
            SetStatus($"Spirit platforms stabilized: {m_ActivatedPlatforms.Count}/{spiritPlatforms.Length}");
            BroadcastNotification("A spirit platform stabilizes in the dream fracture.");
        }

        void CompleteMission()
        {
            if (m_Stage == DescentStage.Complete)
            {
                return;
            }

            m_Stage = DescentStage.Complete;
            TogglePlayerMovement(false);
            ShowStory("First Victory", "The explosive tears open the final breach. God and Soldier enter the next theater of war as a true pair.");
            SetObjective("Mission complete.", "Descent is now recorded in the campaign timeline.");
            BroadcastNotification("Mission complete: Descent.");
            MarkMissionCompleted();
        }

        IEnumerator WaitForPlayersToChooseRoles()
        {
            const float timeout = 3f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                var states = GetConnectedPlayerStates().ToList();
                if (states.Count >= 2 && states.All(state => state.PlayerRole != GodSoldierPlayerRole.None))
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
