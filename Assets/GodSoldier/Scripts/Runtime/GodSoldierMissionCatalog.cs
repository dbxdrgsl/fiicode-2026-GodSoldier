using System;
using System.Collections.Generic;
using System.Linq;
using Blocks.Sessions.Common;
using UnityEngine;

namespace GodSoldier
{
    [Serializable]
    public class GodSoldierMissionDefinition
    {
        [SerializeField] string missionId = "descent";
        [SerializeField] string displayName = "Descent";
        [SerializeField] string headline = "The bond is forged in a dying trench.";
        [SerializeField] [TextArea(3, 6)] string description = "A God descends, revives a fallen Soldier, and teaches the pair to move as one.";
        [SerializeField] int recommendedOrder = 1;
        [SerializeField] string sceneName = GodSoldierSceneNames.Descent;
        [SerializeField] [TextArea(2, 4)] string outOfOrderWarning = "This mission is meant to be played later. The game recommends starting earlier in the timeline.";
        [SerializeField] bool allowPublicMatch = true;
        [SerializeField] bool allowPrivateMatch = true;
        [SerializeField] int maxHumanPlayers = 2;
        [SerializeField] Color accentColor = new(0.82f, 0.62f, 0.38f);
        [SerializeField] SessionSettings publicSessionSettings;
        [SerializeField] SessionSettings privateSessionSettings;

        public string MissionId => missionId;
        public string DisplayName => displayName;
        public string Headline => headline;
        public string Description => description;
        public int RecommendedOrder => recommendedOrder;
        public string SceneName => sceneName;
        public string OutOfOrderWarning => outOfOrderWarning;
        public bool AllowPublicMatch => allowPublicMatch;
        public bool AllowPrivateMatch => allowPrivateMatch;
        public int MaxHumanPlayers => maxHumanPlayers;
        public Color AccentColor => accentColor;
        public SessionSettings PublicSessionSettings => publicSessionSettings;
        public SessionSettings PrivateSessionSettings => privateSessionSettings;
    }

    [CreateAssetMenu(fileName = nameof(GodSoldierMissionCatalog), menuName = "God Soldier/Game/" + nameof(GodSoldierMissionCatalog))]
    public class GodSoldierMissionCatalog : ScriptableObject
    {
        [SerializeField] List<GodSoldierMissionDefinition> missions = new();

        public IReadOnlyList<GodSoldierMissionDefinition> Missions => missions;

        public IEnumerable<GodSoldierMissionDefinition> GetMissionsInOrder()
        {
            return missions.OrderBy(mission => mission.RecommendedOrder);
        }

        public GodSoldierMissionDefinition GetMissionById(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return null;
            }

            return missions.FirstOrDefault(mission => string.Equals(mission.MissionId, missionId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
