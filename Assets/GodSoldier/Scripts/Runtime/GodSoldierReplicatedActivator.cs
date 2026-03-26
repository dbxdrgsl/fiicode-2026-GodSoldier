using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    public class GodSoldierReplicatedActivator : NetworkBehaviour
    {
        [SerializeField] GameObject[] targets;
        [SerializeField] bool initialState = true;

        void Start()
        {
            ApplyState(initialState);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ApplyState(initialState);
        }

        public void SetState(bool state)
        {
            initialState = state;

            if (NetworkObject == null || !NetworkObject.IsSpawned)
            {
                ApplyState(state);
                return;
            }

            if (IsServer)
            {
                ApplyStateRpc(state);
            }
            else
            {
                RequestStateRpc(state);
            }
        }

        void ApplyState(bool state)
        {
            if (targets == null)
            {
                return;
            }

            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.SetActive(state);
                }
            }
        }

        [Rpc(SendTo.Authority)]
        void RequestStateRpc(bool state)
        {
            ApplyStateRpc(state);
        }

        [Rpc(SendTo.Everyone)]
        void ApplyStateRpc(bool state)
        {
            initialState = state;
            ApplyState(state);
        }
    }
}
