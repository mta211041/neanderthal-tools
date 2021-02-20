﻿using NeanderthalTools.Util;
using ScriptableEvents.Simple;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeanderthalTools.Logging
{
    public class LoggingHandler : MonoBehaviour
    {
        #region Editor

        [SerializeField]
        private LoggingSettings loggingSettings;

        [SerializeField]
        private SimpleScriptableEvent startLoggingEvent;

        [SerializeField]
        private SimpleScriptableEvent stopLoggingEvent;

        #endregion

        #region Fields

        private Scene currentLoggingScene;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            if (currentLoggingScene.IsValid())
            {
                StopLogging();
            }
        }

        #endregion

        #region Methods

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (!loggingSettings.EnableLogging)
            {
                return;
            }

            if (currentLoggingScene.IsValid() && currentLoggingScene == oldScene)
            {
                StopLogging();
            }

            if (IsLoggingScene(newScene))
            {
                currentLoggingScene = newScene;
                StartLogging();
            }
        }

        private void StartLogging()
        {
            loggingSettings.LogFileDirectory = $"{Files.DateName()}_{currentLoggingScene.name}";
            startLoggingEvent.Raise();
        }

        private void StopLogging()
        {
            stopLoggingEvent.Raise();
            if (loggingSettings.UploadLogsToDropbox)
            {
                UploadLogs();
            }
        }

        private bool IsLoggingScene(Scene scene)
        {
            return loggingSettings.LoggingSceneIndexes.Contains(scene.buildIndex);
        }

        private void UploadLogs()
        {
            this.UploadDirectory(
                loggingSettings.LogFileDirectory,
                $"{loggingSettings.LoggingId}/{loggingSettings.CurrentLogFileDirectory}",
                loggingSettings.DropboxAuthorizationToken
            );
        }

        #endregion
    }
}