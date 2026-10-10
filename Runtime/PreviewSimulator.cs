using System;
using System.Collections.Generic;

namespace Neko.VrmPhysicsTuner
{
    public class PreviewJoint
    {
        public string bone = "";
        public Vec3 restPosition;
        public Vec3 position;
        public Vec3 previous;
        public float stiffness;
        public float dragForce;
        public float gravityPower;
        public Vec3 gravityDir;
        public float length;
        public float hitRadius;
        public Vec3 lastDir;
    }

    public class PreviewChain
    {
        public string name = "";
        public List<PreviewJoint> joints = new List<PreviewJoint>();
    }

    public class PreviewSimulator
    {
        public const float MaxStep = 0.05f;
        public const float RestEpsilon = 1e-6f;

        private readonly List<PreviewChain> _chains = new List<PreviewChain>();
        private Vec3 _rootOffset;

        public List<PreviewChain> Chains { get { return _chains; } }

        public void Load(VrmPhysicsConfig config, Func<string, Vec3> positionLookup)
        {
            _chains.Clear();
            if (config == null || config.chains == null || positionLookup == null) return;

            for (int i = 0; i < config.chains.Count; i++)
            {
                var source = config.chains[i];
                if (source.bones == null || source.bones.Count == 0) continue;

                var chain = new PreviewChain();
                chain.name = source.name;

                for (int b = 0; b < source.bones.Count; b++)
                {
                    var boneName = source.bones[b];
                    var rest = positionLookup(boneName);

                    var joint = new PreviewJoint();
                    joint.bone = boneName;
                    joint.restPosition = rest;
                    joint.position = rest;
                    joint.previous = rest;
                    joint.stiffness = source.stiffness;
                    joint.dragForce = source.drag_force;
                    joint.gravityPower = source.gravity_power;
                    joint.gravityDir = Vec3.FromArray(source.gravity_dir, new Vec3(0f, -1f, 0f)).Normalized();
                    joint.hitRadius = source.hit_radius;

                    if (b > 0)
                    {
                        var parentRest = chain.joints[b - 1].restPosition;
                        joint.length = Vec3.Distance(rest, parentRest);
                        joint.lastDir = (rest - parentRest).Normalized();
                    }
                    else
                    {
                        joint.lastDir = new Vec3(0f, -1f, 0f);
                    }

                    chain.joints.Add(joint);
                }

                if (chain.joints.Count > 0) _chains.Add(chain);
            }
        }

        public void Reset()
        {
            for (int c = 0; c < _chains.Count; c++)
            {
                var chain = _chains[c];
                for (int j = 0; j < chain.joints.Count; j++)
                {
                    var joint = chain.joints[j];
                    joint.position = joint.restPosition + _rootOffset;
                    joint.previous = joint.position;
                    if (j > 0)
                    {
                        joint.lastDir = (joint.restPosition - chain.joints[j - 1].restPosition).Normalized();
                    }
                }
            }
        }

        public void SetRootOffset(Vec3 offset)
        {
            _rootOffset = offset;
        }

        public void Step(float delta, float elapsed)
        {
            if (delta <= 0f) return;
            if (delta > MaxStep) delta = MaxStep;

            for (int c = 0; c < _chains.Count; c++)
            {
                var chain = _chains[c];
                if (chain.joints.Count == 0) continue;

                chain.joints[0].position = chain.joints[0].restPosition + _rootOffset;
                chain.joints[0].previous = chain.joints[0].position;

                for (int j = 1; j < chain.joints.Count; j++)
                {
                    var joint = chain.joints[j];
                    var parent = chain.joints[j - 1];

                    var currentDir = (joint.position - parent.position).Normalized();
                    if (currentDir.LengthSq() < RestEpsilon) currentDir = RestDirection(chain, j, elapsed);

                    var restDir = RestDirection(chain, j, elapsed);

                    var dir = currentDir;
                    dir = dir + joint.gravityDir * (joint.gravityPower * delta);
                    dir = dir + (restDir - dir) * SpringFactor(joint.stiffness, delta);

                    var momentum = joint.lastDir * joint.dragForce;
                    dir = (dir + momentum).Normalized();
                    if (dir.LengthSq() < RestEpsilon) dir = restDir;

                    joint.lastDir = dir;
                    joint.position = parent.position + dir * joint.length;
                    joint.previous = joint.position;
                }
            }
        }

        private static Vec3 ConstrainLength(Vec3 anchor, Vec3 target, float length)
        {
            if (length <= RestEpsilon) return anchor;

            var delta = target - anchor;
            var current = delta.Length();
            if (current <= RestEpsilon) return anchor;

            return anchor + delta * (length / current);
        }

        private static float SpringFactor(float stiffness, float delta)
        {
            if (stiffness <= 0f) return 0f;
            float rate = stiffness * stiffness * 60f;
            float factor = rate * delta;
            if (factor > 1f) factor = 1f;
            return factor;
        }

        private Vec3 RestDirection(PreviewChain chain, int index, float elapsed)
        {
            var joint = chain.joints[index];
            var parent = chain.joints[index - 1];

            var restDir = joint.restPosition - parent.restPosition;
            var len = restDir.Length();
            if (len < RestEpsilon) return new Vec3(0f, -1f, 0f);

            var dir = restDir / len;

            if (elapsed > 0f && index > 1)
            {
                float sway = (float)Math.Sin(elapsed * 2.0f + index) * 0.15f;
                dir = (dir + new Vec3(sway, 0f, 0f)).Normalized();
            }

            return dir;
        }

        public int JointCount()
        {
            int total = 0;
            for (int c = 0; c < _chains.Count; c++) total += _chains[c].joints.Count;
            return total;
        }

        public Vec3 TipOf(int chainIndex)
        {
            if (chainIndex < 0 || chainIndex >= _chains.Count) return new Vec3(0f, 0f, 0f);
            var chain = _chains[chainIndex];
            if (chain.joints.Count == 0) return new Vec3(0f, 0f, 0f);
            return chain.joints[chain.joints.Count - 1].position;
        }

        public float MaxDisplacement()
        {
            float worst = 0f;
            for (int c = 0; c < _chains.Count; c++)
            {
                var chain = _chains[c];
                for (int j = 0; j < chain.joints.Count; j++)
                {
                    float d = Vec3.Distance(chain.joints[j].position, chain.joints[j].restPosition);
                    if (d > worst) worst = d;
                }
            }
            return worst;
        }
    }
}