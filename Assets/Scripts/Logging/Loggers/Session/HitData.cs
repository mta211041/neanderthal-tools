﻿using System;
using UnityEngine;

namespace NeanderthalTools.Logging.Loggers.Session
{
    [Serializable]
    public class HitData
    {
        #region Fields

        [SerializeField]
        private string objectiveName;

        [SerializeField]
        private string knapperName;

        [SerializeField]
        private string flakeName;

        #endregion

        #region Properties

        public string ObjectiveName
        {
            get => objectiveName;
            set => objectiveName = value;
        }

        public string KnapperName
        {
            get => knapperName;
            set => knapperName = value;
        }

        public string FlakeName
        {
            get => flakeName;
            set => flakeName = value;
        }

        #endregion
    }
}
