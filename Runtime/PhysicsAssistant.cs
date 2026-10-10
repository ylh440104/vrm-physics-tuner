using System;
using System.Collections.Generic;

namespace VrmPhysicsTuner
{
    public class PhysicsSuggestion
    {
        public string chain = "";
        public string bone = "";
        public string field = "";
        public float value;
        public float previous;
        public string reason = "";
    }

    public class SuggestionBatch
    {
        public List<PhysicsSuggestion> suggestions = new List<PhysicsSuggestion>();
        public string summary = "";
        public string raw = "";
        public bool ok;
        public string error = "";
    }

    public interface IPhysicsAssistant
    {
        SuggestionBatch RequestSuggestions(VrmPhysicsConfig config, string intent);
    }

    public static class AssistantPrompt
    {
        public static string BuildSystemPrompt()
        {
            return string.Join("\n", new[]
            {
                "You tune VRM spring bone physics. Reply with JSON only.",
                "Schema: {\"summary\": string, \"suggestions\": [{\"chain\": string, \"bone\": string, \"field\": string, \"value\": number, \"reason\": string}]}",
                "Allowed fields: stiffness, drag_force, gravity_power, hit_radius, radius.",
                "stiffness and drag_force are 0..1. gravity_power is 0..1. hit_radius and radius are world units, keep them positive.",
                "Use the chain name or a bone name to target a suggestion. Use an empty bone to apply to a whole chain.",
                "Keep suggestions small and explain each one briefly."
            });
        }

        public static string BuildUserPrompt(VrmPhysicsConfig config, string intent)
        {
            var lines = new List<string>();
            lines.Add("Model: " + (config != null ? config.model : ""));
            lines.Add("Goal: " + (string.IsNullOrEmpty(intent) ? "make the physics look natural" : intent));
            lines.Add("");
            lines.Add("Colliders:");

            if (config != null && config.colliders != null)
            {
                for (int i = 0; i < config.colliders.Count; i++)
                {
                    var c = config.colliders[i];
                    lines.Add("- bone=" + c.bone + " shape=" + c.shape + " radius=" + Format(c.radius));
                }
            }

            lines.Add("");
            lines.Add("Chains:");

            if (config != null && config.chains != null)
            {
                for (int i = 0; i < config.chains.Count; i++)
                {
                    var ch = config.chains[i];
                    lines.Add("- name=" + ch.name
                        + " bones=" + string.Join(",", ch.bones.ToArray())
                        + " stiffness=" + Format(ch.stiffness)
                        + " drag_force=" + Format(ch.drag_force)
                        + " gravity_power=" + Format(ch.gravity_power)
                        + " hit_radius=" + Format(ch.hit_radius));
                }
            }

            return string.Join("\n", lines.ToArray());
        }

        private static string Format(float value)
        {
            return value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public static class AssistantResponseParser
    {
        public static SuggestionBatch Parse(string body)
        {
            var batch = new SuggestionBatch();
            batch.raw = body ?? "";

            if (string.IsNullOrEmpty(body))
            {
                batch.error = "empty response";
                return batch;
            }

            try
            {
                var json = ExtractJson(body);
                var root = JsonParser.Parse(json).AsObject();
                batch.summary = root.Get("summary").AsString("");

                var list = root.Get("suggestions");
                if (list is List<object>)
                {
                    foreach (var item in list.AsArray())
                    {
                        var m = item.AsObject();
                        var s = new PhysicsSuggestion
                        {
                            chain = m.Get("chain").AsString(""),
                            bone = m.Get("bone").AsString(""),
                            field = m.Get("field").AsString(""),
                            value = m.Get("value").AsFloat(float.NaN),
                            reason = m.Get("reason").AsString("")
                        };
                        if (string.IsNullOrEmpty(s.field)) continue;
                        if (float.IsNaN(s.value)) continue;
                        batch.suggestions.Add(s);
                    }
                }

                batch.ok = true;
                return batch;
            }
            catch (Exception e)
            {
                batch.error = e.Message;
                return batch;
            }
        }

        private static string ExtractJson(string body)
        {
            var trimmed = body.Trim();

            if (trimmed.StartsWith("```"))
            {
                int firstNewline = trimmed.IndexOf('\n');
                if (firstNewline >= 0) trimmed = trimmed.Substring(firstNewline + 1);
                int fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (fence >= 0) trimmed = trimmed.Substring(0, fence);
                trimmed = trimmed.Trim();
            }

            int open = trimmed.IndexOf('{');
            int close = trimmed.LastIndexOf('}');
            if (open < 0 || close < open) throw new FormatException("no JSON object found");
            return trimmed.Substring(open, close - open + 1);
        }
    }

    public static class SuggestionApplier
    {
        public static int Apply(VrmPhysicsConfig config, SuggestionBatch batch, bool dryRun)
        {
            if (config == null || batch == null || !batch.ok) return 0;
            int applied = 0;

            for (int i = 0; i < batch.suggestions.Count; i++)
            {
                var s = batch.suggestions[i];
                if (ApplyOne(config, s, dryRun)) applied++;
            }

            return applied;
        }

        private static bool ApplyOne(VrmPhysicsConfig config, PhysicsSuggestion s, bool dryRun)
        {
            if (s.field == "radius")
            {
                var collider = FindCollider(config, s.bone);
                if (collider == null) return false;
                s.previous = collider.radius;
                if (!dryRun) collider.radius = ClampRadius(s.value);
                return true;
            }

            var chain = FindChain(config, s.chain, s.bone);
            if (chain == null) return false;

            switch (s.field)
            {
                case "stiffness":
                    s.previous = chain.stiffness;
                    if (!dryRun) chain.stiffness = Clamp01(s.value);
                    return true;
                case "drag_force":
                    s.previous = chain.drag_force;
                    if (!dryRun) chain.drag_force = Clamp01(s.value);
                    return true;
                case "gravity_power":
                    s.previous = chain.gravity_power;
                    if (!dryRun) chain.gravity_power = Clamp01(s.value);
                    return true;
                case "hit_radius":
                    s.previous = chain.hit_radius;
                    if (!dryRun) chain.hit_radius = ClampRadius(s.value);
                    return true;
                default:
                    return false;
            }
        }

        private static VrmSpringBoneCollider FindCollider(VrmPhysicsConfig config, string bone)
        {
            if (config.colliders == null) return null;
            for (int i = 0; i < config.colliders.Count; i++)
            {
                if (config.colliders[i].bone == bone) return config.colliders[i];
            }
            return null;
        }

        private static VrmSpringBoneChain FindChain(VrmPhysicsConfig config, string chainName, string bone)
        {
            if (config.chains == null) return null;

            if (!string.IsNullOrEmpty(chainName))
            {
                for (int i = 0; i < config.chains.Count; i++)
                {
                    if (config.chains[i].name == chainName) return config.chains[i];
                }
            }

            if (!string.IsNullOrEmpty(bone))
            {
                for (int i = 0; i < config.chains.Count; i++)
                {
                    var chain = config.chains[i];
                    if (chain.bones == null) continue;
                    for (int b = 0; b < chain.bones.Count; b++)
                    {
                        if (chain.bones[b] == bone) return chain;
                    }
                }
            }

            return null;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        private static float ClampRadius(float v)
        {
            if (v < ColliderAutoFit.MinRadius) return ColliderAutoFit.MinRadius;
            if (v > ColliderAutoFit.MaxRadius) return ColliderAutoFit.MaxRadius;
            return v;
        }
    }
}