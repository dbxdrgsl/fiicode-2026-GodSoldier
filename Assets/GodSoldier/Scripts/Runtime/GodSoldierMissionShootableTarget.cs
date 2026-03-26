using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace GodSoldier
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider))]
    public class GodSoldierMissionShootableTarget : HitProcessor
    {
        [SerializeField] string targetId = "shootable";
        [SerializeField] int health = 3;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] Color readyColor = new(0.69f, 0.25f, 0.19f);
        [SerializeField] Color damagedColor = new(0.95f, 0.70f, 0.25f);
        [SerializeField] Color defeatedColor = new(0.19f, 0.25f, 0.31f);
        [SerializeField] bool disableColliderWhenDefeated = true;

        int m_CurrentHealth;
        Collider m_Collider;
        MaterialPropertyBlock m_PropertyBlock;

        void Awake()
        {
            m_CurrentHealth = Mathf.Max(1, health);
            m_Collider = GetComponent<Collider>();

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            m_PropertyBlock = new MaterialPropertyBlock();
            ApplyTint(readyColor);
        }

        protected override void HandleHit(HitInfo info)
        {
            if (m_CurrentHealth <= 0)
            {
                return;
            }

            m_CurrentHealth--;

            if (m_CurrentHealth <= 0)
            {
                GodSoldierMissionDirectorBase.Current?.NotifyShootableResolved(targetId, info.attackerId);
                ResolveDefeatRpc();
                return;
            }

            UpdateDamageStateRpc();
        }

        [Rpc(SendTo.Everyone)]
        void UpdateDamageStateRpc()
        {
            ApplyTint(damagedColor);
        }

        [Rpc(SendTo.Everyone)]
        void ResolveDefeatRpc()
        {
            ApplyTint(defeatedColor);

            if (disableColliderWhenDefeated && m_Collider != null)
            {
                m_Collider.enabled = false;
            }
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
