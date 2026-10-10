using System;
using System.Collections.Generic;
using System.Text;

namespace VrmPhysicsTuner
{
    public enum SpringBoneCategory
    {
        Unknown,
        HairFront,
        HairSide,
        HairBack,
        Ponytail,
        Skirt,
        Sleeve,
        Tail,
        Cape,
        Ribbon,
        Ear,
        Breast,
        Accessory
    }

    public static class VrmPhysicsProfiler
    {
        private static readonly Dictionary<SpringBoneCategory, string[]> Keywords =
            new Dictionary<SpringBoneCategory, string[]>
        {
            { SpringBoneCategory.HairFront, new[] { "bang", "bangs", "fringe", "front", "forelock", "hairfront" } },
            { SpringBoneCategory.HairSide, new[] { "sidehair", "side", "sidelock", "twin", "twintail", "hairslide" } },
            { SpringBoneCategory.HairBack, new[] { "backhair", "back", "rear", "hairback", "hair" } },
            { SpringBoneCategory.Ponytail, new[] { "ponytail", "bun", "braid" } },
            { SpringBoneCategory.Skirt, new[] { "skirt", "skirtcloth", "hem" } },
            { SpringBoneCategory.Sleeve, new[] { "sleeve", "cuff", "armcloth", "cloth_arm" } },
            { SpringBoneCategory.Tail, new[] { "tail", "caudal" } },
            { SpringBoneCategory.Cape, new[] { "cape", "cloak", "mantle" } },
            { SpringBoneCategory.Ribbon, new[] { "ribbon", "bow", "ribon" } },
            { SpringBoneCategory.Ear, new[] { "ear", "ears" } },
            { SpringBoneCategory.Breast, new[] { "breast", "bust", "chestcloth" } },
            { SpringBoneCategory.Accessory, new[] { "accessory", "charm", "pendant", "strap", "belt", "necklace" } }
        };

        private static readonly Dictionary<SpringBoneCategory, string[]> Disambiguators =
            new Dictionary<SpringBoneCategory, string[]>
        {
            { SpringBoneCategory.HairFront, new[] { "front", "bang", "bangs", "fringe", "forelock" } },
            { SpringBoneCategory.HairSide, new[] { "side", "twin", "twintail", "sidelock" } },
            { SpringBoneCategory.HairBack, new[] { "back", "rear", "hairback" } }
        };

        private static readonly Dictionary<SpringBoneCategory, VrmSpringBoneChain> Defaults =
            new Dictionary<SpringBoneCategory, VrmSpringBoneChain>
        {
            { SpringBoneCategory.HairFront, Make(0.9f, 0.4f, 0.05f, 0.02f) },
            { SpringBoneCategory.HairSide, Make(0.85f, 0.4f, 0.08f, 0.02f) },
            { SpringBoneCategory.HairBack, Make(0.75f, 0.45f, 0.12f, 0.02f) },
            { SpringBoneCategory.Ponytail, Make(0.6f, 0.5f, 0.2f, 0.02f) },
            { SpringBoneCategory.Skirt, Make(0.7f, 0.5f, 0.15f, 0.03f) },
            { SpringBoneCategory.Sleeve, Make(0.75f, 0.45f, 0.12f, 0.02f) },
            { SpringBoneCategory.Tail, Make(0.55f, 0.5f, 0.25f, 0.02f) },
            { SpringBoneCategory.Cape, Make(0.5f, 0.55f, 0.3f, 0.03f) },
            { SpringBoneCategory.Ribbon, Make(0.8f, 0.45f, 0.1f, 0.015f) },
            { SpringBoneCategory.Ear, Make(0.9f, 0.4f, 0.06f, 0.02f) },
            { SpringBoneCategory.Breast, Make(0.8f, 0.5f, 0.08f, 0.03f) },
            { SpringBoneCategory.Accessory, Make(0.7f, 0.5f, 0.15f, 0.015f) },
            { SpringBoneCategory.Unknown, Make(0.8f, 0.45f, 0.1f, 0.02f) }
        };

        private static VrmSpringBoneChain Make(float stiffness, float drag, float gravity, float hitRadius)
        {
            return new VrmSpringBoneChain
            {
                stiffness = stiffness,
                drag_force = drag,
                gravity_power = gravity,
                gravity_dir = new float[] { 0f, -1f, 0f },
                hit_radius = hitRadius
            };
        }

        public static VrmSpringBoneChain GetDefaults(SpringBoneCategory category)
        {
            VrmSpringBoneChain template;
            if (!Defaults.TryGetValue(category, out template))
                template = Defaults[SpringBoneCategory.Unknown];
            return new VrmSpringBoneChain
            {
                stiffness = template.stiffness,
                drag_force = template.drag_force,
                gravity_power = template.gravity_power,
                gravity_dir = (float[])template.gravity_dir.Clone(),
                hit_radius = template.hit_radius
            };
        }

        public static List<string> Tokenize(string boneName)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(boneName)) return tokens;

            var cleaned = new StringBuilder(boneName.Length);
            for (int i = 0; i < boneName.Length; i++)
            {
                char c = boneName[i];
                if (char.IsUpper(c) && i > 0 && char.IsLower(boneName[i - 1])) { cleaned.Append(' '); cleaned.Append(c); }
                else if (char.IsDigit(c)) cleaned.Append(' ');
                else if (c == '_' || c == '-' || c == '.' || c == ' ') cleaned.Append(' ');
                else cleaned.Append(c);
            }

            var parts = cleaned.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var t = parts[i].ToLowerInvariant();
                if (t.Length == 0) continue;
                tokens.Add(t);
            }
            return tokens;
        }

        private static readonly HashSet<string> NoiseTokens = new HashSet<string>
        {
            "j", "bip", "c", "root", "spine", "hip", "hips", "chest", "upper", "lower", "arm",
            "leg", "hand", "foot", "head", "neck", "shoulder", "bone", "spr", "spring", "second",
            "secondary", "obj", "object", "grp", "group", "l", "r", "left", "right", "cloth",
            "sk", "n", "a", "m", "d", "ex", "vrm", "mixamo", "mixamorig"
        };

        public static SpringBoneCategory Classify(string boneName)
        {
            var tokens = Tokenize(boneName);
            var filtered = new List<string>();
            for (int i = 0; i < tokens.Count; i++)
            {
                if (!NoiseTokens.Contains(tokens[i])) filtered.Add(tokens[i]);
            }

            SpringBoneCategory best = SpringBoneCategory.Unknown;
            int bestScore = 0;

            foreach (var pair in Keywords)
            {
                var words = pair.Value;
                int score = 0;
                for (int w = 0; w < words.Length; w++)
                {
                    for (int t = 0; t < filtered.Count; t++)
                    {
                        if (filtered[t] == words[w])
                        {
                            score += words[w].Length;
                            break;
                        }
                    }
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = pair.Key;
                }
            }

            if (IsHairCategory(best))
            {
                string[] disambiguators;
                if (Disambiguators.TryGetValue(best, out disambiguators))
                {
                    if (!ContainsAny(filtered, disambiguators)) best = SpringBoneCategory.HairBack;
                }
            }

            return best;
        }

        private static bool IsHairCategory(SpringBoneCategory category)
        {
            return category == SpringBoneCategory.HairFront
                || category == SpringBoneCategory.HairSide
                || category == SpringBoneCategory.HairBack;
        }

        private static bool ContainsAny(List<string> tokens, string[] words)
        {
            for (int w = 0; w < words.Length; w++)
            {
                for (int t = 0; t < tokens.Count; t++)
                {
                    if (tokens[t] == words[w]) return true;
                }
            }
            return false;
        }

        public static bool IsLikelySpringBone(string boneName)
        {
            return Classify(boneName) != SpringBoneCategory.Unknown;
        }

        public static string CategoryName(SpringBoneCategory category)
        {
            switch (category)
            {
                case SpringBoneCategory.HairFront: return "hair_front";
                case SpringBoneCategory.HairSide: return "hair_side";
                case SpringBoneCategory.HairBack: return "hair_back";
                case SpringBoneCategory.Ponytail: return "ponytail";
                case SpringBoneCategory.Skirt: return "skirt";
                case SpringBoneCategory.Sleeve: return "sleeve";
                case SpringBoneCategory.Tail: return "tail";
                case SpringBoneCategory.Cape: return "cape";
                case SpringBoneCategory.Ribbon: return "ribbon";
                case SpringBoneCategory.Ear: return "ear";
                case SpringBoneCategory.Breast: return "breast";
                case SpringBoneCategory.Accessory: return "accessory";
                default: return "unknown";
            }
        }

        public static VrmSpringBoneChain BuildChain(string rootBoneName, List<string> bonePath, SpringBoneCategory category)
        {
            var chain = GetDefaults(category);
            chain.name = CategoryName(category) + ":" + StripDigits(rootBoneName);
            chain.bones = new List<string>(bonePath);
            return chain;
        }

        public static string StripDigits(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsDigit(c)) continue;
                if (i > 0 && sb.Length > 0 && sb[sb.Length - 1] == '_' && c == '_') continue;
                sb.Append(c);
            }
            return sb.ToString().Trim('_');
        }
    }
}