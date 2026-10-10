using System.Collections.Generic;
using UnityEngine;

namespace VrmPhysicsTuner.Editor
{
    public static class PhysicsConfigBuilder
    {
        public static VrmPhysicsConfig Build(GameObject root, List<DetectedChain> chains, List<Transform> colliderBones, float padding)
        {
            return Build(root, chains, colliderBones, padding, DefaultFallbackFactor);
        }

        public const float DefaultFallbackFactor = 0.5f;

        public static VrmPhysicsConfig Build(GameObject root, List<DetectedChain> chains, List<Transform> colliderBones, float padding, float fallbackFactor)
        {
            var config = new VrmPhysicsConfig();
            if (root != null) config.model = root.name;

            if (colliderBones != null)
            {
                for (int i = 0; i < colliderBones.Count; i++)
                {
                    var collider = BuildCollider(colliderBones[i], padding, fallbackFactor);
                    if (collider != null) config.colliders.Add(collider);
                }
            }

            if (chains != null)
            {
                for (int i = 0; i < chains.Count; i++)
                {
                    var chain = BuildChain(chains[i], colliderBones);
                    if (chain != null) config.chains.Add(chain);
                }
            }

            return config;
        }

        private static VrmSpringBoneCollider BuildCollider(Transform bone, float padding, float fallbackFactor)
        {
            if (bone == null) return null;

            var anchors = SampleClothAnchors(bone);
            var collider = new VrmSpringBoneCollider();
            collider.bone = bone.name;
            collider.bone_path = HierarchyPath(bone);

            var tail = PickLongestChild(bone);
            float lateral;

            if (tail != null)
            {
                collider.shape = "capsule";
                collider.offset = new float[] { 0f, 0f, 0f };
                var tailLocal = bone.InverseTransformPoint(tail.position);
                collider.tail = new float[] { tailLocal.x, tailLocal.y, tailLocal.z };
                lateral = RawCapsuleSpread(ToVec3(bone.position), ToVec3(tail.position), anchors);
            }
            else
            {
                collider.shape = "sphere";
                collider.offset = new float[] { 0f, 0f, 0f };
                collider.tail = null;
                lateral = RawSphereSpread(ToVec3(bone.position), anchors);
            }

            float ownSegment = OwnSegmentLength(bone);
            float floor = ownSegment * fallbackFactor;
            float radius = lateral > floor ? lateral : floor;
            collider.radius = ColliderAutoFit.Clamp(radius * (1f + padding));
            return collider;
        }

        private static float RawCapsuleSpread(Vec3 head, Vec3 tail, List<Vec3> samples)
        {
            if (samples.Count == 0) return 0f;
            float maxDist = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                float d = ColliderAutoFit.DistanceToSegment(samples[i], head, tail);
                if (d > maxDist) maxDist = d;
            }
            return maxDist;
        }

        private static float RawSphereSpread(Vec3 center, List<Vec3> samples)
        {
            if (samples.Count == 0) return 0f;
            float maxSq = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                float d = (samples[i] - center).LengthSq();
                if (d > maxSq) maxSq = d;
            }
            return (float)System.Math.Sqrt(maxSq);
        }

        private static float OwnSegmentLength(Transform bone)
        {
            if (bone == null) return 0f;
            float longest = 0f;
            for (int i = 0; i < bone.childCount; i++)
            {
                var child = bone.GetChild(i);
                if (child.GetComponent<SkinnedMeshRenderer>() != null) continue;
                float d = Vector3.Distance(bone.position, child.position);
                if (d > longest) longest = d;
            }
            return longest;
        }

        public static string HierarchyPath(Transform t)
        {
            if (t == null) return "";
            var parts = new List<string>();
            var current = t;
            while (current != null)
            {
                parts.Add(DisambiguatedName(current));
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static string DisambiguatedName(Transform t)
        {
            var parent = t.parent;
            if (parent == null) return t.name;
            if (!HasDuplicateSiblingName(parent, t.name)) return t.name;
            return t.name + "[" + SiblingIndex(parent, t) + "]";
        }

        private static bool HasDuplicateSiblingName(Transform parent, string name)
        {
            int matches = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == name)
                {
                    matches++;
                    if (matches > 1) return true;
                }
            }
            return false;
        }

        private static int SiblingIndex(Transform parent, Transform child)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) == child) return i;
            }
            return 0;
        }

        private static Vec3 ToVec3(Vector3 v)
        {
            return new Vec3(v.x, v.y, v.z);
        }

        private static Transform PickLongestChild(Transform bone)
        {
            Transform best = null;
            float bestLen = -1f;
            for (int i = 0; i < bone.childCount; i++)
            {
                var child = bone.GetChild(i);
                if (child.GetComponent<SkinnedMeshRenderer>() != null) continue;
                float len = (child.position - bone.position).sqrMagnitude;
                if (len > bestLen) { bestLen = len; best = child; }
            }
            return best;
        }

        private static List<Vec3> SampleClothAnchors(Transform bone)
        {
            var anchors = new List<Vec3>();
            int count = bone.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = bone.GetChild(i);
                if (child.GetComponent<SkinnedMeshRenderer>() != null) continue;
                if (!VrmPhysicsProfiler.IsLikelySpringBone(child.name)) continue;
                var p = child.position;
                anchors.Add(new Vec3(p.x, p.y, p.z));
            }
            return anchors;
        }

        private static VrmSpringBoneChain BuildChain(DetectedChain detected, List<Transform> colliderBones)
        {
            if (detected == null || detected.bones.Count == 0) return null;

            var chain = VrmPhysicsProfiler.GetDefaults(detected.category);
            chain.name = detected.label;

            float thinnest = float.MaxValue;
            for (int i = 0; i < detected.bones.Count; i++)
            {
                var bone = detected.bones[i];
                if (bone == null) continue;
                chain.bones.Add(bone.name);
                chain.bone_paths.Add(HierarchyPath(bone));
                float candidate = EstimateHitRadius(bone, chain.hit_radius);
                if (candidate < thinnest) thinnest = candidate;
            }
            if (thinnest < float.MaxValue) chain.hit_radius = thinnest;

            if (colliderBones != null)
            {
                for (int i = 0; i < colliderBones.Count; i++)
                {
                    if (colliderBones[i] != null) chain.collider_bones.Add(colliderBones[i].name);
                }
            }

            return chain;
        }

        private static float EstimateHitRadius(Transform bone, float fallback)
        {
            if (bone.parent == null) return fallback;
            float segment = Vector3.Distance(bone.position, bone.parent.position);
            if (segment <= 0f) return fallback;
            float radius = segment * 0.25f;
            if (radius < ColliderAutoFit.MinRadius) radius = ColliderAutoFit.MinRadius;
            if (radius > ColliderAutoFit.MaxRadius) radius = ColliderAutoFit.MaxRadius;
            return radius;
        }
    }
}