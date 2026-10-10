using System;
using System.Collections.Generic;
using Neko.VrmPhysicsTuner;
using UnityEngine;

namespace Neko.VrmPhysicsTuner.Tests
{
    public static class Program
    {
        private static int _pass;
        private static int _fail;
        private static readonly List<string> _failures = new List<string>();

        private static void Check(bool condition, string label)
        {
            if (condition) { _pass++; }
            else { _fail++; _failures.Add(label); Console.WriteLine("FAIL: " + label); }
        }

        private static void CheckClose(float actual, float expected, float tol, string label)
        {
            bool ok = Math.Abs(actual - expected) <= tol;
            if (!ok) Console.WriteLine("  expected " + expected + " got " + actual);
            Check(ok, label);
        }

        public static int Main()
        {
            TestVec3Math();
            TestDistanceToSegment();
            TestSphereFit();
            TestCapsuleFit();
            TestClamping();
            TestCentroid();
            TestSerializeShape();
            TestRoundTrip();
            TestJsonEscaping();
            TestJsonParser();
            TestSerializeDefaults();
            TestEmptyConfig();
            TestTokenizer();
            TestClassification();
            TestFalsePositiveGuards();
            TestCategoryDefaults();
            TestBuildChain();
            TestEndToEndPipeline();
            TestBonePathUniqueness();
            TestHitRadiusAggregation();
            TestRotatedTransformTail();
            TestAssistantPrompt();
            TestAssistantParser();
            TestSuggestionApplier();
            TestChatRequestBuilder();
            TestChatResponseReader();
            TestPreviewSimulator();

            Console.WriteLine();
            Console.WriteLine("passed: " + _pass + "  failed: " + _fail);
            if (_fail > 0)
            {
                Console.WriteLine("--- failures ---");
                foreach (var f in _failures) Console.WriteLine("  " + f);
            }
            return _fail == 0 ? 0 : 1;
        }

        private static void TestVec3Math()
        {
            var a = new Vec3(1f, 2f, 3f);
            var b = new Vec3(4f, 5f, 6f);
            CheckClose((a + b).x, 5f, 1e-5f, "add.x");
            CheckClose((b - a).y, 3f, 1e-5f, "sub.y");
            CheckClose((a * 2f).z, 6f, 1e-5f, "mul.z");
            CheckClose((2f * a).z, 6f, 1e-5f, "rmul.z");
            CheckClose((a / 2f).x, 0.5f, 1e-5f, "div.x");
            CheckClose(a.Length(), 3.741657f, 1e-4f, "length");
            CheckClose(a.LengthSq(), 14f, 1e-4f, "lengthSq");
            var n = a.Normalized();
            CheckClose(n.Length(), 1f, 1e-4f, "normalized unit length");
            CheckClose(Vec3.Dot(a, b), 32f, 1e-4f, "dot");
            var c = Vec3.Cross(new Vec3(1f, 0f, 0f), new Vec3(0f, 1f, 0f));
            CheckClose(c.z, 1f, 1e-5f, "cross.z");
            CheckClose(Vec3.Distance(a, b), 5.196152f, 1e-4f, "distance");
            CheckClose(new Vec3(0f, 0f, 0f).Normalized().Length(), 0f, 1e-6f, "zero normalize safe");
        }

        private static void TestDistanceToSegment()
        {
            var a = new Vec3(0f, 0f, 0f);
            var b = new Vec3(0f, 10f, 0f);

            CheckClose(ColliderAutoFit.DistanceToSegment(new Vec3(3f, 5f, 0f), a, b), 3f, 1e-4f, "perpendicular hit");
            CheckClose(ColliderAutoFit.DistanceToSegment(new Vec3(0f, -5f, 0f), a, b), 5f, 1e-4f, "beyond head clamps");
            CheckClose(ColliderAutoFit.DistanceToSegment(new Vec3(0f, 15f, 0f), a, b), 5f, 1e-4f, "beyond tail clamps");
            CheckClose(ColliderAutoFit.DistanceToSegment(new Vec3(4f, 0f, 3f), a, b), 5f, 1e-4f, "3d perpendicular");
            CheckClose(ColliderAutoFit.DistanceToSegment(new Vec3(1f, 2f, 3f), a, a), 3.741657f, 1e-4f, "degenerate segment");
        }

        private static void TestSphereFit()
        {
            var samples = new Vec3[]
            {
                new Vec3(0.1f, 0f, 0f),
                new Vec3(-0.1f, 0f, 0f),
                new Vec3(0f, 0.08f, 0f),
                new Vec3(0f, 0f, 0.05f)
            };
            var center = new Vec3(0f, 0f, 0f);
            CheckClose(ColliderAutoFit.FitSphereRadius(center, samples, 0f), 0.1f, 1e-4f, "sphere radius no padding");
            CheckClose(ColliderAutoFit.FitSphereRadius(center, samples, 0.5f), 0.15f, 1e-4f, "sphere radius 50% padding");

            var offCenter = new Vec3(0.1f, 0f, 0f);
            CheckClose(ColliderAutoFit.FitSphereRadius(offCenter, samples, 0f), 0.2f, 1e-4f, "sphere from offset center");

            CheckClose(ColliderAutoFit.FitSphereRadius(center, new Vec3[0], 0f), ColliderAutoFit.MinRadius, 1e-6f, "empty samples -> min");
            CheckClose(ColliderAutoFit.FitSphereRadius(center, null, 0f), ColliderAutoFit.MinRadius, 1e-6f, "null samples -> min");
        }

        private static void TestCapsuleFit()
        {
            var head = new Vec3(0f, 0f, 0f);
            var tail = new Vec3(0f, 10f, 0f);
            var samples = new Vec3[]
            {
                new Vec3(0.2f, 3f, 0f),
                new Vec3(-0.15f, 7f, 0f),
                new Vec3(0f, 5f, 0.1f)
            };
            CheckClose(ColliderAutoFit.FitCapsuleRadius(head, tail, samples, 0f), 0.2f, 1e-4f, "capsule max lateral dist");
            CheckClose(ColliderAutoFit.FitCapsuleRadius(head, tail, samples, 0.25f), 0.25f, 1e-4f, "capsule with 25% padding");

            var wide = new Vec3[]
            {
                new Vec3(0.5f, 0f, 0f)
            };
            CheckClose(ColliderAutoFit.FitCapsuleRadius(head, tail, wide, 0f), 0.5f, 1e-4f, "capsule endpoint sample");
        }

        private static void TestClamping()
        {
            CheckClose(ColliderAutoFit.Clamp(0.0000001f), ColliderAutoFit.MinRadius, 1e-8f, "clamp tiny");
            CheckClose(ColliderAutoFit.Clamp(100f), ColliderAutoFit.MaxRadius, 1e-8f, "clamp huge");
            CheckClose(ColliderAutoFit.Clamp(0.05f), 0.05f, 1e-8f, "clamp passthrough");

            var far = new Vec3[] { new Vec3(1000f, 0f, 0f) };
            CheckClose(ColliderAutoFit.FitSphereRadius(new Vec3(0f, 0f, 0f), far, 0f), ColliderAutoFit.MaxRadius, 1e-6f, "fit clamps to max");

            var origin = new Vec3[] { new Vec3(0f, 0f, 0f) };
            CheckClose(ColliderAutoFit.FitSphereRadius(new Vec3(0f, 0f, 0f), origin, 0f), ColliderAutoFit.MinRadius, 1e-6f, "fit clamps to min");
        }

        private static void TestCentroid()
        {
            var samples = new Vec3[]
            {
                new Vec3(0f, 0f, 0f),
                new Vec3(2f, 0f, 0f),
                new Vec3(0f, 3f, 0f)
            };
            var c = ColliderAutoFit.Centroid(samples);
            CheckClose(c.x, 2f / 3f, 1e-4f, "centroid.x");
            CheckClose(c.y, 1f, 1e-4f, "centroid.y");
            CheckClose(c.z, 0f, 1e-4f, "centroid.z");
            var empty = ColliderAutoFit.Centroid(new Vec3[0]);
            CheckClose(empty.x, 0f, 1e-6f, "centroid empty safe");
        }

        private static VrmPhysicsConfig BuildSampleConfig()
        {
            var config = new VrmPhysicsConfig();
            config.model = "sister1.0";
            config.colliders.Add(new VrmSpringBoneCollider
            {
                bone = "J_Bip_C_Head",
                shape = "sphere",
                radius = 0.09f,
                offset = new float[] { 0f, 0.02f, 0f }
            });
            config.colliders.Add(new VrmSpringBoneCollider
            {
                bone = "J_Bip_C_Chest",
                shape = "capsule",
                radius = 0.06f,
                offset = new float[] { 0f, 0f, 0f },
                tail = new float[] { 0f, 0.12f, 0f }
            });
            config.chains.Add(new VrmSpringBoneChain
            {
                name = "hair_front",
                bones = new List<string> { "hair_front_01", "hair_front_02" },
                stiffness = 0.85f,
                drag_force = 0.4f,
                gravity_power = 0.2f,
                gravity_dir = new float[] { 0f, -1f, 0f },
                hit_radius = 0.02f,
                collider_bones = new List<string> { "J_Bip_C_Head" }
            });
            return config;
        }

        private static void TestSerializeShape()
        {
            var json = VrmPhysicsConfigSerializer.Serialize(BuildSampleConfig());

            Check(json.Contains("\"version\":1"), "serialize version");
            Check(json.Contains("\"model\":\"sister1.0\""), "serialize model");
            Check(json.Contains("\"bone\":\"J_Bip_C_Head\""), "serialize collider bone");
            Check(json.Contains("\"shape\":\"sphere\""), "serialize shape");
            Check(json.Contains("\"radius\":0.09"), "serialize radius");
            Check(json.Contains("\"radius\":0.06"), "serialize capsule radius");
            Check(json.Contains("\"tail\":[0,0.12,0]"), "serialize tail vector");
            Check(json.Contains("\"name\":\"hair_front\""), "serialize chain name");
            Check(json.Contains("\"stiffness\":0.85"), "serialize stiffness");
            Check(json.Contains("\"gravity_power\":0.2"), "serialize gravity_power");
            Check(json.Contains("\"hit_radius\":0.02"), "serialize hit_radius");
            Check(json.Contains("\"hair_front_01\""), "serialize chain bones");

            int opens = CountChar(json, '{');
            int closes = CountChar(json, '}');
            Check(opens == closes, "balanced braces");
            int brackets = CountChar(json, '[');
            int bracketsClose = CountChar(json, ']');
            Check(brackets == bracketsClose, "balanced brackets");
        }

        private static int CountChar(string s, char c)
        {
            int n = 0;
            for (int i = 0; i < s.Length; i++) if (s[i] == c) n++;
            return n;
        }

        private static void TestRoundTrip()
        {
            var original = BuildSampleConfig();
            var json = VrmPhysicsConfigSerializer.Serialize(original);
            var restored = VrmPhysicsConfigSerializer.Deserialize(json);

            Check(restored.version == original.version, "roundtrip version");
            Check(restored.model == original.model, "roundtrip model");
            Check(restored.colliders.Count == 2, "roundtrip collider count");
            Check(restored.chains.Count == 1, "roundtrip chain count");
            Check(restored.colliders[0].bone == "J_Bip_C_Head", "roundtrip collider bone");
            CheckClose(restored.colliders[0].radius, 0.09f, 1e-5f, "roundtrip collider radius");
            Check(restored.colliders[1].shape == "capsule", "roundtrip capsule shape");
            CheckClose(restored.colliders[1].tail[1], 0.12f, 1e-5f, "roundtrip tail");
            Check(restored.chains[0].name == "hair_front", "roundtrip chain name");
            Check(restored.chains[0].bones.Count == 2, "roundtrip bones count");
            Check(restored.chains[0].bones[1] == "hair_front_02", "roundtrip bone order");
            CheckClose(restored.chains[0].stiffness, 0.85f, 1e-5f, "roundtrip stiffness");
            Check(restored.chains[0].collider_bones.Count == 1, "roundtrip collider_bones");

            var reserialized = VrmPhysicsConfigSerializer.Serialize(restored);
            Check(reserialized == json, "roundtrip is byte-stable");
        }

        private static void TestJsonEscaping()
        {
            var config = new VrmPhysicsConfig();
            config.model = "weird\"name\\with\ttabs";
            var json = VrmPhysicsConfigSerializer.Serialize(config);
            Check(json.Contains("\\\""), "escapes quote");
            Check(json.Contains("\\\\"), "escapes backslash");
            Check(json.Contains("\\t"), "escapes tab");

            var back = VrmPhysicsConfigSerializer.Deserialize(json);
            Check(back.model == config.model, "escaping roundtrips");
        }

        private static void TestJsonParser()
        {
            var obj = JsonParser.Parse("{\"a\":1,\"b\":[1,2,3],\"c\":\"x\",\"d\":true,\"e\":null}").AsObject();
            Check(obj.Get("a").AsInt(0) == 1, "parser int");
            Check(obj.Get("b").AsArray().Count == 3, "parser array");
            Check(obj.Get("c").AsString("") == "x", "parser string");
            Check((bool)obj.Get("d") == true, "parser bool");
            Check(obj.Get("e") == null, "parser null");

            var nested = JsonParser.Parse("{\"outer\":{\"inner\":42}}").AsObject();
            Check(nested.Get("outer").AsObject().Get("inner").AsInt(0) == 42, "parser nested");

            var floats = JsonParser.Parse("[0.5,-1.25,1e3]").AsArray();
            CheckClose(floats[0].AsFloat(0f), 0.5f, 1e-6f, "parser float");
            CheckClose(floats[1].AsFloat(0f), -1.25f, 1e-6f, "parser negative");
            CheckClose(floats[2].AsFloat(0f), 1000f, 1e-3f, "parser exponent");

            bool threw = false;
            try { JsonParser.Parse("{bad}"); } catch (FormatException) { threw = true; }
            Check(threw, "parser rejects malformed");

            threw = false;
            try { JsonParser.Parse("{\"a\":1} trailing"); } catch (FormatException) { threw = true; }
            Check(threw, "parser rejects trailing content");
        }

        private static void TestSerializeDefaults()
        {
            var config = new VrmPhysicsConfig();
            var json = VrmPhysicsConfigSerializer.Serialize(config);
            Check(json.Contains("\"version\":1"), "default version");
            Check(json.Contains("\"colliders\":[]"), "empty colliders");
            Check(json.Contains("\"chains\":[]"), "empty chains");

            var back = VrmPhysicsConfigSerializer.Deserialize(json);
            Check(back.colliders.Count == 0, "empty colliders roundtrip");
            Check(back.chains.Count == 0, "empty chains roundtrip");
        }

        private static void TestEmptyConfig()
        {
            var chain = new VrmSpringBoneChain();
            var json = VrmPhysicsConfigSerializer.Serialize(new VrmPhysicsConfig
            {
                chains = new List<VrmSpringBoneChain> { chain }
            });
            var back = VrmPhysicsConfigSerializer.Deserialize(json);
            Check(back.chains.Count == 1, "default chain survives");
            CheckClose(back.chains[0].gravity_dir[1], -1f, 1e-6f, "default gravity dir is -Y");
            Check(back.chains[0].center == null, "null center stays null");

            var collider = new VrmSpringBoneCollider();
            var cjson = VrmPhysicsConfigSerializer.Serialize(new VrmPhysicsConfig
            {
                colliders = new List<VrmSpringBoneCollider> { collider }
            });
            Check(cjson.Contains("\"tail\":null"), "sphere collider tail is null");
            Check(cjson.Contains("\"shape\":\"sphere\""), "default shape sphere");
        }

        private static void TestTokenizer()
        {
            var t1 = VrmPhysicsProfiler.Tokenize("J_Bip_C_Head");
            Check(t1.Contains("head"), "tokenize J_Bip_C_Head finds head");
            Check(t1.Contains("bip"), "tokenize keeps bip");

            var t2 = VrmPhysicsProfiler.Tokenize("hair_front_01");
            Check(t2.Contains("hair"), "tokenize underscore splits hair");
            Check(t2.Contains("front"), "tokenize underscore splits front");
            Check(!t2.Contains("01"), "tokenize drops pure digits");

            var t3 = VrmPhysicsProfiler.Tokenize("mixamorigSpine1");
            Check(t3.Contains("mixamorig"), "tokenize camelCase first word");
            Check(t3.Contains("spine"), "tokenize camelCase splits spine");

            var t4 = VrmPhysicsProfiler.Tokenize("Skirt_Side_L");
            Check(t4.Contains("skirt"), "tokenize lowercase skirt");
            Check(t4.Contains("side"), "tokenize lowercase side");
            Check(t4.Contains("l"), "tokenize keeps l");

            Check(VrmPhysicsProfiler.Tokenize("").Count == 0, "tokenize empty");
            Check(VrmPhysicsProfiler.Tokenize(null).Count == 0, "tokenize null safe");
        }

        private static void TestClassification()
        {
            Check(VrmPhysicsProfiler.Classify("hair_front_01") == SpringBoneCategory.HairFront, "classify hair front");
            Check(VrmPhysicsProfiler.Classify("J_Bip_C_HairBack1") == SpringBoneCategory.HairBack, "classify hair back");
            Check(VrmPhysicsProfiler.Classify("Skirt_01") == SpringBoneCategory.Skirt, "classify skirt");
            Check(VrmPhysicsProfiler.Classify("Ponytail_02") == SpringBoneCategory.Ponytail, "classify ponytail");
            Check(VrmPhysicsProfiler.Classify("tail_01") == SpringBoneCategory.Tail, "classify tail");
            Check(VrmPhysicsProfiler.Classify("Cape_L_02") == SpringBoneCategory.Cape, "classify cape");
            Check(VrmPhysicsProfiler.Classify("Ribbon_R") == SpringBoneCategory.Ribbon, "classify ribbon");
            Check(VrmPhysicsProfiler.Classify("Sleeve_L_01") == SpringBoneCategory.Sleeve, "classify sleeve");
            Check(VrmPhysicsProfiler.Classify("J_Bip_C_Spine") == SpringBoneCategory.Unknown, "spine is not spring bone");
            Check(VrmPhysicsProfiler.Classify("J_Bip_L_UpperArm") == SpringBoneCategory.Unknown, "arm is not spring bone");
            Check(VrmPhysicsProfiler.Classify("") == SpringBoneCategory.Unknown, "empty is unknown");
            Check(VrmPhysicsProfiler.Classify(null) == SpringBoneCategory.Unknown, "null is unknown");

            Check(VrmPhysicsProfiler.Classify("hair_01") == SpringBoneCategory.HairBack, "bare hair defaults to hair back");
            Check(VrmPhysicsProfiler.Classify("hair_front_01") == SpringBoneCategory.HairFront, "hair front beats bare hair");
            Check(VrmPhysicsProfiler.Classify("hair_side_01") == SpringBoneCategory.HairSide, "hair side beats bare hair");
            Check(VrmPhysicsProfiler.Classify("HairBack_01") == SpringBoneCategory.HairBack, "hair back explicit");
            Check(VrmPhysicsProfiler.IsLikelySpringBone("hair_01"), "bare hair is a spring bone");
        }

        private static void TestFalsePositiveGuards()
        {
            Check(VrmPhysicsProfiler.Classify("J_Bip_L_Forearm") == SpringBoneCategory.Unknown,
                "forearm must not match ear");
            Check(VrmPhysicsProfiler.Classify("J_Bip_R_Forearm") == SpringBoneCategory.Unknown,
                "right forearm must not match ear");
            Check(VrmPhysicsProfiler.Classify("J_Bip_L_Hand") == SpringBoneCategory.Unknown,
                "hand is not a spring bone");
            Check(VrmPhysicsProfiler.Classify("J_Bip_C_UpperChest") == SpringBoneCategory.Unknown,
                "upperchest is not breast cloth");
            Check(VrmPhysicsProfiler.Classify("J_Bip_C_Hips") == SpringBoneCategory.Unknown,
                "hips is not a spring bone");
            Check(VrmPhysicsProfiler.Classify("J_Bip_L_LowerLeg") == SpringBoneCategory.Unknown,
                "lower leg is not a spring bone");

            Check(VrmPhysicsProfiler.Classify("Ear_L") == SpringBoneCategory.Ear, "real ear still detected");
            Check(VrmPhysicsProfiler.Classify("J_Bip_C_Ear1") == SpringBoneCategory.Ear, "ear with suffix detected");
        }

        private static void TestCategoryDefaults()
        {
            var hairFront = VrmPhysicsProfiler.GetDefaults(SpringBoneCategory.HairFront);
            var ponytail = VrmPhysicsProfiler.GetDefaults(SpringBoneCategory.Ponytail);
            Check(hairFront.stiffness > ponytail.stiffness, "hair front stiffer than ponytail");
            Check(hairFront.gravity_power < ponytail.gravity_power, "ponytail heavier than hair front");

            var skirt = VrmPhysicsProfiler.GetDefaults(SpringBoneCategory.Skirt);
            Check(skirt.hit_radius > hairFront.hit_radius, "skirt has larger hit radius");

            var a = VrmPhysicsProfiler.GetDefaults(SpringBoneCategory.Tail);
            var b = VrmPhysicsProfiler.GetDefaults(SpringBoneCategory.Tail);
            a.gravity_dir[0] = 99f;
            Check(b.gravity_dir[0] != 99f, "defaults return independent copies");

            var unknown = VrmPhysicsProfiler.GetDefaults(SpringBoneCategory.Unknown);
            Check(unknown.stiffness > 0f, "unknown category has sane defaults");
        }

        private static void TestBuildChain()
        {
            var path = new List<string> { "hair_front_01", "hair_front_02", "hair_front_03" };
            var chain = VrmPhysicsProfiler.BuildChain("hair_front_01", path, SpringBoneCategory.HairFront);
            Check(chain.name.StartsWith("hair_front:"), "chain name prefixed by category");
            Check(chain.bones.Count == 3, "chain keeps bone path");
            Check(chain.bones[2] == "hair_front_03", "chain preserves order");
            Check(chain.stiffness == VrmPhysicsProfiler.GetDefaults(SpringBoneCategory.HairFront).stiffness,
                "chain uses category defaults");

            Check(VrmPhysicsProfiler.StripDigits("hair_front_01") == "hair_front", "strip digits keeps underscores");
            Check(VrmPhysicsProfiler.StripDigits("Skirt02") == "Skirt", "strip digits camel");
            Check(VrmPhysicsProfiler.StripDigits("") == "", "strip digits empty safe");

            Check(VrmPhysicsProfiler.IsLikelySpringBone("Skirt_01"), "likely spring bone skirt");
            Check(!VrmPhysicsProfiler.IsLikelySpringBone("J_Bip_C_Head"), "head not spring bone");
        }

        private static GameObject MakeBone(string name, Transform parent, float x, float y, float z)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(x, y, z);
            if (parent != null) go.transform.SetParent(parent);
            return go;
        }

        private static GameObject BuildSyntheticAvatar()
        {
            var root = new GameObject("SyntheticAvatar");
            root.transform.position = new Vector3(0f, 0f, 0f);

            var hips = MakeBone("J_Bip_C_Hips", root.transform, 0f, 0.9f, 0f);
            var spine = MakeBone("J_Bip_C_Spine", hips.transform, 0f, 1.05f, 0f);
            var chest = MakeBone("J_Bip_C_Chest", spine.transform, 0f, 1.2f, 0f);
            var head = MakeBone("J_Bip_C_Head", chest.transform, 0f, 1.5f, 0f);

            var hair1 = MakeBone("hair_front_01", head.transform, 0.03f, 1.62f, 0.08f);
            var hair2 = MakeBone("hair_front_02", hair1.transform, 0.03f, 1.52f, 0.09f);
            MakeBone("hair_front_03", hair2.transform, 0.03f, 1.42f, 0.10f);

            var skirtL = MakeBone("Skirt_L_01", hips.transform, 0.10f, 0.85f, 0f);
            var skirtL2 = MakeBone("Skirt_L_02", skirtL.transform, 0.11f, 0.70f, 0f);
            MakeBone("Skirt_L_03", skirtL2.transform, 0.12f, 0.55f, 0f);

            var tail1 = MakeBone("tail_01", hips.transform, -0.08f, 0.88f, -0.05f);
            MakeBone("tail_02", tail1.transform, -0.10f, 0.75f, -0.12f);

            MakeBone("J_Bip_L_Forearm", chest.transform, 0.2f, 1.1f, 0f);

            return root;
        }

        private static void TestEndToEndPipeline()
        {
            var root = BuildSyntheticAvatar();
            var chains = VrmPhysicsTuner.Editor.AvatarScanner.Scan(root);

            Check(chains.Count >= 3, "e2e scans at least hair+skirt+tail");

            bool foundHair = false;
            bool foundSkirt = false;
            bool foundTail = false;
            int hairLength = 0;
            for (int i = 0; i < chains.Count; i++)
            {
                if (chains[i].category == SpringBoneCategory.HairFront) { foundHair = true; hairLength = chains[i].bones.Count; }
                if (chains[i].category == SpringBoneCategory.Skirt) foundSkirt = true;
                if (chains[i].category == SpringBoneCategory.Tail) foundTail = true;
            }
            Check(foundHair, "e2e finds hair chain");
            Check(foundSkirt, "e2e finds skirt chain");
            Check(foundTail, "e2e finds tail chain");
            Check(hairLength == 3, "e2e hair chain has 3 bones");

            for (int i = 0; i < chains.Count; i++)
            {
                Check(chains[i].category != SpringBoneCategory.Ear,
                    "e2e forearm not misclassified as ear (chain " + i + ")");
            }

            var colliderBones = VrmPhysicsTuner.Editor.AvatarScanner.CollectColliderCandidates(root);
            Check(colliderBones.Count >= 3, "e2e finds collider candidate bones");

            var config = VrmPhysicsTuner.Editor.PhysicsConfigBuilder.Build(root, chains, colliderBones, 0.1f);
            Check(config.model == "SyntheticAvatar", "e2e config model name");
            Check(config.chains.Count == chains.Count, "e2e config chain count matches scan");
            Check(config.colliders.Count == colliderBones.Count, "e2e config collider count matches bones");

            for (int i = 0; i < config.colliders.Count; i++)
            {
                var c = config.colliders[i];
                Check(c.radius >= ColliderAutoFit.MinRadius && c.radius <= ColliderAutoFit.MaxRadius,
                    "e2e collider radius in range for " + c.bone);
                Check(!string.IsNullOrEmpty(c.shape), "e2e collider has shape");
            }

            for (int i = 0; i < config.chains.Count; i++)
            {
                Check(config.chains[i].bones.Count > 0, "e2e chain has bones " + config.chains[i].name);
                Check(config.chains[i].hit_radius > 0f, "e2e chain hit radius positive " + config.chains[i].name);
            }

            var json = VrmPhysicsConfigSerializer.Serialize(config);
            var restored = VrmPhysicsConfigSerializer.Deserialize(json);
            Check(restored.chains.Count == config.chains.Count, "e2e json roundtrip chains");
            Check(restored.colliders.Count == config.colliders.Count, "e2e json roundtrip colliders");

            var outPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vrm-physics-tuner-e2e.json");
            System.IO.File.WriteAllText(outPath, json);
            Check(System.IO.File.Exists(outPath), "e2e wrote output json");

            CheckColliderQuality(config);
        }

        private static void CheckColliderQuality(VrmPhysicsConfig config)
        {
            VrmSpringBoneCollider head = null;
            VrmSpringBoneCollider hips = null;
            for (int i = 0; i < config.colliders.Count; i++)
            {
                if (config.colliders[i].bone == "J_Bip_C_Head") head = config.colliders[i];
                if (config.colliders[i].bone == "J_Bip_C_Hips") hips = config.colliders[i];
            }

            Check(head != null, "quality: head collider exists");
            Check(hips != null, "quality: hips collider exists");

            if (head != null)
            {
                Check(head.radius > ColliderAutoFit.MinRadius * 2f,
                    "quality: head radius not collapsed (got " + head.radius + ")");
                Check(head.shape == "capsule", "quality: head collider is capsule");
            }

            if (hips != null)
            {
                Check(hips.radius > ColliderAutoFit.MinRadius * 2f,
                    "quality: hips radius not collapsed (got " + hips.radius + ")");
            }

            int collapsed = 0;
            for (int i = 0; i < config.colliders.Count; i++)
            {
                if (config.colliders[i].radius <= ColliderAutoFit.MinRadius * 1.001f) collapsed++;
            }
            Check(collapsed == 0, "quality: no collider collapsed to minimum (collapsed=" + collapsed + ")");
        }

        private static void TestBonePathUniqueness()
        {
            var root = new GameObject("Avatar");
            var headA = MakeBone("Head", root.transform, 0f, 1.5f, 0f);
            var headB = MakeBone("Head", root.transform, 0.5f, 1.5f, 0f);

            var hairA = MakeBone("hair_01", headA.transform, 0f, 1.6f, 0.1f);
            var hairB = MakeBone("hair_01", headB.transform, 0.5f, 1.6f, 0.1f);

            var pathA = VrmPhysicsTuner.Editor.PhysicsConfigBuilder.HierarchyPath(hairA.transform);
            var pathB = VrmPhysicsTuner.Editor.PhysicsConfigBuilder.HierarchyPath(hairB.transform);

            Check(pathA != pathB, "bone_path disambiguates duplicate bone names");
            Check(pathA == "Avatar/Head[0]/hair_01", "bone_path disambiguates duplicate parents");
            Check(hairA.name == hairB.name, "duplicate names really are identical");

            var simpleRoot = new GameObject("Solo");
            var soloHead = MakeBone("Head", simpleRoot.transform, 0f, 1.5f, 0f);
            var soloHair = MakeBone("hair_01", soloHead.transform, 0f, 1.6f, 0.1f);
            var soloPath = VrmPhysicsTuner.Editor.PhysicsConfigBuilder.HierarchyPath(soloHair.transform);
            Check(soloPath == "Solo/Head/hair_01", "bone_path has no suffix when names are unique");

            var chains = VrmPhysicsTuner.Editor.AvatarScanner.Scan(root);
            var config = VrmPhysicsTuner.Editor.PhysicsConfigBuilder.Build(root, chains, new List<Transform>(), 0.1f);

            bool sawPath = false;
            for (int i = 0; i < config.chains.Count; i++)
            {
                if (config.chains[i].bone_paths.Count > 0) sawPath = true;
            }
            Check(sawPath, "config chains carry bone_paths");

            for (int i = 0; i < config.chains.Count; i++)
            {
                Check(config.chains[i].bone_paths.Count == config.chains[i].bones.Count,
                    "bone_paths aligned with bones (chain " + i + ")");
            }

            var json = VrmPhysicsConfigSerializer.Serialize(config);
            var back = VrmPhysicsConfigSerializer.Deserialize(json);
            Check(back.chains[0].bone_paths.Count == config.chains[0].bone_paths.Count,
                "bone_paths survive roundtrip");
            Check(back.chains[0].bone_paths[0] == config.chains[0].bone_paths[0],
                "bone_paths content survives roundtrip");
        }

        private static void TestHitRadiusAggregation()
        {
            var root = new GameObject("Avatar");
            var head = MakeBone("Head", root.transform, 0f, 1.5f, 0f);
            var h1 = MakeBone("hair_front_01", head.transform, 0f, 1.6f, 0.1f);
            var h2 = MakeBone("hair_front_02", h1.transform, 0f, 1.55f, 0.12f);
            var h3 = MakeBone("hair_front_03", h2.transform, 0f, 1.50f, 0.14f);

            var chains = VrmPhysicsTuner.Editor.AvatarScanner.Scan(root);
            var config = VrmPhysicsTuner.Editor.PhysicsConfigBuilder.Build(root, chains, new List<Transform>(), 0f);

            VrmSpringBoneChain hairChain = null;
            for (int i = 0; i < config.chains.Count; i++)
            {
                if (config.chains[i].name.StartsWith("hair_front")) hairChain = config.chains[i];
            }
            Check(hairChain != null, "hit radius: hair chain found");
            if (hairChain == null) return;

            float shortestSegment = float.MaxValue;
            for (int i = 0; i < hairChain.bones.Count; i++)
            {
                var t = FindByName(root.transform, hairChain.bones[i]);
                if (t == null || t.parent == null) continue;
                float d = Vector3.Distance(t.position, t.parent.position);
                if (d > 0f && d < shortestSegment) shortestSegment = d;
            }

            Check(hairChain.hit_radius <= shortestSegment,
                "hit radius uses thinnest segment (got " + hairChain.hit_radius + " vs " + shortestSegment + ")");
            Check(hairChain.hit_radius > 0f, "hit radius positive");

            var lastBone = FindByName(root.transform, hairChain.bones[hairChain.bones.Count - 1]);
            if (lastBone != null && lastBone.parent != null)
            {
                float lastSegment = Vector3.Distance(lastBone.position, lastBone.parent.position);
                float lastOnly = lastSegment * 0.25f;
                Check(hairChain.hit_radius <= lastOnly + 1e-6f || lastOnly >= shortestSegment,
                    "hit radius not just the last bone value");
            }
        }

        private static Transform FindByName(Transform node, string name)
        {
            if (node.name == name) return node;
            for (int i = 0; i < node.childCount; i++)
            {
                var found = FindByName(node.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void TestRotatedTransformTail()
        {
            var root = new GameObject("Avatar");
            var bone = MakeBone("Chest", root.transform, 0f, 1.0f, 0f);
            bone.transform.rotation = Quaternion.Euler(0f, 0f, (float)System.Math.PI / 2f);

            var child = new GameObject("Spine");
            child.transform.position = new Vector3(0.2f, 1.0f, 0f);
            child.transform.SetParent(bone.transform);

            var local = bone.transform.InverseTransformPoint(child.transform.position);
            CheckClose(local.x, 0f, 1e-3f, "rotated inverse: x collapses");
            CheckClose(local.y, -0.2f, 1e-3f, "rotated inverse: y carries offset");

            var roundTrip = bone.transform.TransformPoint(local);
            CheckClose(roundTrip.x, 0.2f, 1e-3f, "rotated roundtrip x");
            CheckClose(roundTrip.y, 1.0f, 1e-3f, "rotated roundtrip y");

            var scaled = MakeBone("ScaledBone", root.transform, 0f, 0f, 0f);
            scaled.transform.localScale = new Vector3(2f, 2f, 2f);
            var sLocal = scaled.transform.InverseTransformPoint(new Vector3(1f, 0f, 0f));
            CheckClose(sLocal.x, 0.5f, 1e-4f, "scale-aware inverse transform");
        }

        private static void TestAssistantPrompt()
        {
            var config = BuildSampleConfig();
            var system = AssistantPrompt.BuildSystemPrompt();
            Check(system.Contains("stiffness"), "system prompt lists fields");
            Check(system.Contains("JSON"), "system prompt demands JSON");

            var user = AssistantPrompt.BuildUserPrompt(config, "make hair softer");
            Check(user.Contains("sister1.0"), "user prompt carries model name");
            Check(user.Contains("make hair softer"), "user prompt carries intent");
            Check(user.Contains("hair_front"), "user prompt lists chains");
            Check(user.Contains("J_Bip_C_Head"), "user prompt lists colliders");
            Check(user.Contains("stiffness=0.85"), "user prompt carries chain values");

            var fallback = AssistantPrompt.BuildUserPrompt(config, "");
            Check(fallback.Contains("natural"), "empty intent gets a default goal");
        }

        private static void TestAssistantParser()
        {
            var body = "{\"summary\":\"softer hair\",\"suggestions\":[{\"chain\":\"hair_front\",\"bone\":\"\",\"field\":\"stiffness\",\"value\":0.6,\"reason\":\"too stiff\"}]}";
            var batch = AssistantResponseParser.Parse(body);
            Check(batch.ok, "parses plain json");
            Check(batch.summary == "softer hair", "parses summary");
            Check(batch.suggestions.Count == 1, "parses one suggestion");
            Check(batch.suggestions[0].field == "stiffness", "parses field");
            CheckClose(batch.suggestions[0].value, 0.6f, 1e-5f, "parses value");

            var fenced = "Here you go:\n```json\n{\"summary\":\"x\",\"suggestions\":[{\"chain\":\"a\",\"field\":\"drag_force\",\"value\":0.3}]}\n```\nHope that helps.";
            var fencedBatch = AssistantResponseParser.Parse(fenced);
            Check(fencedBatch.ok, "parses fenced json");
            Check(fencedBatch.suggestions.Count == 1, "fenced yields one suggestion");

            var noisy = "Sure! {\"summary\":\"y\",\"suggestions\":[]} done";
            Check(AssistantResponseParser.Parse(noisy).ok, "parses json embedded in prose");

            var bad = AssistantResponseParser.Parse("not json at all");
            Check(!bad.ok, "rejects non-json");
            Check(!string.IsNullOrEmpty(bad.error), "records parse error");

            Check(!AssistantResponseParser.Parse("").ok, "rejects empty body");
            Check(!AssistantResponseParser.Parse(null).ok, "rejects null body");

            var missingValue = "{\"suggestions\":[{\"chain\":\"a\",\"field\":\"stiffness\"}]}";
            Check(AssistantResponseParser.Parse(missingValue).suggestions.Count == 0, "drops suggestion without value");

            var missingField = "{\"suggestions\":[{\"chain\":\"a\",\"value\":0.5}]}";
            Check(AssistantResponseParser.Parse(missingField).suggestions.Count == 0, "drops suggestion without field");

            Check(AssistantResponseParser.Parse("{\"summary\":\"none\"}").ok, "tolerates missing suggestions array");
        }

        private static void TestSuggestionApplier()
        {
            var config = BuildSampleConfig();

            var batch = new SuggestionBatch();
            batch.ok = true;
            batch.suggestions.Add(new PhysicsSuggestion { chain = "hair_front", field = "stiffness", value = 0.42f });
            batch.suggestions.Add(new PhysicsSuggestion { chain = "hair_front", field = "drag_force", value = 0.11f });
            batch.suggestions.Add(new PhysicsSuggestion { chain = "hair_front", field = "gravity_power", value = 0.33f });
            batch.suggestions.Add(new PhysicsSuggestion { chain = "hair_front", field = "hit_radius", value = 0.05f });

            int applied = SuggestionApplier.Apply(config, batch, false);
            Check(applied == 4, "applies four chain suggestions");
            CheckClose(config.chains[0].stiffness, 0.42f, 1e-5f, "applied stiffness");
            CheckClose(config.chains[0].drag_force, 0.11f, 1e-5f, "applied drag_force");
            CheckClose(config.chains[0].gravity_power, 0.33f, 1e-5f, "applied gravity_power");
            CheckClose(config.chains[0].hit_radius, 0.05f, 1e-5f, "applied hit_radius");

            var radiusBatch = new SuggestionBatch();
            radiusBatch.ok = true;
            radiusBatch.suggestions.Add(new PhysicsSuggestion { bone = "J_Bip_C_Head", field = "radius", value = 0.077f });
            Check(SuggestionApplier.Apply(config, radiusBatch, false) == 1, "applies collider radius");
            CheckClose(config.colliders[0].radius, 0.077f, 1e-5f, "radius applied");

            var dryBatch = new SuggestionBatch();
            dryBatch.ok = true;
            dryBatch.suggestions.Add(new PhysicsSuggestion { chain = "hair_front", field = "stiffness", value = 0.99f });
            Check(SuggestionApplier.Apply(config, dryBatch, true) == 1, "dry run reports match");
            CheckClose(config.chains[0].stiffness, 0.42f, 1e-5f, "dry run does not mutate");
            CheckClose(dryBatch.suggestions[0].previous, 0.42f, 1e-5f, "dry run records previous");

            var clampBatch = new SuggestionBatch();
            clampBatch.ok = true;
            clampBatch.suggestions.Add(new PhysicsSuggestion { chain = "hair_front", field = "stiffness", value = 5f });
            clampBatch.suggestions.Add(new PhysicsSuggestion { bone = "J_Bip_C_Head", field = "radius", value = -1f });
            SuggestionApplier.Apply(config, clampBatch, false);
            CheckClose(config.chains[0].stiffness, 1f, 1e-5f, "clamps stiffness to 1");
            CheckClose(config.colliders[0].radius, ColliderAutoFit.MinRadius, 1e-6f, "clamps radius to min");

            var unknown = new SuggestionBatch();
            unknown.ok = true;
            unknown.suggestions.Add(new PhysicsSuggestion { chain = "nope", field = "stiffness", value = 0.5f });
            unknown.suggestions.Add(new PhysicsSuggestion { chain = "hair_front", field = "bogus_field", value = 0.5f });
            Check(SuggestionApplier.Apply(config, unknown, false) == 0, "ignores unknown targets and fields");

            Check(SuggestionApplier.Apply(config, null, false) == 0, "null batch is safe");
            var notOk = new SuggestionBatch();
            Check(SuggestionApplier.Apply(config, notOk, false) == 0, "failed batch is not applied");

            var byBone = new SuggestionBatch();
            byBone.ok = true;
            byBone.suggestions.Add(new PhysicsSuggestion { bone = "hair_front_02", field = "stiffness", value = 0.2f });
            Check(SuggestionApplier.Apply(config, byBone, false) == 1, "resolves chain by bone name");
            CheckClose(config.chains[0].stiffness, 0.2f, 1e-5f, "bone-targeted change applied");
        }

        private static void TestChatRequestBuilder()
        {
            var endpoint = new AssistantEndpoint { url = "https://example.invalid/v1/chat/completions", model = "some-model" };
            Check(endpoint.IsUsable(), "endpoint usable when url+model set");
            Check(!new AssistantEndpoint { url = "x" }.IsUsable(), "endpoint unusable without model");

            var json = ChatRequestBuilder.Build(endpoint, BuildSampleConfig(), "test");
            var root = JsonParser.Parse(json).AsObject();
            Check(root.Get("model").AsString("") == "some-model", "request carries model");
            var messages = root.Get("messages").AsArray();
            Check(messages.Count == 2, "request has system+user messages");
            Check(messages[0].AsObject().Get("role").AsString("") == "system", "first message is system");
            Check(messages[1].AsObject().Get("role").AsString("") == "user", "second message is user");
            Check(messages[1].AsObject().Get("content").AsString("").Contains("hair_front"), "user content has chains");
        }

        private static void TestChatResponseReader()
        {
            var ok = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"summary\\\":\\\"s\\\",\\\"suggestions\\\":[]}\"}}]}";
            var content = ChatResponseReader.ExtractContent(ok);
            Check(content.Contains("summary"), "extracts message content");

            var legacy = "{\"choices\":[{\"text\":\"plain\"}]}";
            Check(ChatResponseReader.ExtractContent(legacy) == "plain", "supports legacy text field");

            Check(ChatResponseReader.ExtractContent("") == "", "empty body yields empty");
            Check(ChatResponseReader.ExtractContent("{bad") == "", "malformed body yields empty");

            bool threw = false;
            try
            {
                ChatResponseReader.ExtractContent("{\"error\":{\"message\":\"rate limited\"}}");
            }
            catch (Exception e)
            {
                threw = true;
                Check(e.Message.Contains("rate limited"), "surfaces API error message");
            }
            Check(threw, "api error throws");
        }

        private static void CheckBoneLengths(PreviewSimulator sim, string label)
        {
            float worst = 0f;
            for (int c = 0; c < sim.Chains.Count; c++)
            {
                var chain = sim.Chains[c];
                for (int j = 1; j < chain.joints.Count; j++)
                {
                    float actual = Vec3.Distance(chain.joints[j].position, chain.joints[j - 1].position);
                    float expected = chain.joints[j].length;
                    float err = Math.Abs(actual - expected);
                    if (err > worst) worst = err;
                }
            }
            Check(worst < 1e-3f, label + ": bone lengths preserved (worst error " + worst + ")");
        }

        private static void TestPreviewSimulator()
        {
            var config = BuildSampleConfig();
            var positions = new Dictionary<string, Vec3>();
            positions["hair_front_01"] = new Vec3(0f, 1.6f, 0f);
            positions["hair_front_02"] = new Vec3(0f, 1.5f, 0f);

            var sim = new PreviewSimulator();
            sim.Load(config, name => positions.ContainsKey(name) ? positions[name] : new Vec3(0f, 0f, 0f));
            Check(sim.JointCount() == 2, "loads two joints");
            Check(sim.Chains.Count == 1, "loads one chain");

            sim.Reset();
            CheckClose(sim.MaxDisplacement(), 0f, 1e-6f, "reset puts joints at rest");

            var horizontal = new VrmPhysicsConfig();
            horizontal.chains.Add(new VrmSpringBoneChain
            {
                name = "h",
                bones = new List<string> { "a", "b" },
                stiffness = 0f,
                drag_force = 0f,
                gravity_power = 1f,
                gravity_dir = new float[] { 0f, -1f, 0f },
                hit_radius = 0.01f
            });
            var horizontalPos = new Dictionary<string, Vec3> { { "a", new Vec3(0f, 1f, 0f) }, { "b", new Vec3(0.2f, 1f, 0f) } };
            var hSim = new PreviewSimulator();
            hSim.Load(horizontal, n => horizontalPos[n]);
            hSim.Reset();

            var restTip = hSim.TipOf(0);

            for (int i = 0; i < 60; i++) hSim.Step(1f / 60f, 0f);

            var movedTip = hSim.TipOf(0);
            Check(Vec3.Distance(movedTip, restTip) > 0f, "tip moves away from rest after stepping");
            Check(hSim.MaxDisplacement() < 10f, "simulation stays bounded");

            for (int i = 0; i < 600; i++) hSim.Step(1f / 60f, 0f);
            Check(hSim.MaxDisplacement() < 10f, "long run stays bounded");
            Check(!float.IsNaN(hSim.TipOf(0).x), "no NaN after long run");

            hSim.Reset();
            CheckClose(hSim.MaxDisplacement(), 0f, 1e-6f, "reset after run clears drift");

            var stiff = new VrmPhysicsConfig();
            stiff.chains.Add(new VrmSpringBoneChain
            {
                name = "s",
                bones = new List<string> { "a", "b" },
                stiffness = 1f,
                drag_force = 0f,
                gravity_power = 0f,
                gravity_dir = new float[] { 0f, -1f, 0f },
                hit_radius = 0.01f
            });
            var stiffPos = new Dictionary<string, Vec3> { { "a", new Vec3(0f, 1f, 0f) }, { "b", new Vec3(0.2f, 1f, 0f) } };
            var stiffSim = new PreviewSimulator();
            stiffSim.Load(stiff, n => stiffPos[n]);
            stiffSim.Reset();
            for (int i = 0; i < 60; i++) stiffSim.Step(1f / 60f, 0f);
            Check(stiffSim.MaxDisplacement() < 1e-4f, "stiffness=1 keeps joint at rest");

            var loose = new VrmPhysicsConfig();
            loose.chains.Add(new VrmSpringBoneChain
            {
                name = "l",
                bones = new List<string> { "a", "b" },
                stiffness = 0f,
                drag_force = 0f,
                gravity_power = 1f,
                gravity_dir = new float[] { 0f, -1f, 0f },
                hit_radius = 0.01f
            });
            var looseSim = new PreviewSimulator();
            looseSim.Load(loose, n => stiffPos[n]);
            looseSim.Reset();
            for (int i = 0; i < 60; i++) looseSim.Step(1f / 60f, 0f);
            Check(looseSim.MaxDisplacement() > stiffSim.MaxDisplacement(), "loose chain drifts more than stiff one");

            var looseTip = looseSim.TipOf(0);
            Check(looseTip.y < 1f - 1e-3f, "gravity pulls a horizontal joint downward");

            var driftSim = new PreviewSimulator();
            driftSim.Load(loose, n => stiffPos[n]);
            driftSim.Reset();
            var driftRest = driftSim.TipOf(0);
            for (int i = 0; i < 600; i++) driftSim.Step(1f / 60f, 0f);
            Check(Vec3.Distance(driftSim.TipOf(0), driftRest) > 1e-3f, "tip moves away from rest under gravity");
            CheckBoneLengths(driftSim, "drift run");
            Check(!float.IsNaN(driftSim.TipOf(0).x), "no NaN after gravity run");

            var boundedSim = new PreviewSimulator();
            boundedSim.Load(loose, n => stiffPos[n]);
            boundedSim.Reset();
            for (int i = 0; i < 3000; i++) boundedSim.Step(1f / 60f, 0f);
            CheckBoneLengths(boundedSim, "long run");

            var clampedSim = new PreviewSimulator();
            clampedSim.Load(config, n => positions.ContainsKey(n) ? positions[n] : new Vec3(0f, 0f, 0f));
            clampedSim.Reset();
            for (int i = 0; i < 30; i++) clampedSim.Step(10f, i);
            Check(clampedSim.MaxDisplacement() < 100f, "huge delta is clamped");

            var empty = new PreviewSimulator();
            empty.Load(null, null);
            Check(empty.JointCount() == 0, "null config is safe");
            empty.Step(1f / 60f, 0f);
            Check(true, "stepping empty simulator does not throw");
        }
    }
}