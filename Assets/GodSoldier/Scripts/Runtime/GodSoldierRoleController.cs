using Blocks.Gameplay.Core;
using Blocks.Gameplay.Shooter;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GodSoldier
{
    [RequireComponent(typeof(CorePlayerState))]
    [RequireComponent(typeof(CoreMovement))]
    public class GodSoldierRoleController : NetworkBehaviour
    {
        [Header("Optional References")]
        [SerializeField] private CorePlayerState corePlayerState;
        [SerializeField] private CoreMovement coreMovement;
        [SerializeField] private CorePlayerManager corePlayerManager;
        [SerializeField] private ShooterAddon shooterAddon;
        [SerializeField] private ShooterInputHandler shooterInputHandler;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private AimController aimController;
        [SerializeField] private GameEvent onPrimaryActionPressed;
        [SerializeField] private NotificationEvent onNotification;
        [SerializeField] private UIDocument playerHudDocument;
        [SerializeField] private Renderer[] roleRenderers;
        [SerializeField] private float primaryActionRange = 2.4f;
        [SerializeField] private bool enableMissionPrimaryActions;

        [Header("Soldier Tuning")]
        [SerializeField] private float soldierMoveSpeed = 4.5f;
        [SerializeField] private float soldierSprintSpeed = 8.5f;
        [SerializeField] private float soldierJumpHeight = 1.5f;
        [SerializeField] private float soldierGravity = -15f;

        [Header("God Tuning")]
        [SerializeField] private float godMoveSpeed = 5.25f;
        [SerializeField] private float godSprintSpeed = 5.75f;
        [SerializeField] private float godJumpHeight = 2.4f;
        [SerializeField] private float godGravity = -8f;

        [Header("Visual Identity")]
        [SerializeField] private Color soldierTint = new Color(0.80f, 0.78f, 0.72f);
        [SerializeField] private Color godTint = new Color(0.55f, 0.82f, 1f);

        float m_DefaultMoveSpeed;
        float m_DefaultSprintSpeed;
        float m_DefaultJumpHeight;
        float m_DefaultGravity;
        MaterialPropertyBlock m_PropertyBlock;
        bool[] m_DefaultRendererStates;

        void Awake()
        {
            if (corePlayerState == null) corePlayerState = GetComponent<CorePlayerState>();
            if (coreMovement == null) coreMovement = GetComponent<CoreMovement>();
            if (corePlayerManager == null) corePlayerManager = GetComponent<CorePlayerManager>();
            if (shooterAddon == null) shooterAddon = GetComponent<ShooterAddon>();
            if (shooterInputHandler == null) shooterInputHandler = GetComponent<ShooterInputHandler>();
            if (weaponController == null) weaponController = GetComponent<WeaponController>();
            if (aimController == null) aimController = GetComponent<AimController>();
            if (playerHudDocument == null) playerHudDocument = GetComponent<UIDocument>();
            if (roleRenderers == null || roleRenderers.Length == 0)
            {
                roleRenderers = GetComponentsInChildren<Renderer>(true);
            }

            m_DefaultMoveSpeed = coreMovement != null ? coreMovement.moveSpeed : 4f;
            m_DefaultSprintSpeed = coreMovement != null ? coreMovement.sprintSpeed : 6f;
            m_DefaultJumpHeight = coreMovement != null ? coreMovement.jumpHeight : 1.2f;
            m_DefaultGravity = coreMovement != null ? coreMovement.gravity : -15f;
            m_PropertyBlock = new MaterialPropertyBlock();
            m_DefaultRendererStates = new bool[roleRenderers.Length];
            for (int i = 0; i < roleRenderers.Length; i++)
            {
                m_DefaultRendererStates[i] = roleRenderers[i] != null && roleRenderers[i].enabled;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;

            if (corePlayerState != null)
            {
                corePlayerState.OnRoleChanged += HandleRoleChanged;
                HandleRoleChanged(corePlayerState.PlayerRole);
            }

            if (IsOwner && enableMissionPrimaryActions && onPrimaryActionPressed != null)
            {
                onPrimaryActionPressed.RegisterListener(HandlePrimaryActionPressed);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (corePlayerState != null)
            {
                corePlayerState.OnRoleChanged -= HandleRoleChanged;
            }

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

            if (IsOwner && enableMissionPrimaryActions && onPrimaryActionPressed != null)
            {
                onPrimaryActionPressed.UnregisterListener(HandlePrimaryActionPressed);
            }

            base.OnNetworkDespawn();
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            TeleportOwnerRpc(position, rotation);
        }

        void HandlePrimaryActionPressed()
        {
            if (corePlayerState == null)
            {
                return;
            }

            if (TryInvokeNearbyMissionAction())
            {
                return;
            }

            var director = GodSoldierMissionDirectorBase.Current;
            if (director == null || onNotification == null)
            {
                return;
            }

            onNotification.Raise(new NotificationPayload
            {
                clientId = OwnerClientId,
                message = director.GetHintForRole(corePlayerState.PlayerRole)
            });
        }

        bool TryInvokeNearbyMissionAction()
        {
            var hits = Physics.OverlapSphere(transform.position, primaryActionRange, ~0, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            GodSoldierMissionActionTarget bestTarget = null;
            float bestDistance = float.MaxValue;
            var seenTargets = new HashSet<GodSoldierMissionActionTarget>();

            foreach (var hit in hits)
            {
                if (hit == null || !hit.TryGetComponent(out GodSoldierMissionActionTarget actionTarget) || !seenTargets.Add(actionTarget))
                {
                    continue;
                }

                float distance = (actionTarget.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = actionTarget;
                }
            }

            return bestTarget != null && bestTarget.TryHandlePrimaryActionLocal(OwnerClientId, corePlayerState.PlayerRole);
        }

        void HandleRoleChanged(GodSoldierPlayerRole role)
        {
            switch (role)
            {
                case GodSoldierPlayerRole.God:
                    ApplyMovement(godMoveSpeed, godSprintSpeed, godJumpHeight, godGravity);
                    ApplyShooterState(true);
                    ApplyTint(godTint);
                    break;

                case GodSoldierPlayerRole.Soldier:
                    ApplyMovement(soldierMoveSpeed, soldierSprintSpeed, soldierJumpHeight, soldierGravity);
                    ApplyShooterState(true);
                    ApplyTint(soldierTint);
                    break;

                default:
                    ApplyMovement(m_DefaultMoveSpeed, m_DefaultSprintSpeed, m_DefaultJumpHeight, m_DefaultGravity);
                    ApplyShooterState(true);
                    ApplyTint(Color.white);
                    break;
            }

            ApplyScenePresentationState();
        }

        void ApplyMovement(float moveSpeed, float sprintSpeed, float jumpHeight, float gravity)
        {
            if (coreMovement == null)
            {
                return;
            }

            coreMovement.moveSpeed = moveSpeed;
            coreMovement.sprintSpeed = sprintSpeed;
            coreMovement.jumpHeight = jumpHeight;
            coreMovement.gravity = gravity;
        }

        void ApplyShooterState(bool enabledState)
        {
            if (weaponController != null)
            {
                weaponController.enabled = enabledState;
                weaponController.SetCurrentWeaponActive(enabledState);
            }

            if (aimController != null)
            {
                aimController.enabled = enabledState;
            }

            if (shooterInputHandler != null)
            {
                shooterInputHandler.enabled = enabledState;
            }

            if (shooterAddon != null)
            {
                shooterAddon.enabled = enabledState;

                if (shooterAddon.WeaponHUD != null)
                {
                    shooterAddon.WeaponHUD.enabled = enabledState;
                }

                if (shooterAddon.ShooterAnimator != null)
                {
                    shooterAddon.ShooterAnimator.enabled = enabledState;
                }
            }
        }

        void ApplyTint(Color tint)
        {
            if (roleRenderers == null)
            {
                return;
            }

            foreach (var targetRenderer in roleRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(m_PropertyBlock);
                m_PropertyBlock.SetColor("_BaseColor", tint);
                m_PropertyBlock.SetColor("_Color", tint);
                targetRenderer.SetPropertyBlock(m_PropertyBlock);
            }
        }

        void ApplyScenePresentationState()
        {
            bool suppressGameplay = SceneManager.GetActiveScene().name == GodSoldierSceneNames.Lobby;
            bool isActiveGameplayState = !suppressGameplay && corePlayerState != null && corePlayerState.IsActive;

            if (corePlayerManager != null)
            {
                corePlayerManager.SetMovementInputEnabled(isActiveGameplayState);

                if (IsOwner)
                {
                    if (corePlayerManager.CoreInput != null)
                    {
                        corePlayerManager.CoreInput.enabled = isActiveGameplayState;
                    }

                    if (corePlayerManager.CoreCamera != null)
                    {
                        corePlayerManager.CoreCamera.enabled = isActiveGameplayState;
                        if (suppressGameplay)
                        {
                            foreach (var cameraMode in corePlayerManager.CoreCamera.GetRegisteredCameraModes())
                            {
                                cameraMode?.SetActive(false);
                            }
                        }
                        else if (isActiveGameplayState)
                        {
                            RestoreActiveCameraMode(corePlayerManager.CoreCamera);
                        }
                    }
                }
            }

            if (coreMovement != null)
            {
                coreMovement.IsMovementEnabled = isActiveGameplayState;
                if (!isActiveGameplayState)
                {
                    coreMovement.ResetMovementForces();
                }

                var characterController = coreMovement.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = isActiveGameplayState;
                }
            }

            if (suppressGameplay)
            {
                ApplyShooterState(false);
            }

            ApplyRendererVisibility(!suppressGameplay);
        }

        void RestoreActiveCameraMode(CoreCameraController cameraController)
        {
            if (cameraController == null)
            {
                return;
            }

            if (cameraController.ActiveCameraMode != null)
            {
                cameraController.SwitchCameraMode(cameraController.ActiveCameraMode.ModeName);
                if (coreMovement != null)
                {
                    coreMovement.PlayerRotationMode = cameraController.CurrentPlayerRotationMode;
                }
                return;
            }

            var registeredModes = cameraController.GetRegisteredCameraModes();
            if (registeredModes == null || registeredModes.Count == 0 || registeredModes[0] == null)
            {
                return;
            }

            cameraController.SwitchCameraMode(registeredModes[0].ModeName);
            if (coreMovement != null)
            {
                coreMovement.PlayerRotationMode = cameraController.CurrentPlayerRotationMode;
            }
        }

        void ApplyRendererVisibility(bool visible)
        {
            if (roleRenderers == null)
            {
                return;
            }

            for (int i = 0; i < roleRenderers.Length; i++)
            {
                var targetRenderer = roleRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                bool defaultVisible = m_DefaultRendererStates != null && i < m_DefaultRendererStates.Length && m_DefaultRendererStates[i];
                targetRenderer.enabled = visible && defaultVisible;
            }
        }

        void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            HandleRoleChanged(corePlayerState != null ? corePlayerState.PlayerRole : GodSoldierPlayerRole.None);
        }

        [Rpc(SendTo.Owner)]
        void TeleportOwnerRpc(Vector3 position, Quaternion rotation)
        {
            if (coreMovement == null)
            {
                return;
            }

            transform.rotation = rotation;
            coreMovement.SetPosition(position);
            coreMovement.ResetMovementForces();

            if (corePlayerManager != null)
            {
                corePlayerManager.SetMovementInputEnabled(true);
            }
        }
    }
}
