using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierGameFlowState : MonoBehaviour
    {
        const string k_MissionCompletePrefix = "god-soldier.mission.complete.";

        public static GodSoldierGameFlowState Instance { get; private set; }

        [SerializeField] GodSoldierMissionCatalog missionCatalog;
        [SerializeField] string selectedMissionId = "descent";

        readonly HashSet<string> m_CompletedMissionIds = new();

        public GodSoldierMissionCatalog MissionCatalog => missionCatalog;
        public string SelectedMissionId => selectedMissionId;
        public GodSoldierMissionDefinition SelectedMission => missionCatalog != null ? missionCatalog.GetMissionById(selectedMissionId) : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMissionCompletion();

            if (missionCatalog != null && missionCatalog.GetMissionById(selectedMissionId) == null)
            {
                selectedMissionId = missionCatalog.GetMissionsInOrder().FirstOrDefault()?.MissionId;
            }
        }

        public void Initialize(GodSoldierMissionCatalog catalog)
        {
            missionCatalog = catalog;
            LoadMissionCompletion();

            if (missionCatalog != null && missionCatalog.GetMissionById(selectedMissionId) == null)
            {
                selectedMissionId = missionCatalog.GetMissionsInOrder().FirstOrDefault()?.MissionId;
            }
        }

        public IReadOnlyList<GodSoldierMissionDefinition> GetMissionsInOrder()
        {
            return missionCatalog != null ? missionCatalog.GetMissionsInOrder().ToList() : new List<GodSoldierMissionDefinition>();
        }

        public GodSoldierMissionDefinition GetMissionById(string missionId)
        {
            return missionCatalog != null ? missionCatalog.GetMissionById(missionId) : null;
        }

        public GodSoldierMissionDefinition GetRecommendedMission()
        {
            if (missionCatalog == null)
            {
                return null;
            }

            foreach (var mission in missionCatalog.GetMissionsInOrder())
            {
                if (!IsMissionCompleted(mission.MissionId))
                {
                    return mission;
                }
            }

            return missionCatalog.GetMissionsInOrder().LastOrDefault();
        }

        public bool IsOutOfOrder(GodSoldierMissionDefinition mission)
        {
            if (mission == null || IsMissionCompleted(mission.MissionId))
            {
                return false;
            }

            var recommendedMission = GetRecommendedMission();
            return recommendedMission != null && recommendedMission.MissionId != mission.MissionId;
        }

        public void SelectMission(string missionId)
        {
            if (missionCatalog == null)
            {
                selectedMissionId = missionId;
                return;
            }

            var mission = missionCatalog.GetMissionById(missionId);
            if (mission != null)
            {
                selectedMissionId = mission.MissionId;
            }
        }

        public bool IsMissionCompleted(string missionId)
        {
            return !string.IsNullOrWhiteSpace(missionId) && m_CompletedMissionIds.Contains(missionId);
        }

        public void MarkMissionCompleted(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId) || m_CompletedMissionIds.Contains(missionId))
            {
                return;
            }

            m_CompletedMissionIds.Add(missionId);
            PlayerPrefs.SetInt(k_MissionCompletePrefix + missionId, 1);
            PlayerPrefs.Save();
        }

        void LoadMissionCompletion()
        {
            m_CompletedMissionIds.Clear();

            if (missionCatalog == null)
            {
                return;
            }

            foreach (var mission in missionCatalog.GetMissionsInOrder())
            {
                if (PlayerPrefs.GetInt(k_MissionCompletePrefix + mission.MissionId, 0) == 1)
                {
                    m_CompletedMissionIds.Add(mission.MissionId);
                }
            }
        }
    }
}
