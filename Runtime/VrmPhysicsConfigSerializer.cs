using System;
using System.Collections.Generic;

namespace Neko.VrmPhysicsTuner
{
    public static class VrmPhysicsConfigSerializer
    {
        public static string Serialize(VrmPhysicsConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var w = new JsonWriter();
            w.BeginObject();
            w.Key("version").Value(config.version);
            w.Key("model").Value(config.model ?? "");
            w.Key("generated_by").Value(config.generated_by ?? "");

            w.Key("colliders").BeginArray();
            if (config.colliders != null)
            {
                for (int i = 0; i < config.colliders.Count; i++)
                {
                    var c = config.colliders[i];
                    w.BeginObject();
                    w.Key("bone").Value(c.bone ?? "");
                    w.Key("bone_path").Value(c.bone_path ?? "");
                    w.Key("shape").Value(c.shape ?? "sphere");
                    w.Key("radius").Value(c.radius);
                    WriteVector(w, "offset", c.offset);
                    if (c.tail == null) w.Key("tail").Null();
                    else WriteVector(w, "tail", c.tail);
                    w.EndObject();
                }
            }
            w.EndArray();

            w.Key("chains").BeginArray();
            if (config.chains != null)
            {
                for (int i = 0; i < config.chains.Count; i++)
                {
                    var ch = config.chains[i];
                    w.BeginObject();
                    w.Key("name").Value(ch.name ?? "");
                    w.Key("bones").BeginArray();
                    if (ch.bones != null)
                    {
                        for (int b = 0; b < ch.bones.Count; b++) w.Value(ch.bones[b]);
                    }
                    w.EndArray();
                    w.Key("bone_paths").BeginArray();
                    if (ch.bone_paths != null)
                    {
                        for (int b = 0; b < ch.bone_paths.Count; b++) w.Value(ch.bone_paths[b]);
                    }
                    w.EndArray();
                    w.Key("stiffness").Value(ch.stiffness);
                    w.Key("drag_force").Value(ch.drag_force);
                    w.Key("gravity_power").Value(ch.gravity_power);
                    WriteVector(w, "gravity_dir", ch.gravity_dir);
                    w.Key("hit_radius").Value(ch.hit_radius);
                    if (ch.center == null) w.Key("center").Null();
                    else WriteVector(w, "center", ch.center);
                    w.Key("collider_bones").BeginArray();
                    if (ch.collider_bones != null)
                    {
                        for (int b = 0; b < ch.collider_bones.Count; b++) w.Value(ch.collider_bones[b]);
                    }
                    w.EndArray();
                    w.EndObject();
                }
            }
            w.EndArray();

            w.EndObject();
            return w.ToString();
        }

        private static void WriteVector(JsonWriter w, string key, float[] v)
        {
            w.Key(key).BeginArray();
            if (v == null)
            {
                w.Value(0f).Value(0f).Value(0f);
            }
            else
            {
                for (int i = 0; i < v.Length; i++) w.Value(v[i]);
            }
            w.EndArray();
        }

        public static VrmPhysicsConfig Deserialize(string json)
        {
            var root = JsonParser.Parse(json).AsObject();
            var config = new VrmPhysicsConfig();
            config.version = root.Get("version").AsInt(1);
            config.model = root.Get("model").AsString("");
            config.generated_by = root.Get("generated_by").AsString("");

            var collidersNode = root.Get("colliders");
            if (collidersNode is List<object>)
            {
                foreach (var item in collidersNode.AsArray())
                {
                    var m = item.AsObject();
                    config.colliders.Add(new VrmSpringBoneCollider
                    {
                        bone = m.Get("bone").AsString(""),
                        bone_path = m.Get("bone_path").AsString(""),
                        shape = m.Get("shape").AsString("sphere"),
                        radius = m.Get("radius").AsFloat(0.05f),
                        offset = m.Get("offset").AsFloatArray() ?? new float[] { 0f, 0f, 0f },
                        tail = m.Get("tail").AsFloatArray()
                    });
                }
            }

            var chainsNode = root.Get("chains");
            if (chainsNode is List<object>)
            {
                foreach (var item in chainsNode.AsArray())
                {
                    var m = item.AsObject();
                    var chain = new VrmSpringBoneChain
                    {
                        name = m.Get("name").AsString(""),
                        bones = m.Get("bones").AsStringList(),
                        bone_paths = m.Get("bone_paths").AsStringList(),
                        stiffness = m.Get("stiffness").AsFloat(1f),
                        drag_force = m.Get("drag_force").AsFloat(0.4f),
                        gravity_power = m.Get("gravity_power").AsFloat(0f),
                        gravity_dir = m.Get("gravity_dir").AsFloatArray() ?? new float[] { 0f, -1f, 0f },
                        hit_radius = m.Get("hit_radius").AsFloat(0.02f),
                        center = m.Get("center").AsFloatArray(),
                        collider_bones = m.Get("collider_bones").AsStringList()
                    };
                    config.chains.Add(chain);
                }
            }

            return config;
        }
    }
}