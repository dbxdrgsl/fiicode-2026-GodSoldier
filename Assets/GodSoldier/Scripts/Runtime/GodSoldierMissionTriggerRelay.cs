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
        [SerializeField] bool enforceRoleRequirement;
        [SerializeField] bool hideAfterTrigger;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var networkObject = other.GetComponentInParent<NetworkObject>();
            if (networkObject == null || !networkObject.IsOwner)
            {
                return;
            }

            var playerState = other.GetComponentInParent<CorePlayerState>();
            if (enforceRoleRequirement && requiredRole != GodSoldierPlayerRole.None && (playerState == null || playerState.PlayerRole != requiredRole))
            {
                return;
            }

            GodSoldierMissionDirectorBase.Current?.RequestTrigger(triggerId, networkObject.OwnerClientId);
            if (hideAfterTrigger)
            {
                HideForEveryone();
            }
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
