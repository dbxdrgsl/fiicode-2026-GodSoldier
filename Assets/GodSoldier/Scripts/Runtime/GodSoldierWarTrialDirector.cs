using System.Collections;
using System.Linq;
using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierWarTrialDirector : GodSoldierMissionDirectorBase
    {
        enum WarTrialStage
        {
            Intro,
            ReachFirstAnchor,
            ClearFirstWard,
            ReachSecondAnchor,
            ClearSecondWard,
            Complete
        }

        const string k_AnchorSoldierBoss = "anchor_soldier_boss";
        const string k_ShatterSoldierWard = "shatter_soldier_ward";
        const string k_AnchorGodBoss = "anchor_god_boss";
        const string k_SealGodBoss = "seal_god_boss";

        [Header("Boss Encounter")]
        [SerializeField] GodSoldierReplicatedActivator soldierBossShield;
        [SerializeField] GodSoldierReplicatedActivator godBossShield;
        [SerializeField] GodSoldierReplicatedActivator waveGroupActivator;
        [SerializeField] GodSoldierReplicatedActivator[] soldierAttackTelegraphs;
        [SerializeField] GodSoldierReplicatedActivator[] godAttackTelegraphs;

        WarTrialStage m_Stage;

        protected override void OnMissionNetworkSpawn()
        {
            StartCoroutine(BeginMissionRoutine());
        }

        public override string GetHintForRole(GodSoldierPlayerRole role)
        {
            return m_Stage switch
            {
                WarTrialStage.ReachFirstAnchor => "Move to the first arena anchor block.",
                WarTrialStage.ClearFirstWard => "Touch the first ward block to clear the phase.",
                WarTrialStage.ReachSecondAnchor => "Cross the arena and enter the second anchor block.",
                WarTrialStage.ClearSecondWard => "Touch the final seal block to finish the trial.",
                _ => base.GetHintForRole(role)
            };
        }

        protected override void HandleAction(string actionId, ulong playerId)
        {
            switch (m_Stage)
            {
                case WarTrialStage.ClearFirstWard when actionId == k_ShatterSoldierWard:
                    soldierBossShield?.SetState(false);
                    m_Stage = WarTrialStage.ReachSecondAnchor;
                    SetObjective("Phase one is open. Cross the arena and enter the second anchor block.",
                        "Second anchor block ahead.");
                    BroadcastNotification("The first ward collapses.");
                    break;

                case WarTrialStage.ClearSecondWard when actionId == k_SealGodBoss:
                    godBossShield?.SetState(false);
                    CompleteMission();
                    break;
            }
        }

        protected override void HandleTrigger(string triggerId, ulong playerId)
        {
            switch (m_Stage)
            {
                case WarTrialStage.ReachFirstAnchor when triggerId == k_AnchorSoldierBoss:
                    m_Stage = WarTrialStage.ClearFirstWard;
                    SetObjective("The first route is active. Touch the ward block to clear it.",
                        "First ward block ahead.");
                    BroadcastNotification("The first arena anchor is active.");
                    break;

                case WarTrialStage.ReachSecondAnchor when triggerId == k_AnchorGodBoss:
                    m_Stage = WarTrialStage.ClearSecondWard;
                    SetObjective("Final route is active. Touch the last seal block to end the trial.",
                        "Final seal block ahead.");
                    BroadcastNotification("The second arena anchor is active.");
                    break;
            }
        }

        IEnumerator BeginMissionRoutine()
        {
            missionName = "War Trial";
            m_Stage = WarTrialStage.Intro;

            soldierBossShield?.SetState(true);
            godBossShield?.SetState(true);
            waveGroupActivator?.SetState(false);
            ToggleTelegraphSet(soldierAttackTelegraphs, false);
            ToggleTelegraphSet(godAttackTelegraphs, false);

            yield return WaitForPlayersToChooseRoles();

            TeleportPlayersToRoleSpawns();
            TogglePlayerMovement(true);

            ShowStory("War Trial", "The arena now uses a simpler route: keep both players in third-person, keep the cameras personal, and clear each phase by entering the arena trigger blocks.");
            yield return new WaitForSeconds(2.8f);
            ShowStory("Arena Route", "The encounter is temporarily reduced to anchor blocks and ward blocks so the baseline movement loop stays solid.");
            yield return new WaitForSeconds(3.1f);
            HideStory();

            m_Stage = WarTrialStage.ReachFirstAnchor;
            SetObjective("Move to the first arena anchor block to start the trial.",
                "First anchor block ahead.");
            BroadcastNotification("War Trial begins.");
        }

        void ToggleTelegraphSet(GodSoldierReplicatedActivator[] activators, bool enabledState)
        {
            if (activators == null)
            {
                return;
            }

            foreach (var activator in activators)
            {
                activator?.SetState(enabledState);
            }
        }

        void CompleteMission()
        {
            if (m_Stage == WarTrialStage.Complete)
            {
                return;
            }

            m_Stage = WarTrialStage.Complete;
            TogglePlayerMovement(false);
            ToggleTelegraphSet(soldierAttackTelegraphs, false);
            ToggleTelegraphSet(godAttackTelegraphs, false);
            ShowStory("Trial Cleared", "The corrupted mirror encounter falls away and the level now resolves through clean third-person traversal instead of unfinished boss scripting.");
            SetObjective("Mission complete.", "War Trial is now recorded in the campaign timeline.");
            BroadcastNotification("Mission complete: War Trial.");
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
