using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public class GodSoldierResourcePickup : NetworkBehaviour
    {
        [SerializeField] private GodSoldierResourceType resourceType = GodSoldierResourceType.Scrap;
        [SerializeField] private int amount = 1;
        [SerializeField] private GodSoldierPlayerRole requiredRole = GodSoldierPlayerRole.Soldier;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;

        Vector3 m_StartPosition;
        bool m_Consumed;

        void Awake()
        {
            m_StartPosition = transform.position;
            GetComponent<Collider>().isTrigger = true;
        }

        void Update()
        {
            if (m_Consumed)
            {
                return;
            }

            transform.position = m_StartPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (m_Consumed || !other.TryGetComponent<NetworkObject>(out var networkObject) || !networkObject.IsOwner)
            {
                return;
            }

            RequestCollectRpc(networkObject.OwnerClientId);
        }

        [Rpc(SendTo.Authority)]
        void RequestCollectRpc(ulong playerId)
        {
            if (m_Consumed || GodSoldierSliceDirector.Instance == null)
            {
                return;
            }

            var playerObject = NetworkManager.Singleton?.SpawnManager?.GetPlayerNetworkObject(playerId);
            if (playerObject == null)
            {
                return;
            }

            var state = playerObject.GetComponent<CorePlayerState>();
            if (state == null || state.PlayerRole != requiredRole)
            {
                return;
            }

            if (!GodSoldierSliceDirector.Instance.TryAddResource(resourceType, amount, playerId))
            {
                return;
            }

            m_Consumed = true;
            HidePickupRpc();

            if (IsSpawned)
            {
                NetworkObject.Despawn();
            }
        }

        [Rpc(SendTo.Everyone)]
        void HidePickupRpc()
        {
            m_Consumed = true;
            gameObject.SetActive(false);
        }
    }
}
