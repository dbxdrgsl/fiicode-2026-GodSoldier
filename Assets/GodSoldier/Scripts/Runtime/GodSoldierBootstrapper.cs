using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace GodSoldier
{
    public class GodSoldierBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameObject networkManagerPrefab;
        [SerializeField] private GameObject unityServicesPrefab;
        [SerializeField] private GodSoldierMissionCatalog missionCatalog;
        [SerializeField] private string menuSceneName = GodSoldierSceneNames.MainMenu;

        void Awake()
        {
            EnsurePersistentPrefab(networkManagerPrefab, "god-soldier-network");
            EnsurePersistentPrefab(unityServicesPrefab, "god-soldier-services");
            EnsureGameFlowState();
        }

        IEnumerator Start()
        {
            yield return null;

            if (SceneManager.GetActiveScene().name == GodSoldierSceneNames.Bootstrap)
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }

        void EnsurePersistentPrefab(GameObject prefab, string persistenceId)
        {
            if (prefab == null)
            {
                return;
            }

            var persistentObjects = FindObjectsByType<GodSoldierPersistentObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var existing in persistentObjects)
            {
                if (existing.PersistenceId == persistenceId)
                {
                    return;
                }
            }

            var instance = Instantiate(prefab);
            var marker = instance.GetComponent<GodSoldierPersistentObject>();
            if (marker == null)
            {
                marker = instance.AddComponent<GodSoldierPersistentObject>();
            }

            marker.PersistenceId = persistenceId;

            if (instance.GetComponent<NetworkManager>() != null)
            {
                instance.name = "GodSoldier_NetworkRuntime";
            }
        }

        void EnsureGameFlowState()
        {
            if (GodSoldierGameFlowState.Instance != null)
            {
                GodSoldierGameFlowState.Instance.Initialize(missionCatalog);
                return;
            }

            var stateObject = new GameObject("GodSoldier_GameFlowState");
            var state = stateObject.AddComponent<GodSoldierGameFlowState>();
            state.Initialize(missionCatalog);
        }
    }
}
