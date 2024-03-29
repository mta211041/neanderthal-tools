﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeanderthalTools.Util.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeanderthalTools.Logging.Visualizers.Editor
{
    public class LogVisualizer : EditorWindow
    {
        #region Fields

        private static readonly MethodInfo ApplyWireMaterialMethod = typeof(HandleUtility)
            .GetMethod(
                "ApplyWireMaterial",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] {typeof(CompareFunction)},
                null
            );

        private readonly List<UserData> users = new List<UserData>();

        private readonly Dictionary<Vector3, int> heatmap =
            new SerializedDictionary<Vector3, int>();

        private LogVisualizerSettings settings;

        private Vector2 usersScroll;

        private int maxHeatmapValue;
        private int maxPositions;

        private int seekStart;
        private int seekSize;

        #endregion

        #region Unity Lifecycle

        [MenuItem("Tools/Log visualizer")]
        public static void Init()
        {
            var window = GetWindow<LogVisualizer>("Log visualizer");
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
            LoadHeatmap();
        }

        private void OnFocus()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Users", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            usersScroll = EditorGUILayout.BeginScrollView(
                usersScroll,
                GUIStyle.none,
                GUI.skin.verticalScrollbar
            );

            DrawUsersGUI();
            EditorGUILayout.EndScrollView();

            DrawPositionsGUI();
            EditorGUILayout.Space();

            DrawHeatmapGUI();
            EditorGUILayout.Space();

            if (GUILayout.Button("Save user sessions"))
            {
                SaveUserSessions();
            }

            if (GUILayout.Button("Reload heatmap"))
            {
                LoadHeatmap();
            }

            if (GUILayout.Button("Add user sessions"))
            {
                AddSessions();
                LoadHeatmap();
            }
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (settings.IsDrawUserPositions)
            {
                DrawUserPositions();
            }

            if (settings.IsDrawHeatmap)
            {
                DrawHeatmap();
            }
        }

        #endregion

        #region GUI drawing methods

        private void DrawUsersGUI()
        {
            for (var userIndex = users.Count - 1; userIndex >= 0; userIndex--)
            {
                var user = users[userIndex];
                user.Foldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                    user.Foldout,
                    user.LoggingId
                );

                if (user.Foldout)
                {
                    DrawUserGUI(user);
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawUserGUI(UserData user)
        {
            EditorGUI.indentLevel++;

            var sessions = user.Sessions;
            for (var sessionIndex = sessions.Count - 1; sessionIndex >= 0; sessionIndex--)
            {
                var session = sessions[sessionIndex];

                DrawSessionGUI(sessions, session);
                EditorGUILayout.Space();
            }

            EditorGUI.indentLevel--;
            if (GUILayout.Button("Remove user"))
            {
                users.Remove(user);
            }

            EditorGUILayout.Space();
        }

        private static void DrawSessionGUI(
            ICollection<AggregatedSessionData> sessions,
            AggregatedSessionData session
        )
        {
            DrawSessionHeaderGUI(sessions, session);
            EditorGUI.indentLevel++;
            DrawSessionPoseGUI(session);
            EditorGUI.indentLevel--;

            session.Color = EditorGUILayout.ColorField("Color", session.Color);
        }

        private static void DrawSessionHeaderGUI(
            ICollection<AggregatedSessionData> sessions,
            AggregatedSessionData session
        )
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(session.SessionName);
            if (GUILayout.Button("Remove session"))
            {
                sessions.Remove(session);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSessionPoseGUI(AggregatedSessionData session)
        {
            foreach (var pose in session.Poses)
            {
                EditorGUILayout.LabelField(pose.Name);
                EditorGUI.indentLevel++;
                pose.IsHeatmap = EditorGUILayout.Toggle("Include in heatmap", pose.IsHeatmap);
                pose.IsDraw = EditorGUILayout.Toggle("Draw positions", pose.IsDraw);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPositionsGUI()
        {
            EditorGUILayout.LabelField("Positions", EditorStyles.boldLabel);
            settings.IsDrawUserPositions = EditorGUILayout
                .Toggle("Draw", settings.IsDrawUserPositions);

            seekStart = EditorGUILayout.IntSlider("Seek start", seekStart, 0, maxPositions);
            seekSize = EditorGUILayout.IntSlider("Seek size", seekSize, 0, maxPositions);
        }

        private void DrawHeatmapGUI()
        {
            EditorGUILayout.LabelField("Heatmap", EditorStyles.boldLabel);

            // IsDrawHeatmap
            settings.IsDrawHeatmap = EditorGUILayout.Toggle(
                "Draw",
                settings.IsDrawHeatmap
            );

            // HeatmapCellMaxScale
            settings.HeatmapCellMaxScale = EditorGUILayout.FloatField(
                "Cell max scale",
                settings.HeatmapCellMaxScale
            );
            settings.HeatmapCellMaxScale = Math.Max(settings.HeatmapCellMaxScale, 0);

            // HeatmapCellSize
            settings.HeatmapCellSize = EditorGUILayout.FloatField(
                "Cell size",
                settings.HeatmapCellSize
            );
            settings.HeatmapCellSize = Math.Max(settings.HeatmapCellSize, 0);

            // HeatmapMaterial
            settings.HeatmapMaterial = (Material) EditorGUILayout.ObjectField(
                "Material",
                settings.HeatmapMaterial,
                typeof(Material),
                false
            );

            // HeatmapMesh
            settings.HeatmapMesh = (Mesh) EditorGUILayout.ObjectField(
                "Mesh",
                settings.HeatmapMesh,
                typeof(Mesh),
                false
            );

            // HeatmapColorProperty
            settings.HeatmapColorProperty = EditorGUILayout.TextField(
                "Color property",
                settings.HeatmapColorProperty
            );

            // HeatmapToColor
            settings.HeatmapFromColor = EditorGUILayout.ColorField(
                "From color",
                settings.HeatmapFromColor
            );

            // HeatmapToColor
            settings.HeatmapToColor = EditorGUILayout.ColorField(
                "To color",
                settings.HeatmapToColor
            );
        }

        #endregion

        #region User utility methods

        private void LoadSettings()
        {
            settings = ScriptableObjectExtensions.FindOrCreateAsset<LogVisualizerSettings>(
                "Assets/Settings/Logging/LogVisualizerSettings.asset"
            );

            users.AddRange(settings.Users);
            maxPositions = FindMaxPositions();
            seekSize = maxPositions;
        }

        private void SaveUserSessions()
        {
            settings.Users.Clear();
            settings.Users.AddRange(users);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void AddSessions()
        {
            var directoryPath = EditorUtility.OpenFolderPanel("Open sessions directory", "", "");
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            var sessions = AggregatedSessionDataReader.Read(directoryPath);
            foreach (var session in sessions)
            {
                AddOrReplace(session);
            }

            maxPositions = FindMaxPositions();
        }

        private int FindMaxPositions()
        {
            var max = 0;
            foreach (var user in users)
            {
                foreach (var session in user.Sessions)
                {
                    foreach (var pose in session.Poses)
                    {
                        var positionCount = pose.Positions.Count;
                        if (max < positionCount)
                        {
                            max = positionCount;
                        }
                    }
                }
            }

            return max;
        }

        private void AddOrReplace(AggregatedSessionData session)
        {
            var existingUser = users.FirstOrDefault(user => user.LoggingId == session.LoggingId);
            if (existingUser != null)
            {
                var sessions = existingUser.Sessions;
                var index = sessions.FindIndex(
                    existingSession => existingSession.SessionName == session.SessionName
                );

                if (index != -1)
                {
                    sessions[index] = session;
                }
                else
                {
                    sessions.Add(session);
                }

                sessions.Sort((sessionA, sessionB) =>
                    string.CompareOrdinal(sessionA.SessionName, sessionB.SessionName)
                );
            }
            else
            {
                var newUser = new UserData(session);
                users.Add(newUser);
            }
        }

        #endregion

        #region Heatmap utility methods

        private void LoadHeatmap()
        {
            heatmap.Clear();

            foreach (var user in users)
            {
                foreach (var session in user.Sessions)
                {
                    foreach (var pose in session.Poses)
                    {
                        if (!pose.IsHeatmap)
                        {
                            continue;
                        }

                        foreach (var posePosition in pose.Positions)
                        {
                            AddToHeatmap(posePosition);
                        }
                    }
                }
            }

            maxHeatmapValue = FindMaxHeatmapValue();
        }

        private int FindMaxHeatmapValue()
        {
            var max = 0;
            foreach (var value in heatmap.Values)
            {
                if (max < value)
                {
                    max = value;
                }
            }

            return max;
        }

        private bool IsLoadHeatmapRenderers()
        {
            return settings.HeatmapMaterial == null || settings.HeatmapMesh == null;
        }

        private void LoadHeatmapRenderers()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            var renderer = sphere.GetComponent<Renderer>();
            settings.HeatmapMaterial = renderer.sharedMaterial;

            var meshFilter = sphere.GetComponent<MeshFilter>();
            settings.HeatmapMesh = meshFilter.sharedMesh;

            DestroyImmediate(sphere);
        }

        private void AddToHeatmap(Vector3 worldPosition)
        {
            var heatmapScale = settings.HeatmapCellSize;

            var heatmapPosition = new Vector3(
                Mathf.Round(worldPosition.x / heatmapScale) * heatmapScale,
                Mathf.Round(worldPosition.y / heatmapScale) * heatmapScale,
                Mathf.Round(worldPosition.z / heatmapScale) * heatmapScale
            );

            if (heatmap.TryGetValue(heatmapPosition, out var heatmapValue))
            {
                heatmapValue += 1;
                heatmap[heatmapPosition] = heatmapValue;
            }
            else
            {
                heatmap[heatmapPosition] = 1;
            }
        }

        #endregion

        #region User session drawing methods

        private void DrawUserPositions()
        {
            foreach (var user in users)
            {
                foreach (var session in user.Sessions)
                {
                    DrawPositions(session);
                }
            }
        }

        private void DrawPositions(AggregatedSessionData session)
        {
            ApplyWireMaterial();

            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);

            var poses = session.Poses;
            foreach (var pose in poses)
            {
                if (pose.IsDraw)
                {
                    DrawPositions(pose, session.Color);
                }
            }

            GL.PopMatrix();
        }

        private void DrawPositions(PoseData pose, Color color)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(color);

            var positions = pose.Positions;

            var startIndex = Math.Min(seekStart, positions.Count);
            var endIndex = Math.Min(seekStart + seekSize, positions.Count) - 1;

            for (var index = startIndex; index < endIndex; index++)
            {
                var currentPosition = positions[index];
                var nextPosition = positions[index + 1];

                GL.Vertex(currentPosition);
                GL.Vertex(nextPosition);
            }

            GL.End();
        }

        private static void ApplyWireMaterial()
        {
            ApplyWireMaterialMethod.Invoke(null, new object[] {Handles.zTest});
        }

        #endregion

        #region Heatmap drawing methods

        private void DrawHeatmap()
        {
            if (IsLoadHeatmapRenderers())
            {
                LoadHeatmapRenderers();
            }

            var colorProperty = Shader.PropertyToID(settings.HeatmapColorProperty);
            var material = settings.HeatmapMaterial;

            foreach (var heatmapKeyValue in heatmap)
            {
                var cellPosition = heatmapKeyValue.Key;
                var cellValue = heatmapKeyValue.Value;

                var progress = cellValue / (float) maxHeatmapValue;
                var scale = Vector3.Lerp(
                    Vector3.one * settings.HeatmapCellSize,
                    Vector3.one * settings.HeatmapCellMaxScale,
                    progress
                );

                var color = Color.Lerp(
                    settings.HeatmapFromColor,
                    settings.HeatmapToColor,
                    progress
                );

                material.SetColor(colorProperty, color);
                material.SetPass(0);

                var matrix = Handles.matrix * Matrix4x4.TRS(
                    cellPosition,
                    Quaternion.identity,
                    scale
                );

                Graphics.DrawMeshNow(settings.HeatmapMesh, matrix);
            }
        }

        #endregion
    }
}
