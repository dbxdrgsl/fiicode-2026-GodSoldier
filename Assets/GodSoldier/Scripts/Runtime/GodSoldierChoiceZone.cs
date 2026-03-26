using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public class GodSoldierChoiceZone : NetworkBehaviour
    {
        [SerializeField] private string choiceId = "artillery";
        [SerializeField] private GodSoldierAlignmentChoice alignment = GodSoldierAlignmentChoice.Order;
        [SerializeField] [TextArea] private string summary = "A choice has been recorded.";

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

            SubmitChoiceRpc();
        }

        [Rpc(SendTo.Authority)]
        void SubmitChoiceRpc()
        {
            if (GodSoldierSliceDirector.Instance != null &&
                GodSoldierSliceDirector.Instance.RegisterChoice(choiceId, alignment, summary))
            {
                MarkChoiceResolvedRpc();
            }
        }

        [Rpc(SendTo.Everyone)]
        void MarkChoiceResolvedRpc()
        {
            gameObject.SetActive(false);
        }
    }
}
