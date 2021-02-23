﻿using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Based on: https://www.youtube.com/watch?v=-a36GpPkW-Q
namespace NeanderthalTools.Hands
{
    public class OffsetGrabInteractable : XRGrabInteractable
    {
        #region Fields

        private Vector3 interactorPosition = Vector3.zero;
        private Quaternion interactorRotation = Quaternion.identity;
        private GrabWelder grabWelder;

        #endregion

        #region Overrides

        protected override void Awake()
        {
            base.Awake();
            grabWelder = GetComponent<GrabWelder>();
        }

        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            var interactor = args.interactor;
            SetInteractorPose(interactor);
            SetAttachmentPose(interactor);
            SetIgnoreCollision(interactor, true);

            if (grabWelder != null)
            {
                grabWelder.Weld(interactor);
            }
        }

        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);

            var interactor = args.interactor;
            ResetAttachmentPose(interactor);
            ClearInteractorPose();
            SetIgnoreCollision(interactor, false);

            if (grabWelder != null)
            {
                grabWelder.UnWeld(interactor);
            }
        }

        #endregion

        #region Methods

        private void SetInteractorPose(XRBaseInteractor interactor)
        {
            var interactorTransform = interactor.attachTransform;
            interactorPosition = interactorTransform.localPosition;
            interactorRotation = interactorTransform.localRotation;
        }

        private void SetAttachmentPose(XRBaseInteractor interactor)
        {
            var interactorAttachTransform = interactor.attachTransform;
            if (attachTransform != null)
            {
                interactorAttachTransform.position = attachTransform.position;
                interactorAttachTransform.rotation = attachTransform.rotation;
            }
            else
            {
                var interactableTransform = transform;

                interactorAttachTransform.position = interactableTransform.position;
                interactorAttachTransform.rotation = interactableTransform.rotation;
            }
        }

        private void ResetAttachmentPose(XRBaseInteractor interactor)
        {
            var interactorAttachTransform = interactor.attachTransform;
            interactorAttachTransform.localPosition = interactorPosition;
            interactorAttachTransform.localRotation = interactorRotation;
        }

        private void ClearInteractorPose()
        {
            interactorPosition = Vector3.zero;
            interactorRotation = Quaternion.identity;
        }

        private void SetIgnoreCollision(Component interactor, bool ignore)
        {
            var physicsPoser = interactor.GetComponent<PhysicsPoser>();
            if (physicsPoser == null)
            {
                return;
            }

            foreach (var physicsPoserCollider in physicsPoser.Colliders)
            {
                foreach (var interactableCollider in colliders)
                {
                    Physics.IgnoreCollision(
                        physicsPoserCollider,
                        interactableCollider,
                        ignore
                    );
                }
            }
        }

        #endregion
    }
}
