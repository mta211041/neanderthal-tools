﻿using System.Collections.Generic;
using System.Linq;
using NeanderthalTools.ToolCrafting.Knapping;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace NeanderthalTools.ToolCrafting.Hafting
{
    [RequireComponent(typeof(Collider))]
    public class AttachPoint : MonoBehaviour
    {
        #region Enums

        private enum Target
        {
            Adhesive,
            Flake
        }

        #endregion

        #region Editor

        [SerializeField]
        [Tooltip("Other parts that need attached")]
        private List<AttachPoint> dependencies;

        [SerializeField]
        [Tooltip("Where to attach the object on this attacher")]
        private Transform attachTransform;

        [SerializeField]
        private Target target;

        #endregion

        #region Fields

        private readonly List<AttachPoint> dependants = new List<AttachPoint>();
        private Collider attachPointCollider;
        private Handle handle;

        #endregion

        #region Unity Lifecycle

        private void OnDrawGizmos()
        {
            if (attachTransform == null)
            {
                return;
            }

            var position = attachTransform.position;
            var direction = attachTransform.rotation * Vector3.forward / 2;

            Gizmos.DrawWireSphere(position, 0.1f);
            Gizmos.DrawRay(position, direction);
        }

        private void Awake()
        {
            attachPointCollider = GetComponent<Collider>();
            handle = GetComponentInParent<Handle>();
            SetupAttachTransform();
            SetupDependencies();
        }

        private void OnDisable()
        {
            ClearDependencies();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsDependencies() || IsAttached())
            {
                return;
            }

            var otherGameObject = collision.gameObject;
            switch (target)
            {
                case Target.Adhesive:
                    HandleAttachAdhesive(otherGameObject);
                    break;
                case Target.Flake:
                    HandleAttachFlake(otherGameObject);
                    break;
                default:
                    Debug.LogError($"Unsupported target: ${target}");
                    break;
            }

            if (IsAttached())
            {
                Destroy(attachPointCollider);
                Destroy(this);
            }
        }

        #endregion

        #region Methods

        private void SetupAttachTransform()
        {
            if (attachTransform == null)
            {
                attachTransform = GetAttachTransform();
            }
        }

        private Transform GetAttachTransform()
        {
            return GetComponentsInChildren<Transform>()
                .Where(IsSingleTransform)
                .FirstOrDefault() ?? transform;
        }

        private static bool IsSingleTransform(Transform target)
        {
            return target.GetComponents<Component>().Length == 1;
        }

        private void SetupDependencies()
        {
            foreach (var dependency in dependencies)
            {
                dependency.dependants.Add(this);
            }
        }

        private void HandleAttachAdhesive(GameObject otherGameObject)
        {
            if (!otherGameObject.TryGetComponent<Adhesive>(out var adhesive))
            {
                return;
            }

            handle.HandleAttachAdhesive(adhesive);
            Attach(adhesive);
        }

        private void HandleAttachFlake(GameObject otherGameObject)
        {
            if (!otherGameObject.TryGetComponent<Flake>(out var flake) || !flake.IsAttachable)
            {
                return;
            }

            handle.HandleAttachFlake(flake);
            Attach(flake);
        }

        private bool IsDependencies()
        {
            return dependencies.Count > 0;
        }

        private bool IsAttached()
        {
            return attachTransform.childCount > 0;
        }

        private void Attach(Component part)
        {
            part.transform.parent = attachTransform;
            if (part.TryGetComponent<XRBaseInteractable>(out var interactable))
            {
                Destroy(interactable);
            }

            if (part.TryGetComponent<Rigidbody>(out var rb))
            {
                Destroy(rb);
            }

            ClearDependencies();
        }

        private void ClearDependencies()
        {
            foreach (var dependant in dependants)
            {
                dependant.dependencies.Remove(this);
            }

            dependencies.Clear();
            dependants.Clear();
        }

        #endregion
    }
}