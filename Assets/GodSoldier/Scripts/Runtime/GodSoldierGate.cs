using UnityEngine;

namespace GodSoldier
{
    public class GodSoldierGate : MonoBehaviour
    {
        [SerializeField] private GodSoldierBlessingType requiredBlessing = GodSoldierBlessingType.BridgeBlessing;
        [SerializeField] private GameObject[] deactivateWhenOpen;
        [SerializeField] private GameObject[] activateWhenOpen;

        bool m_IsOpen;

        void Update()
        {
            if (m_IsOpen || GodSoldierSliceDirector.Instance == null)
            {
                return;
            }

            var shouldOpen = requiredBlessing switch
            {
                GodSoldierBlessingType.BridgeBlessing => GodSoldierSliceDirector.Instance.BridgeCrafted,
                GodSoldierBlessingType.WarSigil => GodSoldierSliceDirector.Instance.SigilCrafted,
                _ => false
            };

            if (!shouldOpen)
            {
                return;
            }

            m_IsOpen = true;
            foreach (var target in deactivateWhenOpen)
            {
                if (target != null) target.SetActive(false);
            }

            foreach (var target in activateWhenOpen)
            {
                if (target != null) target.SetActive(true);
            }
        }
    }
}
