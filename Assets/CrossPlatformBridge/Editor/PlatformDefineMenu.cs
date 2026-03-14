#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrossPlatformBridge.Editor
{
    internal sealed class PlatformDefineWindow : EditorWindow
    {
        private const string MenuPath = "Tools/CrossPlatformBridge/Platform Defines";
        private const string SymbolPrefix = "USE_CROSSPLATFORMBRIDGE_";

        private static string GetPlatformRootPath([System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "")
        {
            var editorDir = Path.GetDirectoryName(sourceFilePath);
            var bridgeDir = Path.GetDirectoryName(editorDir);
            return Path.Combine(bridgeDir, "Platform");
        }

        private Vector2 _scroll;
        private List<PlatformEntry> _entries = new List<PlatformEntry>();
        private BuildTargetGroup _cachedGroup;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<PlatformDefineWindow>(true, "CrossPlatformBridge Platform Defines");
            window.minSize = new Vector2(420f, 320f);
            window.RefreshEntries(true);
            window.Show();
        }

        private void OnFocus()
        {
            RefreshEntries(false);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Build Target", EditorStyles.boldLabel);
            var currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            EditorGUILayout.LabelField(currentGroup.ToString());

            if (_cachedGroup != currentGroup)
            {
                RefreshEntries(true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);

            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No platforms found under Platform folder.", MessageType.Info);
                if (GUILayout.Button("Rescan"))
                {
                    RefreshEntries(true);
                }
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var entry in _entries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(entry.DisplayName, GUILayout.Width(160f));
                    bool enabled = entry.Enabled;
                    bool newEnabled = EditorGUILayout.ToggleLeft("Enabled", enabled, GUILayout.Width(80f));
                    if (newEnabled != enabled)
                    {
                        entry.Enabled = newEnabled;
                        entry.Dirty = true;
                    }

                    EditorGUILayout.LabelField(entry.Symbol, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rescan"))
                {
                    RefreshEntries(true);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply"))
                {
                    ApplyChanges();
                }
            }
        }

        private void RefreshEntries(bool fullRescan)
        {
            _cachedGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (fullRescan)
            {
                _entries = ScanPlatforms()
                    .Select(name => new PlatformEntry(name, GetSymbol(name)))
                    .OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var symbols = GetSymbolsForGroup(_cachedGroup);
            foreach (var entry in _entries)
            {
                entry.Enabled = symbols.Contains(entry.Symbol);
                entry.Dirty = false;
            }

            Repaint();
        }

        private static List<string> ScanPlatforms()
        {
            var path = GetPlatformRootPath();
            if (!Directory.Exists(path))
            {
                return new List<string>();
            }

            var names = new List<string>();
            foreach (var dir in Directory.GetDirectories(path))
            {
                var name = Path.GetFileName(dir);
                if (string.Equals(name, "Dummy", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                if (Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Length == 0)
                {
                    continue;
                }

                names.Add(name);
            }

            return names;
        }

        private static string GetSymbol(string platformName)
        {
            var upper = platformName.Replace(" ", "").Replace("-", "").Replace("/", "").ToUpperInvariant();
            return SymbolPrefix + upper;
        }

        private static HashSet<string> GetSymbolsForGroup(BuildTargetGroup group)
        {
            var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            return new HashSet<string>(
                current.Split(';').Where(s => !string.IsNullOrWhiteSpace(s))
            );
        }

        private static void SetSymbolsForGroup(BuildTargetGroup group, IEnumerable<string> symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols));
        }

        private void ApplyChanges()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var symbols = GetSymbolsForGroup(group);

            foreach (var entry in _entries)
            {
                if (!entry.Dirty)
                {
                    continue;
                }

                if (entry.Enabled)
                {
                    symbols.Add(entry.Symbol);
                }
                else
                {
                    symbols.Remove(entry.Symbol);
                }

                entry.Dirty = false;
            }

            SetSymbolsForGroup(group, symbols);
            RefreshEntries(false);
        }

        private sealed class PlatformEntry
        {
            public PlatformEntry(string displayName, string symbol)
            {
                DisplayName = displayName;
                Symbol = symbol;
            }

            public string DisplayName { get; }
            public string Symbol { get; }
            public bool Enabled { get; set; }
            public bool Dirty { get; set; }
        }
    }
}
#endif
