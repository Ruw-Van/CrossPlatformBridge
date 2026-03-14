#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrossPlatformBridge.Editor
{
    internal sealed class IntegrationTestSettingsWindow : EditorWindow
    {
        private const string MenuPath = "Tools/CrossPlatformBridge/Create Integration Test Settings";
        private const string TargetFolder = "Assets/CrossPlatformBridgeSettings/Editor";

        private Vector2 _scroll;
        private List<SettingsEntry> _entries = new List<SettingsEntry>();

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<IntegrationTestSettingsWindow>(true, "Integration Test Settings");
            window.minSize = new Vector2(420f, 300f);
            window.Refresh();
            window.Show();
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Integration Test Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("チェックを付けて「Create Selected」で一括作成します。", MessageType.Info);
            EditorGUILayout.Space();

            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "利用可能な IntegrationTestSettings が見つかりません。\n" +
                    "Platform Defines でプラットフォームを有効にしてください。",
                    MessageType.Warning);
                if (GUILayout.Button("Refresh"))
                {
                    Refresh();
                }
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var entry in _entries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(entry.Exists))
                    {
                        entry.Selected = EditorGUILayout.ToggleLeft(
                            entry.DisplayName,
                            entry.Exists || entry.Selected,
                            GUILayout.Width(180f));
                    }

                    if (entry.Exists)
                    {
                        EditorGUILayout.LabelField("✓ Exists", EditorStyles.miniLabel, GUILayout.Width(60f));
                        if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                        {
                            EditorUtility.FocusProjectWindow();
                            Selection.activeObject = entry.ExistingAsset;
                            EditorGUIUtility.PingObject(entry.ExistingAsset);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("+ New", EditorStyles.miniLabel, GUILayout.Width(60f));
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                {
                    Refresh();
                }

                GUILayout.FlexibleSpace();

                var toCreate = _entries.Where(e => e.Selected && !e.Exists).ToList();
                using (new EditorGUI.DisabledScope(toCreate.Count == 0))
                {
                    if (GUILayout.Button($"Create Selected ({toCreate.Count})"))
                    {
                        CreateSelected(toCreate);
                        Refresh();
                    }
                }
            }
        }

        private void Refresh()
        {
            EnsureFolders();

            _entries = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => !t.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(t))
                .Where(t => t.Name.EndsWith("IntegrationTestSettings"))
                .OrderBy(t => t.Name)
                .Select(t => new SettingsEntry(t))
                .ToList();

            Repaint();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CrossPlatformBridgeSettings"))
                AssetDatabase.CreateFolder("Assets", "CrossPlatformBridgeSettings");
            if (!AssetDatabase.IsValidFolder("Assets/CrossPlatformBridgeSettings/Editor"))
                AssetDatabase.CreateFolder("Assets/CrossPlatformBridgeSettings", "Editor");
        }

        private static void CreateSelected(IReadOnlyList<SettingsEntry> entries)
        {
            EnsureFolders();
            foreach (var entry in entries)
            {
                var instance = ScriptableObject.CreateInstance(entry.SettingsType);
                AssetDatabase.CreateAsset(instance, entry.AssetPath);
                Debug.Log($"[CrossPlatformBridge] Created {entry.AssetPath}");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private sealed class SettingsEntry
        {
            public Type SettingsType { get; }
            public string DisplayName { get; }
            public string AssetPath { get; }
            public bool Selected { get; set; }
            public ScriptableObject ExistingAsset { get; }
            public bool Exists => ExistingAsset != null;

            public SettingsEntry(Type type)
            {
                SettingsType = type;
                DisplayName = type.Name.Replace("IntegrationTestSettings", "");
                AssetPath = $"{TargetFolder}/{type.Name}.asset";
                ExistingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetPath);
                Selected = !Exists;
            }
        }
    }
}
#endif
