namespace GodSoldier
{
    public enum GodSoldierPlayerRole : byte
    {
        None,
        God,
        Soldier
    }

    public enum GodSoldierResourceType : byte
    {
        Scrap,
        Essence
    }

    public enum GodSoldierAlignmentChoice : byte
    {
        Neutral,
        Order,
        Agency
    }

    public enum GodSoldierObjectiveStage : byte
    {
        Lobby,
        Intro,
        GatherResources,
        CraftBridge,
        CrossBridge,
        CraftSigil,
        ArtilleryChoice,
        FinalChoice,
        Ending
    }

    public enum GodSoldierBlessingType : byte
    {
        None,
        BridgeBlessing,
        WarSigil
    }

    public static class GodSoldierSceneNames
    {
        public const string Bootstrap = "GodSoldier_Bootstrap";
        public const string MainMenu = "GodSoldier_MainMenu";
        public const string Lobby = "GodSoldier_Lobby";
        public const string Descent = "GodSoldier_Descent";
        public const string WarTrial = "GodSoldier_WarTrial";
        public const string Judgment = "GodSoldier_Judgment";
        public const string BattlefieldSlice = "GodSoldier_BattlefieldSlice";
    }

    [System.Serializable]
    public struct GodSoldierSliceSnapshot
    {
        public GodSoldierObjectiveStage objectiveStage;
        public int scrap;
        public int essence;
        public int orderScore;
        public int agencyScore;
        public bool bridgeCrafted;
        public bool sigilCrafted;
        public string objectiveText;
        public string storyTitle;
        public string storyBody;
        public bool storyVisible;
    }

    [System.Serializable]
    public struct GodSoldierMissionHudSnapshot
    {
        public string missionName;
        public string objectiveText;
        public string statusText;
        public string storyTitle;
        public string storyBody;
        public bool storyVisible;
    }
}
