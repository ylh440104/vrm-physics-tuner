using System.Collections.Generic;
using UnityEngine;

namespace Neko.VrmPhysicsTuner.Editor
{
    public class DetectedChain
    {
        public SpringBoneCategory category;
        public string label = "";
        public List<Transform> bones = new List<Transform>();
    }

    public static class AvatarScanner
    {
        private const int MaxChainLength = 16;

        public static List<DetectedChain> Scan(GameObject root)
        {
            var result = new List<DetectedChain>();
            if (root == null) return result;

            var all = root.GetComponentsInChildren<Transform>(true);
            var claimed = new HashSet<Transform>();

            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (claimed.Contains(t)) continue;
                if (t == root.transform) continue;

                var category = VrmPhysicsProfiler.Classify(t.name);
                if (category == SpringBoneCategory.Unknown) continue;

                var chain = BuildChainFrom(t, category, claimed);
                if (chain.bones.Count > 0) result.Add(chain);
            }

            return result;
        }

        private static DetectedChain BuildChainFrom(Transform start, SpringBoneCategory category, HashSet<Transform> claimed)
        {
            var chain = new DetectedChain();
            chain.category = category;
            chain.label = VrmPhysicsProfiler.CategoryName(category) + ":" + VrmPhysicsProfiler.StripDigits(start.name);

            var current = start;
            int guard = 0;
            while (current != null && guard < MaxChainLength)
            {
                if (claimed.Contains(current)) break;

                var currentCategory = VrmPhysicsProfiler.Classify(current.name);
                if (current != start && currentCategory != SpringBoneCategory.Unknown && currentCategory != category) break;

                chain.bones.Add(current);
                claimed.Add(current);
                guard++;

                current = PickSuccessor(current, category);
            }

            return chain;
        }

        private static Transform PickSuccessor(Transform parent, SpringBoneCategory category)
        {
            Transform fallback = null;
            int count = parent.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = parent.GetChild(i);
                if (child.GetComponent<SkinnedMeshRenderer>() != null) continue;
                if (fallback == null) fallback = child;

                var childCategory = VrmPhysicsProfiler.Classify(child.name);
                if (childCategory == category) return child;
            }
            return fallback;
        }

        public static List<DetectedChain> ScanSkirt(GameObject root, Transform hips)
        {
            var result = new List<DetectedChain>();
            if (root == null || hips == null) return result;

            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (VrmPhysicsProfiler.Classify(t.name) != SpringBoneCategory.Unknown) continue;
                if (!IsDirectChildOf(t, hips)) continue;
                if (t.GetComponent<SkinnedMeshRenderer>() != null) continue;
                if (t.childCount == 0) continue;

                var chain = new DetectedChain();
                chain.category = SpringBoneCategory.Skirt;
                chain.label = "skirt:" + t.name;
                chain.bones.Add(t);
                result.Add(chain);
            }
            return result;
        }

        private static bool IsDirectChildOf(Transform child, Transform parent)
        {
            return child.parent == parent;
        }

        public static List<Transform> CollectColliderCandidates(GameObject root)
        {
            var bones = new List<Transform>();
            if (root == null) return bones;

            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.Head));
                AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.Chest));
                AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.Spine));
                AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.Hips));
                AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
                AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
                return bones;
            }

            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (MatchesAny(t.name, "head", "chest", "spine", "hips", "upperarm", "upper_arm"))
                    bones.Add(t);
            }
            return bones;
        }

        private static void AddIfPresent(List<Transform> list, Transform t)
        {
            if (t != null) list.Add(t);
        }

        private static bool MatchesAny(string name, params string[] needles)
        {
            var tokens = VrmPhysicsProfiler.Tokenize(name);
            for (int i = 0; i < needles.Length; i++)
            {
                for (int t = 0; t < tokens.Count; t++)
                {
                    if (tokens[t] == needles[i]) return true;
                }
            }
            return false;
        }
    }
}