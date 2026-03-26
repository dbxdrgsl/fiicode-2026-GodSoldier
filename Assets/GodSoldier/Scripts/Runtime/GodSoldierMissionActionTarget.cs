using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(Collider))]
    public class GodSoldierMissionActionTarget : MonoBehaviour
    {
        [SerializeField] string actionId = "primary-action";
        [SerializeField] GodSoldierPlayerRole requiredRole = GodSoldierPlayerRole.None;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color idleColor = new(0.40f, 0.49f, 0.63f);
        [SerializeField] Color activeColor = new(0.90f, 0.77f, 0.46f);

        MaterialPropertyBlock m_PropertyBlock;

        void Awake()
        {
            var collider = GetComponent<Collider>();
            collider.isTrigger = true;

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            m_PropertyBlock = new MaterialPropertyBlock();
            ApplyTint(idleColor);
        }

        public bool TryHandlePrimaryActionLocal(ulong playerId, GodSoldierPlayerRole playerRole)
        {
            if (requiredRole != GodSoldierPlayerRole.None && requiredRole != playerRole)
            {
                return false;
            }

            if (GodSoldierMissionDirectorBase.Current == null)
            {
                return false;
            }

            GodSoldierMissionDirectorBase.Current.RequestAction(actionId, playerId);
            return true;
        }

        public void SetHighlighted(bool highlighted)
        {
            ApplyTint(highlighted ? activeColor : idleColor);
        }

        void ApplyTint(Color tint)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor("_BaseColor", tint);
            m_PropertyBlock.SetColor("_Color", tint);
            targetRenderer.SetPropertyBlock(m_PropertyBlock);
        }
    }
}
