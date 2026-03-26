using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GodSoldier
{
    [RequireComponent(typeof(UIDocument))]
    public class GodSoldierMenuController : MonoBehaviour
    {
        const string k_MasterVolumeKey = "god-soldier.settings.master-volume";
        const string k_QualityKey = "god-soldier.settings.quality-index";
        const string k_FullscreenKey = "god-soldier.settings.fullscreen";

        UIDocument m_Document;
        VisualElement m_MissionPanel;
        VisualElement m_SettingsModal;
        VisualElement m_WarningModal;
        VisualElement m_MissionTimeline;
        Label m_RecommendedLabel;
        Label m_WarningLabel;
        DropdownField m_QualityField;
        Slider m_MasterVolumeSlider;
        Toggle m_FullscreenToggle;

        string m_PendingMissionId;

        void Awake()
        {
            m_Document = GetComponent<UIDocument>();
        }

        void Start()
        {
            var root = m_Document.rootVisualElement;

            root.Q<Button>("play-button")?.RegisterCallback<ClickEvent>(_ => SetMissionPanelVisible(true));
            root.Q<Button>("settings-button")?.RegisterCallback<ClickEvent>(_ => SetSettingsVisible(true));
            root.Q<Button>("exit-button")?.RegisterCallback<ClickEvent>(_ => ExitGame());
            root.Q<Button>("missions-back-button")?.RegisterCallback<ClickEvent>(_ => SetMissionPanelVisible(false));
            root.Q<Button>("settings-close-button")?.RegisterCallback<ClickEvent>(_ => SetSettingsVisible(false));
            root.Q<Button>("recommended-button")?.RegisterCallback<ClickEvent>(_ => LaunchRecommendedMission());
            root.Q<Button>("play-anyway-button")?.RegisterCallback<ClickEvent>(_ => ConfirmPendingMission());
            root.Q<Button>("warning-close-button")?.RegisterCallback<ClickEvent>(_ => SetWarningVisible(false));

            m_MissionPanel = root.Q<VisualElement>("mission-panel");
            m_SettingsModal = root.Q<VisualElement>("settings-modal");
            m_WarningModal = root.Q<VisualElement>("warning-modal");
            m_MissionTimeline = root.Q<VisualElement>("mission-timeline");
            m_RecommendedLabel = root.Q<Label>("recommended-label");
            m_WarningLabel = root.Q<Label>("warning-label");
            m_QualityField = root.Q<DropdownField>("quality-field");
            m_MasterVolumeSlider = root.Q<Slider>("volume-slider");
            m_FullscreenToggle = root.Q<Toggle>("fullscreen-toggle");

            SetMissionPanelVisible(false);
            SetSettingsVisible(false);
            SetWarningVisible(false);
            BuildMissionTimeline();
            SetupSettings();
        }

        void BuildMissionTimeline()
        {
            if (m_MissionTimeline == null)
            {
                return;
            }

            m_MissionTimeline.Clear();

            var flowState = GodSoldierGameFlowState.Instance;
            var missions = flowState != null ? flowState.GetMissionsInOrder() : new List<GodSoldierMissionDefinition>();
            var recommendedMission = flowState?.GetRecommendedMission();

            if (recommendedMission != null && m_RecommendedLabel != null)
            {
                m_RecommendedLabel.text = $"Recommended first: {recommendedMission.DisplayName}";
            }

            foreach (var mission in missions)
            {
                var tile = CreateMissionTile(mission, recommendedMission, flowState);
                m_MissionTimeline.Add(tile);
            }
        }

        VisualElement CreateMissionTile(GodSoldierMissionDefinition mission, GodSoldierMissionDefinition recommendedMission, GodSoldierGameFlowState flowState)
        {
            var tile = new Button(() => TryOpenMission(mission))
            {
                name = $"mission-{mission.MissionId}"
            };
            tile.AddToClassList("mission-tile");

            tile.style.borderLeftColor = mission.AccentColor;
            tile.style.borderRightColor = mission.AccentColor;
            tile.style.borderTopColor = mission.AccentColor;
            tile.style.borderBottomColor = mission.AccentColor;

            var orderLabel = new Label($"MISSION {mission.RecommendedOrder}");
            orderLabel.AddToClassList("mission-order");

            var titleLabel = new Label(mission.DisplayName);
            titleLabel.AddToClassList("mission-title");

            var headlineLabel = new Label(mission.Headline);
            headlineLabel.AddToClassList("mission-headline");

            var descriptionLabel = new Label(mission.Description);
            descriptionLabel.AddToClassList("mission-description");

            tile.Add(orderLabel);
            tile.Add(titleLabel);
            tile.Add(headlineLabel);
            tile.Add(descriptionLabel);

            if (recommendedMission != null && mission.MissionId == recommendedMission.MissionId)
            {
                var recommendedTag = new Label("Recommended");
                recommendedTag.AddToClassList("mission-tag");
                tile.Add(recommendedTag);
            }

            if (flowState != null && flowState.IsMissionCompleted(mission.MissionId))
            {
                var completedTag = new Label("Completed");
                completedTag.AddToClassList("mission-tag");
                completedTag.AddToClassList("mission-tag--complete");
                tile.Add(completedTag);
            }

            return tile;
        }

        void SetupSettings()
        {
            var qualityNames = QualitySettings.names.ToList();
            if (m_QualityField != null)
            {
                m_QualityField.choices = qualityNames;
                int qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(k_QualityKey, QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, qualityNames.Count - 1));
                if (qualityNames.Count > 0)
                {
                    m_QualityField.value = qualityNames[qualityIndex];
                }

                m_QualityField.RegisterValueChangedCallback(evt =>
                {
                    int index = qualityNames.IndexOf(evt.newValue);
                    if (index >= 0)
                    {
                        QualitySettings.SetQualityLevel(index, true);
                        PlayerPrefs.SetInt(k_QualityKey, index);
                        PlayerPrefs.Save();
                    }
                });
            }

            if (m_MasterVolumeSlider != null)
            {
                float savedVolume = PlayerPrefs.GetFloat(k_MasterVolumeKey, 0.75f);
                m_MasterVolumeSlider.value = savedVolume;
                AudioListener.volume = savedVolume;
                m_MasterVolumeSlider.RegisterValueChangedCallback(evt =>
                {
                    AudioListener.volume = evt.newValue;
                    PlayerPrefs.SetFloat(k_MasterVolumeKey, evt.newValue);
                    PlayerPrefs.Save();
                });
            }

            if (m_FullscreenToggle != null)
            {
                bool fullscreen = PlayerPrefs.GetInt(k_FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
                m_FullscreenToggle.value = fullscreen;
                Screen.fullScreen = fullscreen;
                m_FullscreenToggle.RegisterValueChangedCallback(evt =>
                {
                    Screen.fullScreen = evt.newValue;
                    PlayerPrefs.SetInt(k_FullscreenKey, evt.newValue ? 1 : 0);
                    PlayerPrefs.Save();
                });
            }
        }

        void TryOpenMission(GodSoldierMissionDefinition mission)
        {
            if (mission == null || GodSoldierGameFlowState.Instance == null)
            {
                return;
            }

            if (GodSoldierGameFlowState.Instance.IsOutOfOrder(mission))
            {
                var recommended = GodSoldierGameFlowState.Instance.GetRecommendedMission();
                m_PendingMissionId = mission.MissionId;
                if (m_WarningLabel != null)
                {
                    m_WarningLabel.text = recommended == null
                        ? mission.OutOfOrderWarning
                        : $"{mission.OutOfOrderWarning}\n\nRecommended now: {recommended.DisplayName}.";
                }

                SetWarningVisible(true);
                return;
            }

            LaunchMission(mission.MissionId);
        }

        void LaunchRecommendedMission()
        {
            var recommended = GodSoldierGameFlowState.Instance?.GetRecommendedMission();
            if (recommended != null)
            {
                LaunchMission(recommended.MissionId);
            }
        }

        void ConfirmPendingMission()
        {
            if (!string.IsNullOrWhiteSpace(m_PendingMissionId))
            {
                LaunchMission(m_PendingMissionId);
            }
        }

        void LaunchMission(string missionId)
        {
            var flowState = GodSoldierGameFlowState.Instance;
            if (flowState == null)
            {
                return;
            }

            flowState.SelectMission(missionId);
            SetWarningVisible(false);
            SceneManager.LoadScene(GodSoldierSceneNames.Lobby);
        }

        void SetMissionPanelVisible(bool visible)
        {
            if (m_MissionPanel != null)
            {
                m_MissionPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void SetSettingsVisible(bool visible)
        {
            if (m_SettingsModal != null)
            {
                m_SettingsModal.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void SetWarningVisible(bool visible)
        {
            if (m_WarningModal != null)
            {
                m_WarningModal.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
