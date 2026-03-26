using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierPersistentObject : MonoBehaviour
    {
        [SerializeField] private string persistenceId = "persistent";

        public string PersistenceId
        {
            get => persistenceId;
            set => persistenceId = value;
        }

        void Awake()
        {
            var existing = FindObjectsByType<GodSoldierPersistentObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in existing)
            {
                if (candidate == this)
                {
                    continue;
                }

                if (candidate.persistenceId == persistenceId)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
