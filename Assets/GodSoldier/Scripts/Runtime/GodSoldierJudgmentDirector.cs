using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierJudgmentDirector : GodSoldierMissionDirectorBase
    {
        enum JudgmentStage
        {
            Intro,
            FirstDecision,
            SecondDecision,
            FinalAssassination,
            Complete
        }

        [Header("Judgment State")]
        [SerializeField] GodSoldierReplicatedActivator secondChamberGate;
        [SerializeField] GodSoldierReplicatedActivator finalChamberGate;

        readonly HashSet<string> m_ResolvedChoices = new();
        JudgmentStage m_Stage;
        int m_OrderScore;
        int m_AgencyScore;

        protected override void OnMissionNetworkSpawn()
        {
            StartCoroutine(BeginMissionRoutine());
        }

        public override string GetHintForRole(GodSoldierPlayerRole role)
        {
            return m_Stage switch
            {
                JudgmentStage.FirstDecision => "Step into the chamber that matches the peace you believe should exist.",
                JudgmentStage.SecondDecision => "Hear the survivors, then commit to either commanded order or painful agency.",
                JudgmentStage.FinalAssassination => "Choose whether the assassination seals divine control or rejects it entirely.",
                _ => base.GetHintForRole(role)
            };
        }

        protected override void HandleTrigger(string triggerId, ulong playerId)
        {
            if (m_ResolvedChoices.Contains(triggerId))
            {
                return;
            }

            switch (m_Stage)
            {
                case JudgmentStage.FirstDecision when triggerId is "choice_1_order" or "choice_1_agency":
                    ResolveChoice(triggerId);
                    secondChamberGate?.SetState(false);
                    m_Stage = JudgmentStage.SecondDecision;
                    SetObjective("A second chamber asks what peace should cost the living.", BuildAlignmentStatus());
                    break;

                case JudgmentStage.SecondDecision when triggerId is "choice_2_order" or "choice_2_agency":
                    ResolveChoice(triggerId);
                    finalChamberGate?.SetState(false);
                    m_Stage = JudgmentStage.FinalAssassination;
                    SetObjective("The assassination moment arrives. Decide whether death imposes peace or refuses divine control.",
                        BuildAlignmentStatus());
                    break;

                case JudgmentStage.FinalAssassination when triggerId is "final_assassinate" or "final_spare":
                    ResolveChoice(triggerId);
                    ResolveEnding(triggerId);
                    break;
            }
        }

        IEnumerator BeginMissionRoutine()
        {
            missionName = "Judgment";
            secondChamberGate?.SetState(true);
            finalChamberGate?.SetState(true);

            yield return WaitForPlayersToChooseRoles();

            TeleportPlayersToRoleSpawns();
            TogglePlayerMovement(false);
            ShowStory("Judgment", "The war is no longer won with gunfire alone. The pair must decide what kind of peace deserves to survive.");
            yield return new WaitForSeconds(3.4f);
            ShowStory("The Last Chambers", "Each chamber asks the same question from a different wound: should peace be imposed by power, or earned through human agency?");
            yield return new WaitForSeconds(4.2f);
            HideStory();
            TogglePlayerMovement(true);

            m_Stage = JudgmentStage.FirstDecision;
            SetObjective("Enter the first chamber and choose the shape of peace.", BuildAlignmentStatus());
            BroadcastNotification("Judgment begins.");
        }

        void ResolveChoice(string triggerId)
        {
            m_ResolvedChoices.Add(triggerId);

            if (triggerId.Contains("order"))
            {
                m_OrderScore++;
                BroadcastNotification("Order gains ground.");
            }
            else
            {
                m_AgencyScore++;
                BroadcastNotification("Agency gains ground.");
            }
        }

        void ResolveEnding(string finalTriggerId)
        {
            if (m_Stage == JudgmentStage.Complete)
            {
                return;
            }

            m_Stage = JudgmentStage.Complete;
            TogglePlayerMovement(false);

            bool imposedPeace = finalTriggerId == "final_assassinate" && m_OrderScore >= m_AgencyScore;
            bool earnedFreedom = finalTriggerId == "final_spare" || m_AgencyScore > m_OrderScore;

            if (imposedPeace)
            {
                ShowStory("Imposed Peace", "The target falls. The war ends beneath a peace chosen from above, and the pair leave knowing silence can still be a form of conquest.");
            }
            else if (earnedFreedom)
            {
                ShowStory("Earned Freedom", "The final blow is refused. Humanity keeps the burden of choice, and peace remains something the living must build themselves.");
            }
            else
            {
                ShowStory("Uneasy Verdict", "The assassination succeeds, but doubt follows. The war ends without answering who should hold the right to choose.");
            }

            SetObjective("Mission complete.", BuildAlignmentStatus());
            BroadcastNotification("Mission complete: Judgment.");
            MarkMissionCompleted();
        }

        string BuildAlignmentStatus()
        {
            return $"Order {m_OrderScore} | Agency {m_AgencyScore}";
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
