using System;
using System.Collections.Generic;

namespace Neko.VrmPhysicsTuner
{
    [Serializable]
    public class VrmPhysicsConfig
    {
        public int version = 1;
        public string model = "";
        public string generated_by = "vrm-physics-tuner";
        public List<VrmSpringBoneCollider> colliders = new List<VrmSpringBoneCollider>();
        public List<VrmSpringBoneChain> chains = new List<VrmSpringBoneChain>();
    }

    [Serializable]
    public class VrmSpringBoneCollider
    {
        public string bone = "";
        public string bone_path = "";
        public string shape = "sphere";
        public float radius = 0.05f;
        public float[] offset = new float[] { 0f, 0f, 0f };
        public float[] tail = null;
    }

    [Serializable]
    public class VrmSpringBoneChain
    {
        public string name = "";
        public List<string> bones = new List<string>();
        public List<string> bone_paths = new List<string>();
        public float stiffness = 1f;
        public float drag_force = 0.4f;
        public float gravity_power = 0f;
        public float[] gravity_dir = new float[] { 0f, -1f, 0f };
        public float hit_radius = 0.02f;
        public float[] center = null;
        public List<string> collider_bones = new List<string>();
    }
}
