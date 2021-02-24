using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

// Based on: https://www.youtube.com/watch?v=6lK8QXL4bxc
namespace NeanderthalTools.Hands
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRBaseInteractor))]
    public class PhysicsHand : MonoBehaviour
    {
        #region Editor

        [SerializeField]
        private HandSettings settings;

        #endregion

        #region Fields

        // Nearby interactables.
        private readonly List<XRBaseInteractable> interactables = new List<XRBaseInteractable>();

        // Colliders that make up the hand.
        private List<Collider> colliders;

        private XRBaseInteractor interactor;
        private new Rigidbody rigidbody;

        private Vector3 targetPosition = Vector3.zero;
        private Quaternion targetRotation = Quaternion.identity;

        #endregion

        #region Unity Lifecycle

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, settings.PhysicsRadius);
        }

        private void Awake()
        {
            colliders = GetCollidersInChildren(gameObject);
            interactor = GetComponent<XRBaseInteractor>();
            rigidbody = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            settings.PositionAction.performed += OnPositionChanged;
            settings.RotationAction.performed += OnRotationChanged;
            settings.SelectAction.performed += OnSelectChanged;
            settings.SelectAction.canceled += OnSelectChanged;
        }

        private void OnDisable()
        {
            settings.PositionAction.performed -= OnPositionChanged;
            settings.RotationAction.performed -= OnRotationChanged;
            settings.SelectAction.performed -= OnSelectChanged;
            settings.SelectAction.canceled -= OnSelectChanged;

            interactables.Clear();
        }

        private void Start()
        {
            SetColliderLayerMasks(settings.IncorporealColliderMask);
            MoveImmediate();
            RotateImmediate();
        }

        private void FixedUpdate()
        {
            if (IsHoldingObject() || !IsColliderInRange())
            {
                MoveImmediate();
                RotateImmediate();
            }
            else
            {
                MovePhysics();
                RotatePhysics();
            }
        }

        #endregion

        #region Methods

        private void OnPositionChanged(InputAction.CallbackContext ctx)
        {
            targetPosition = ctx.ReadValue<Vector3>();
        }

        private void OnRotationChanged(InputAction.CallbackContext ctx)
        {
            targetRotation = ctx.ReadValue<Quaternion>();
        }

        private void SetColliderLayerMasks(LayerMask layerMask)
        {
            foreach (var col in colliders)
            {
                col.gameObject.layer = layerMask;
            }
        }

        private void OnSelectChanged(InputAction.CallbackContext ctx)
        {
            if (IsInteractablesInRange())
            {
                // There are interactables in range, should not collide with them even if a "fist"
                // is made.
                SetColliderLayerMasks(settings.IncorporealColliderMask);
            }
            else
            {
                // There are no interactables in range, check if the user is holding the grip
                // button and making a "fist" pose.
                var layerMask = ctx.ReadValueAsButton()
                    ? settings.RegularColliderMask
                    : settings.IncorporealColliderMask;

                SetColliderLayerMasks(layerMask);
            }
        }

        private bool IsInteractablesInRange()
        {
            interactor.GetHoverTargets(interactables);
            return interactables.Count > 0;
        }

        private bool IsHoldingObject()
        {
            return interactor.selectTarget != null;
        }

        private bool IsColliderInRange()
        {
            return Physics.CheckSphere(
                transform.position,
                settings.PhysicsRadius,
                settings.PhysicsMask,
                QueryTriggerInteraction.Ignore
            );
        }

        private void MoveImmediate()
        {
            rigidbody.velocity = Vector3.zero;
            transform.localPosition = targetPosition;
        }

        private void RotateImmediate()
        {
            rigidbody.angularVelocity = Vector3.zero;
            transform.localRotation = targetRotation;
        }

        private void MovePhysics()
        {
            rigidbody.velocity *= settings.VelocityMultiplier;

            var velocity = FindNewVelocity();
            if (IsValidVelocity(velocity))
            {
                rigidbody.velocity = Vector3.MoveTowards(
                    rigidbody.velocity,
                    velocity,
                    settings.PositionChangeSpeed * Time.deltaTime
                );
            }
        }

        private Vector3 FindNewVelocity()
        {
            var worldPosition = transform.root.TransformPoint(targetPosition);
            var positionDiff = worldPosition - rigidbody.position;

            return positionDiff / Time.deltaTime;
        }

        private void RotatePhysics()
        {
            rigidbody.angularVelocity *= settings.AngularVelocityMultiplier;

            var velocity = FindNewAngularVelocity();
            if (IsValidVelocity(velocity))
            {
                rigidbody.angularVelocity = Vector3.MoveTowards(
                    rigidbody.angularVelocity,
                    velocity,
                    settings.RotationChangeSpeed * Time.deltaTime
                );
            }
        }

        private Vector3 FindNewAngularVelocity()
        {
            var worldRotation = transform.root.rotation * targetRotation;
            var rotationDiff = worldRotation * Quaternion.Inverse(rigidbody.rotation);

            rotationDiff.ToAngleAxis(out var angle, out var axis);
            if (angle > 180)
            {
                angle -= 360;
            }

            return angle * Mathf.Deg2Rad * axis / Time.deltaTime;
        }

        private static bool IsValidVelocity(Vector3 velocity)
        {
            return IsValid(velocity.x) && IsValid(velocity.y) && IsValid(velocity.z);
        }

        private static bool IsValid(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static List<Collider> GetCollidersInChildren(GameObject obj)
        {
            return obj
                .GetComponentsInChildren<Collider>()

                // Exclude triggers as their layers should not change.
                .Where(collider => !collider.isTrigger)
                .ToList();
        }

        #endregion
    }
}