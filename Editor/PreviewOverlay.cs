using System.Collections.Generic;
using UnityEngine;

namespace VrmPhysicsTuner.Editor
{
    public class PreviewOverlay
    {
        private readonly PreviewSimulator _simulator = new PreviewSimulator();
        private GameObject _target;
        private VrmPhysicsConfig _config;
        private float _elapsed;
        private bool _loaded;

        public bool IsLoaded { get { return _loaded; } }
        public int JointCount { get { return _simulator.JointCount(); } }
        public float MaxDisplacement { get { return _simulator.MaxDisplacement(); } }

        public void Attach(GameObject target, VrmPhysicsConfig config)
        {
            _target = target;
            _config = config;
            _elapsed = 0f;
            _loaded = false;

            if (target == null || config == null)
            {
                _simulator.Load(null, null);
                return;
            }

            var index = new Dictionary<string, Vector3>();
            var all = target.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (!index.ContainsKey(all[i].name)) index[all[i].name] = all[i].position;
            }

            _simulator.Load(config, name =>
            {
                Vector3 p;
                if (index.TryGetValue(name, out p)) return new Vec3(p.x, p.y, p.z);
                return new Vec3(0f, 0f, 0f);
            });

            _simulator.Reset();
            _loaded = _simulator.JointCount() > 0;
        }

        public void Detach()
        {
            _target = null;
            _config = null;
            _loaded = false;
            _elapsed = 0f;
            _simulator.Load(null, null);
        }

        public void Advance(float delta)
        {
            if (!_loaded) return;
            _elapsed += delta;
            _simulator.Step(delta, _elapsed);
        }

        public void Restart()
        {
            _elapsed = 0f;
            _simulator.Reset();
        }

        public void Draw()
        {
            if (!_loaded || _target == null) return;

            var chains = _simulator.Chains;
            for (int c = 0; c < chains.Count; c++)
            {
                var chain = chains[c];
                var points = new Vector3[chain.joints.Count];
                for (int j = 0; j < chain.joints.Count; j++)
                {
                    var p = chain.joints[j].position;
                    points[j] = new Vector3(p.x, p.y, p.z);
                }

                Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
                for (int j = 0; j + 1 < points.Length; j++)
                {
                    Gizmos.DrawLine(points[j], points[j + 1]);
                }

                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
                for (int j = 0; j < chain.joints.Count; j++)
                {
                    float radius = chain.joints[j].hitRadius;
                    if (radius > 0f) Gizmos.DrawWireSphere(points[j], radius);
                }
            }
        }
    }
}