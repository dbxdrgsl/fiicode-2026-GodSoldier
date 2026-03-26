using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public class GodSoldierCraftingShrine : NetworkBehaviour
    {
        [SerializeField] private MeshRenderer shrineRenderer;
        [SerializeField] private Color inactiveColor = new(0.35f, 0.46f, 0.58f);
        [SerializeField] private Color activeColor = new(0.92f, 0.79f, 0.52f);

        MaterialPropertyBlock m_PropertyBlock;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (shrineRenderer == null || GodSoldierSliceDirector.Instance == null)
            {
                return;
            }

            var color = GodSoldierSliceDirector.Instance.ObjectiveStage switch
            {
                GodSoldierObjectiveStage.CraftBridge => activeColor,
                GodSoldierObjectiveStage.CraftSigil => activeColor,
                _ => inactiveColor
            };

            shrineRenderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor("_BaseColor", color);
            m_PropertyBlock.SetColor("_Color", color);
            shrineRenderer.SetPropertyBlock(m_PropertyBlock);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<NetworkObject>(out var networkObject) || !networkObject.IsOwner)
            {
                return;
            }

            RequestCraftRpc(networkObject.OwnerClientId);
        }

        [Rpc(SendTo.Authority)]
        void RequestCraftRpc(ulong playerId)
        {
            GodSoldierSliceDirector.Instance?.TryCraftBlessing(playerId);
        }
    }
}
