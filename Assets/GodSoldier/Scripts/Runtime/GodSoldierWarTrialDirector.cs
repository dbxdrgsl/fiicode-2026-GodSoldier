using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierWarTrialDirector : GodSoldierMissionDirectorBase
    {
        enum WarTrialStage
        {
            Intro,
            CounterSoldierBoss,
            SoldierBossVulnerable,
            NPCWave,
            CounterGodBoss,
            GodBossVulnerable,
            Complete
        }

        const string k_AnchorSoldierBoss = "anchor_soldier_boss";
        const string k_ShatterSoldierWard = "shatter_soldier_ward";
        const string k_AnchorGodBoss = "anchor_god_boss";
        const string k_SealGodBoss = "seal_god_boss";

        [Header("Boss Encounter")]
        [SerializeField] GodSoldierReplicatedActivator soldierBossShield;
        [SerializeField] GodSoldierReplicatedActivator godBossShield;
        [SerializeField] GodSoldierMissionShootableTarget soldierBossCore;
        [SerializeField] GodSoldierMissionShootableTarget godBossCore;
        [SerializeField] GodSoldierReplicatedActivator waveGroupActivator;
        [SerializeField] GodSoldierMissionShootableTarget[] waveTargets;
        [SerializeField] GodSoldierReplicatedActivator[] soldierAttackTelegraphs;
        [SerializeField] GodSoldierReplicatedActivator[] godAttackTelegraphs;

        readonly HashSet<string> m_DestroyedWaveTargets = new();
        WarTrialStage m_Stage;
        bool m_SoldierBossAnchored;
        bool m_GodBossAnchored;

        protected override void OnMissionNetworkSpawn()
        {
            StartCoroutine(BeginMissionRoutine());
            StartCoroutine(AttackTelegraphLoop());
        }

        public override string GetHintForRole(GodSoldierPlayerRole role)
        {
            return m_Stage switch
            {
                WarTrialStage.CounterSoldierBoss when role == GodSoldierPlayerRole.Soldier => "Hold the Executioner in place from the anchor plate.",
                WarTrialStage.CounterSoldierBoss => "Shatter the Executioner's ward when the Soldier has anchored it.",
                WarTrialStage.SoldierBossVulnerable => "The Soldier pours fire into the exposed Executioner core.",
                WarTrialStage.NPCWave => "Clear the lesser war spirits before the next duel.",
                WarTrialStage.CounterGodBoss when role == GodSoldierPlayerRole.Soldier => "Pin the False Strategist from the ground seal.",
                WarTrialStage.CounterGodBoss => "Use Primary Action to collapse the False Strategist's shield once the Soldier has anchored it.",
                WarTrialStage.GodBossVulnerable => "Finish the corrupted pair. The Strategist is exposed.",
                _ => base.GetHintForRole(role)
            };
        }

        protected override void HandleAction(string actionId, ulong playerId)
        {
            if (!PlayerHasRole(playerId, GodSoldierPlayerRole.God))
            {
                return;
            }

            switch (m_Stage)
            {
                case WarTrialStage.CounterSoldierBoss when actionId == k_ShatterSoldierWard && m_SoldierBossAnchored:
                    soldierBossShield?.SetState(false);
                    m_Stage = WarTrialStage.SoldierBossVulnerable;
                    SetObjective("The corrupted Soldier is exposed. The Soldier must break the boss core.",
                        "Executioner core exposed. Fire now.");
                    BroadcastNotification("The Executioner's ward collapses.");
                    break;

                case WarTrialStage.CounterGodBoss when actionId == k_SealGodBoss && m_GodBossAnchored:
                    godBossShield?.SetState(false);
                    m_Stage = WarTrialStage.GodBossVulnerable;
                    SetObjective("The corrupted God is exposed. Finish the duel before the storm resets.",
                        "False Strategist exposed. Fire now.");
                    BroadcastNotification("The False Strategist loses its shield.");
                    break;
            }
        }

        protected override void HandleTrigger(string triggerId, ulong playerId)
        {
            if (!PlayerHasRole(playerId, GodSoldierPlayerRole.Soldier))
            {
                return;
            }

            switch (m_Stage)
            {
                case WarTrialStage.CounterSoldierBoss when triggerId == k_AnchorSoldierBoss:
                    m_SoldierBossAnchored = true;
                    SetStatus("Executioner anchored. God must shatter the ward.");
                    BroadcastNotification("The Soldier pins the corrupted Executioner in place.");
                    break;

                case WarTrialStage.CounterGodBoss when triggerId == k_AnchorGodBoss:
                    m_GodBossAnchored = true;
                    SetStatus("False Strategist anchored. God must collapse the shield.");
                    BroadcastNotification("The Soldier pins the False Strategist to the ground seal.");
                    break;
            }
        }

        protected override void HandleShootableResolved(string targetId, ulong playerId)
        {
            switch (m_Stage)
            {
                case WarTrialStage.SoldierBossVulnerable when targetId == "soldier_boss_core":
                    waveGroupActivator?.SetState(true);
                    m_Stage = WarTrialStage.NPCWave;
                    SetObjective("Lesser war spirits flood the arena. Clear the wave before the second boss descends.",
                        $"Wave targets remaining: {waveTargets.Length - m_DestroyedWaveTargets.Count}");
                    BroadcastNotification("The corrupted Soldier falls. Lesser war spirits surge forward.");
                    break;

                case WarTrialStage.NPCWave when targetId.StartsWith("wave_"):
                    if (!m_DestroyedWaveTargets.Add(targetId))
                    {
                        return;
                    }

                    SetStatus($"Wave targets remaining: {Mathf.Max(0, waveTargets.Length - m_DestroyedWaveTargets.Count)}");
                    if (m_DestroyedWaveTargets.Count >= waveTargets.Length)
                    {
                        m_Stage = WarTrialStage.CounterGodBoss;
                        SetObjective("Anchor the False Strategist, then let the God collapse the shield.",
                            "Prepare the counter on the second boss.");
                        BroadcastNotification("The arena clears for the final duel.");
                    }
                    break;

                case WarTrialStage.GodBossVulnerable when targetId == "god_boss_core":
                    CompleteMission();
                    break;
            }
        }

        IEnumerator BeginMissionRoutine()
        {
            missionName = "War Trial";
            soldierBossShield?.SetState(true);
            godBossShield?.SetState(true);
            waveGroupActivator?.SetState(false);

            if (soldierAttackTelegraphs != null)
            {
                foreach (var telegraph in soldierAttackTelegraphs)
                {
                    telegraph?.SetState(false);
                }
            }

            if (godAttackTelegraphs != null)
            {
                foreach (var telegraph in godAttackTelegraphs)
                {
                    telegraph?.SetState(false);
                }
            }

            yield return WaitForPlayersToChooseRoles();

            TeleportPlayersToRoleSpawns();
            TogglePlayerMovement(false);
            ShowStory("War Trial", "A corrupted God and Soldier pair descend to prove that unity can also be weaponized.");
            yield return new WaitForSeconds(3.3f);
            ShowStory("Mirror Duel", "To survive, the pair must counter the enemy duo in sequence: anchor, reveal, strike, then endure the swarm between phases.");
            yield return new WaitForSeconds(4.2f);
            HideStory();
            TogglePlayerMovement(true);

            m_Stage = WarTrialStage.CounterSoldierBoss;
            SetObjective("Anchor the corrupted Soldier, then let the God shatter the ward.",
                "The Executioner opens with sweeping volleys.");
            BroadcastNotification("War Trial begins.");
        }

        IEnumerator AttackTelegraphLoop()
        {
            int soldierIndex = 0;
            int godIndex = 0;

            while (true)
            {
                switch (m_Stage)
                {
                    case WarTrialStage.CounterSoldierBoss:
                    case WarTrialStage.SoldierBossVulnerable:
                        ToggleTelegraphSet(soldierAttackTelegraphs, soldierIndex);
                        soldierIndex = IncrementIndex(soldierAttackTelegraphs, soldierIndex);
                        break;

                    case WarTrialStage.CounterGodBoss:
                    case WarTrialStage.GodBossVulnerable:
                        ToggleTelegraphSet(godAttackTelegraphs, godIndex);
                        godIndex = IncrementIndex(godAttackTelegraphs, godIndex);
                        break;

                    default:
                        ToggleTelegraphSet(soldierAttackTelegraphs, -1);
                        ToggleTelegraphSet(godAttackTelegraphs, -1);
                        break;
                }

                yield return new WaitForSeconds(1.25f);
            }
        }

        void ToggleTelegraphSet(GodSoldierReplicatedActivator[] activators, int activeIndex)
        {
            if (activators == null)
            {
                return;
            }

            for (int i = 0; i < activators.Length; i++)
            {
                activators[i]?.SetState(i == activeIndex);
            }
        }

        static int IncrementIndex(GodSoldierReplicatedActivator[] activators, int current)
        {
            if (activators == null || activators.Length == 0)
            {
                return -1;
            }

            return (current + 1) % activators.Length;
        }

        void CompleteMission()
        {
            if (m_Stage == WarTrialStage.Complete)
            {
                return;
            }

            m_Stage = WarTrialStage.Complete;
            TogglePlayerMovement(false);
            ToggleTelegraphSet(soldierAttackTelegraphs, -1);
            ToggleTelegraphSet(godAttackTelegraphs, -1);
            ShowStory("Corrupted Mirror Broken", "The corrupted pair collapse. The true bond survives the trial and carries its scars toward judgment.");
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
