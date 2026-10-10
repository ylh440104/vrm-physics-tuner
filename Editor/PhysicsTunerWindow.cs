using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VrmPhysicsTuner.Editor
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

        private readonly AssistantEndpoint _endpoint = new AssistantEndpoint();
        private string _intent = "";
        private SuggestionBatch _batch;
        private HttpPhysicsAssistant _assistant;
        private string _assistantStatus = "";
        private Vector2 _assistantScroll;
        private readonly HashSet<int> _accepted = new HashSet<int>();

        private readonly PreviewOverlay _preview = new PreviewOverlay();
        private bool _previewing;
        private double _lastTick;

        [MenuItem("Tools/VRM Physics Tuner")]
        public static void Open()
        {
            var window = GetWindow<PhysicsTunerWindow>();
            window.titleContent = new GUIContent("VRM Physics Tuner");
            window.minSize = new Vector2(460f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _preview.Detach();
            SceneView.duringSceneGui -= OnSceneGui;
            if (_assistant != null) _assistant.Cancel();
        }

        private void OnEditorUpdate()
        {
            PollAssistant();

            if (!_previewing || !_preview.IsLoaded) return;

            var now = EditorApplication.timeSinceStartup;
            if (_lastTick <= 0d)
            {
                _lastTick = now;
                return;
            }

            var delta = (float)(now - _lastTick);
            _lastTick = now;
            _preview.Advance(delta);
            SceneView.RepaintAll();
        }

        private void OnSceneGui(SceneView view)
        {
            if (!_previewing) return;
            _preview.Draw();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VRM Physics Tuner", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _target = (GameObject)EditorGUILayout.ObjectField("Avatar Root", _target, typeof(GameObject), true);
            _padding = EditorGUILayout.Slider("Collider Padding", _padding, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan")) Scan();
            if (GUILayout.Button("Build Config")) BuildConfig();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawPreviewControls();
            EditorGUILayout.Space();
            DrawSummary();
            DrawAssistant();
            DrawExport();
        }

        private void Scan()
        {
            _chains.Clear();
            _colliderBones.Clear();
            _config = null;
            _batch = null;
            _accepted.Clear();
            StopPreview();

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

            if (_previewing) StartPreview();
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.BeginHorizontal();

            if (!_previewing)
            {
                if (GUILayout.Button("Start Preview"))
                {
                    StartPreview();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Preview"))
                {
                    StopPreview();
                }
                if (GUILayout.Button("Restart"))
                {
                    _preview.Restart();
                    _lastTick = 0d;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (_previewing && _preview.IsLoaded)
            {
                EditorGUILayout.LabelField("Joints: " + _preview.JointCount
                    + "   max drift: " + _preview.MaxDisplacement.ToString("0.####") + " m");
            }
        }

        private void StartPreview()
        {
            if (_target == null)
            {
                _status = "Select an avatar root first.";
                return;
            }

            if (_config == null) BuildConfig();
            if (_config == null) return;

            _preview.Attach(_target, _config);
            _previewing = true;
            _lastTick = 0d;
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
            SceneView.RepaintAll();
        }

        private void StopPreview()
        {
            _previewing = false;
            _preview.Detach();
            _lastTick = 0d;
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.RepaintAll();
        }

        private void DrawSummary()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(180f));

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

        private void DrawAssistant()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Assistant", EditorStyles.boldLabel);

            _endpoint.url = EditorGUILayout.TextField("Endpoint", _endpoint.url);
            _endpoint.model = EditorGUILayout.TextField("Model", _endpoint.model);
            _endpoint.apiKey = EditorGUILayout.PasswordField("API Key", _endpoint.apiKey);
            _intent = EditorGUILayout.TextField("Intent", _intent);

            EditorGUILayout.BeginHorizontal();
            var pending = _assistant != null && _assistant.IsPending;
            EditorGUI.BeginDisabledGroup(pending);
            if (GUILayout.Button(pending ? "Waiting..." : "Ask for Suggestions"))
            {
                RequestSuggestions();
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Apply Accepted"))
            {
                ApplyAccepted();
            }
            if (GUILayout.Button("Clear"))
            {
                _batch = null;
                _accepted.Clear();
                _assistantStatus = "";
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_assistantStatus))
            {
                EditorGUILayout.HelpBox(_assistantStatus, MessageType.Info);
            }

            if (_batch == null || _batch.suggestions.Count == 0) return;

            if (!string.IsNullOrEmpty(_batch.summary))
            {
                EditorGUILayout.LabelField(_batch.summary, EditorStyles.wordWrappedLabel);
            }

            _assistantScroll = EditorGUILayout.BeginScrollView(_assistantScroll, GUILayout.Height(140f));
            for (int i = 0; i < _batch.suggestions.Count; i++)
            {
                var s = _batch.suggestions[i];
                EditorGUILayout.BeginHorizontal();

                bool accepted = _accepted.Contains(i);
                bool now = EditorGUILayout.Toggle(accepted, GUILayout.Width(20f));
                if (now != accepted)
                {
                    if (now) _accepted.Add(i);
                    else _accepted.Remove(i);
                }

                string target = string.IsNullOrEmpty(s.bone) ? s.chain : s.chain + "/" + s.bone;
                EditorGUILayout.LabelField(target + "  " + s.field + " -> " + s.value.ToString("0.####"));
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(s.reason))
                {
                    EditorGUILayout.LabelField("    " + s.reason, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RequestSuggestions()
        {
            if (_config == null) BuildConfig();
            if (_config == null)
            {
                _assistantStatus = "Build a config first.";
                return;
            }

            if (!_endpoint.IsUsable())
            {
                _assistantStatus = "Set endpoint and model.";
                return;
            }

            if (_assistant == null) _assistant = new HttpPhysicsAssistant(_endpoint);

            if (!_assistant.Begin(_config, _intent))
            {
                _assistantStatus = "Could not start the request.";
                return;
            }

            _accepted.Clear();
            _assistantStatus = "Waiting for suggestions...";
        }

        private void PollAssistant()
        {
            if (_assistant == null || !_assistant.IsPending) return;

            var batch = _assistant.Poll();
            if (batch == null)
            {
                Repaint();
                return;
            }

            _batch = batch;
            _accepted.Clear();

            if (!batch.ok)
            {
                _assistantStatus = "Failed: " + batch.error;
            }
            else
            {
                _assistantStatus = "Got " + batch.suggestions.Count + " suggestions.";
            }

            Repaint();
        }

        private void ApplyAccepted()
        {
            if (_batch == null || _config == null) return;

            var chosen = new SuggestionBatch();
            chosen.ok = true;
            for (int i = 0; i < _batch.suggestions.Count; i++)
            {
                if (_accepted.Contains(i)) chosen.suggestions.Add(_batch.suggestions[i]);
            }

            int applied = SuggestionApplier.Apply(_config, chosen, false);
            _assistantStatus = "Applied " + applied + " of " + chosen.suggestions.Count + ".";

            if (_previewing) StartPreview();
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