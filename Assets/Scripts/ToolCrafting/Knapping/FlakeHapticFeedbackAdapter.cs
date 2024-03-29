﻿using NeanderthalTools.Haptics;
using UnityEngine;

namespace NeanderthalTools.ToolCrafting.Knapping
{
    public class FlakeHapticFeedbackAdapter : MonoBehaviour
    {
        #region Enums

        private enum Target
        {
            Knapper,
            Objective,
            Both
        }

        #endregion

        #region Editor

        [SerializeField]
        private HapticFeedbackSettings settings;

        [SerializeField]
        private Target target = Target.Knapper;

        #endregion

        #region Methods

        public void SendHapticImpulse(FlakeEventArgs args)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            switch (target)
            {
                case Target.Knapper:
                    settings.SendHapticImpulse(args.KnapperInteractor);
                    break;
                case Target.Objective:
                    settings.SendHapticImpulse(args.ObjectiveInteractor);
                    break;
                case Target.Both:
                    settings.SendHapticImpulse(args.KnapperInteractor, args.ObjectiveInteractor);
                    break;
            }
        }

        #endregion
    }
}
