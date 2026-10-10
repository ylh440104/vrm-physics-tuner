using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Neko.VrmPhysicsTuner.Editor
{
    public class PhysicsTunerWindow : EditorWindow
    {
        private GameObject _target;
        private float _padding = 0.1f;
        private string _exportPath = "";
        private List<DetectedChain> _chains = new List<DetectedChain>();
        private List<Transform> _colliderBones = new List<Transform>();
        private VrmPhysicsConfig _config;
        private string _status = "";
        private Vector2 _scroll;

        [MenuItem("N.E.K.O/VRM Physics Tuner")]
        public static void Open()
        {
            var window = GetWindow<PhysicsTunerWindow>();
            window.titleContent = new GUIContent("VRM Physics Tuner");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VRM Physics Tuner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _target = (GameObject)EditorGUILayout.ObjectField("Avatar Root", _target, typeof(GameObject), true);
            _padding = EditorGUILayout.Slider("Collider Padding", _padding, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan"))
            {
                Scan();
            }
            if (GUILayout.Button("Build Config"))
            {
                BuildConfig();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawSummary();
            DrawExport();
        }

        private void Scan()
        {
            _chains.Clear();
            _colliderBones.Clear();
            _config = null;

            if (_target == null)
            {
                _status = "Select an avatar root first.";
                return;
            }

            _chains = AvatarScanner.Scan(_target);
            _colliderBones = AvatarScanner.CollectColliderCandidates(_target);
            _status = "Found " + _chains.Count + " chains, " + _colliderBones.Count + " collider bones.";
        }

        private void BuildConfig()
        {
            if (_target == null)
            {
                _status = "Select an avatar root first.";
                return;
            }

            if (_chains.Count == 0) Scan();

            _config = PhysicsConfigBuilder.Build(_target, _chains, _colliderBones, _padding);
            _status = "Built " + _config.chains.Count + " chains, " + _config.colliders.Count + " colliders.";
        }

        private void DrawSummary()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(240f));

            if (_chains.Count > 0)
            {
                EditorGUILayout.LabelField("Detected Chains", EditorStyles.boldLabel);
                for (int i = 0; i < _chains.Count; i++)
                {
                    var chain = _chains[i];
                    EditorGUILayout.LabelField("  " + chain.label + "  (" + chain.bones.Count + " bones)");
                }
            }

            if (_config != null && _config.colliders.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Auto-Fitted Colliders", EditorStyles.boldLabel);
                for (int i = 0; i < _config.colliders.Count; i++)
                {
                    var c = _config.colliders[i];
                    EditorGUILayout.LabelField("  " + c.bone + "  " + c.shape + " r=" + c.radius.ToString("0.####"));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawExport()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _exportPath = EditorGUILayout.TextField("Export Path", _exportPath);
            if (GUILayout.Button("...", GUILayout.Width(30f)))
            {
                var picked = EditorUtility.SaveFilePanel("Export Physics Config", "", _target != null ? _target.name : "model", "json");
                if (!string.IsNullOrEmpty(picked)) _exportPath = picked;
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Export JSON"))
            {
                Export();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }
        }

        private void Export()
        {
            if (_config == null)
            {
                BuildConfig();
                if (_config == null) return;
            }

            var path = _exportPath;
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(Application.dataPath, _config.model + "_physics.json");
            }

            var json = VrmPhysicsConfigSerializer.Serialize(_config);
            File.WriteAllText(path, json);
            _status = "Exported to " + path;
            AssetDatabase.Refresh();
        }
    }
}