using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierDescentDirector : GodSoldierMissionDirectorBase
    {
        enum DescentStage
        {
            Intro,
            RevealClue,
            PushObstacle,
            SpiritTraversal,
            GatherResources,
            CraftExplosive,
            FinishMission,
            Complete
        }

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
        [SerializeField] GodSoldierMissionTriggerRelay[] explosiveResourceTriggers;

        readonly HashSet<string> m_ActivatedPlatforms = new();
        readonly HashSet<string> m_CollectedResources = new();

        DescentStage m_Stage;
        bool m_ClueRevealed;

        protected override void OnMissionNetworkSpawn()
        {
            StartCoroutine(BeginMissionRoutine());
        }

        public override string GetHintForRole(GodSoldierPlayerRole role)
        {
            return m_Stage switch
            {
                DescentStage.RevealClue => "Walk into the first guidance block to reveal the route, then cross the corridor exit block.",
                DescentStage.PushObstacle => "Touch the obstacle block to clear the path forward.",
                DescentStage.SpiritTraversal => "Cross the stabilizer blocks, then move into the realm entry block.",
                DescentStage.GatherResources => "Sweep the yard and collect every glowing trigger block.",
                DescentStage.CraftExplosive => "Touch the forge block to arm the breach.",
                DescentStage.FinishMission => "Run to the final breach block.",
                _ => base.GetHintForRole(role)
            };
        }

        protected override void HandleAction(string actionId, ulong playerId)
        {
            switch (m_Stage)
            {
                case DescentStage.RevealClue when actionId == k_RevealClueAction:
                    if (m_ClueRevealed)
                    {
                        return;
                    }

                    m_ClueRevealed = true;
                    SetObjective("The route is visible. Cross the corridor and enter the exit trigger block.",
                        "Clue revealed. Keep moving.");
                    BroadcastNotification("The first route opens.");
                    break;

                case DescentStage.PushObstacle when actionId == k_PushAction:
                    obstacleBarrier?.SetState(false);
                    m_Stage = DescentStage.SpiritTraversal;
                    SetObjective("Touch each stabilizer block and then cross the realm entry trigger.",
                        BuildPlatformStatus());
                    BroadcastNotification("The obstacle clears.");
                    break;

                case DescentStage.SpiritTraversal:
                    HandlePlatformAction(actionId);
                    break;

                case DescentStage.CraftExplosive when actionId == k_CraftExplosiveAction:
                    finalBreachBarrier?.SetState(false);
                    m_Stage = DescentStage.FinishMission;
                    SetObjective("The path is open. Reach the final breach trigger block.",
                        "Explosive armed.");
                    BroadcastNotification("The breach charge is ready.");
                    break;
            }
        }

        protected override void HandleTrigger(string triggerId, ulong playerId)
        {
            switch (m_Stage)
            {
                case DescentStage.RevealClue when triggerId == k_RoomExitTrigger && m_ClueRevealed:
                    m_Stage = DescentStage.PushObstacle;
                    SetObjective("A sealed route blocks the pair. Touch the obstacle block to open it.",
                        "Obstacle block ahead.");
                    BroadcastNotification("The corridor opens into the next chamber.");
                    break;

                case DescentStage.SpiritTraversal when triggerId == k_RealmEntryTrigger:
                    m_Stage = DescentStage.GatherResources;
                    SetObjective("Collect the yard resource trigger blocks to prepare the breach charge.",
                        BuildResourceStatus());
                    BroadcastNotification("The pair cross into the war yard.");
                    break;

                case DescentStage.GatherResources when triggerId.StartsWith("resource_"):
                    if (!m_CollectedResources.Add(triggerId))
                    {
                        return;
                    }

                    SetStatus(BuildResourceStatus());
                    BroadcastNotification("Explosive material recovered.");

                    if (m_CollectedResources.Count >= GetResourceGoal())
                    {
                        m_Stage = DescentStage.CraftExplosive;
                        SetObjective("All materials are secured. Touch the forge block to arm the breach charge.",
                            "Forge block ready.");
                    }
                    break;

                case DescentStage.FinishMission when triggerId == k_FinalBreachTrigger:
                    CompleteMission();
                    break;
            }
        }

        IEnumerator BeginMissionRoutine()
        {
            missionName = "Descent";
            m_Stage = DescentStage.Intro;
            m_ClueRevealed = false;
            m_ActivatedPlatforms.Clear();
            m_CollectedResources.Clear();

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
            TogglePlayerMovement(true);

            ShowStory("Descent", "The pair enter the first battlefield together. Movement and route-reading matter more than ritual timing right now.");
            yield return new WaitForSeconds(2.6f);
            ShowStory("Route Training", "For the current build, progress is carried by walking through trigger blocks and keeping the pair moving.");
            yield return new WaitForSeconds(3.1f);
            HideStory();

            m_Stage = DescentStage.RevealClue;
            SetObjective("Walk into the first clue block to reveal the path out of the chamber.",
                "Clue block ahead.");
            BroadcastNotification("Descent begins.");
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
            SetStatus(BuildPlatformStatus());
            BroadcastNotification("A traversal block stabilizes.");
        }

        string BuildPlatformStatus()
        {
            int totalPlatforms = spiritPlatforms != null ? spiritPlatforms.Length : 0;
            return totalPlatforms <= 0
                ? "Route is open."
                : $"Traversal blocks touched: {m_ActivatedPlatforms.Count}/{totalPlatforms}";
        }

        string BuildResourceStatus()
        {
            return $"Resource blocks collected: {m_CollectedResources.Count}/{GetResourceGoal()}";
        }

        int GetResourceGoal()
        {
            return explosiveResourceTriggers != null && explosiveResourceTriggers.Length > 0
                ? explosiveResourceTriggers.Length
                : 3;
        }

        void CompleteMission()
        {
            if (m_Stage == DescentStage.Complete)
            {
                return;
            }

            m_Stage = DescentStage.Complete;
            TogglePlayerMovement(false);
            ShowStory("Forward Momentum", "The final breach opens and the pair leave the first battlefield with the core third-person loop intact.");
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
