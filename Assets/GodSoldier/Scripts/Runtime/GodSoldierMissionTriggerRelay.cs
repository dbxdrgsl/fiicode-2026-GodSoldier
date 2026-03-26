using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public class GodSoldierMissionTriggerRelay : NetworkBehaviour
    {
        [SerializeField] string triggerId = "mission-trigger";
        [SerializeField] GodSoldierPlayerRole requiredRole = GodSoldierPlayerRole.None;
        [SerializeField] bool hideAfterTrigger;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<NetworkObject>(out var networkObject) || !networkObject.IsOwner)
            {
                return;
            }

            var playerState = other.GetComponent<CorePlayerState>();
            if (requiredRole != GodSoldierPlayerRole.None && (playerState == null || playerState.PlayerRole != requiredRole))
            {
                return;
            }

            GodSoldierMissionDirectorBase.Current?.RequestTrigger(triggerId, networkObject.OwnerClientId);
        }

        public void HideForEveryone()
        {
            if (hideAfterTrigger)
            {
                HideForEveryoneRpc();
            }
        }

        [Rpc(SendTo.Everyone)]
        void HideForEveryoneRpc()
        {
            gameObject.SetActive(false);
        }
    }
}
