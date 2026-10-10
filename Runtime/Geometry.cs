using System;

namespace Neko.VrmPhysicsTuner
{
    public struct Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vec3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Vec3 operator +(Vec3 a, Vec3 b) { return new Vec3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vec3 operator -(Vec3 a, Vec3 b) { return new Vec3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vec3 operator *(Vec3 a, float s) { return new Vec3(a.x * s, a.y * s, a.z * s); }
        public static Vec3 operator *(float s, Vec3 a) { return new Vec3(a.x * s, a.y * s, a.z * s); }
        public static Vec3 operator /(Vec3 a, float s) { return new Vec3(a.x / s, a.y / s, a.z / s); }

        public float Length() { return (float)Math.Sqrt(x * x + y * y + z * z); }

        public float LengthSq() { return x * x + y * y + z * z; }

        public Vec3 Normalized()
        {
            float len = Length();
            if (len < 1e-8f) return new Vec3(0f, 0f, 0f);
            return new Vec3(x / len, y / len, z / len);
        }

        public static float Dot(Vec3 a, Vec3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }

        public static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x);
        }

        public static float Distance(Vec3 a, Vec3 b) { return (a - b).Length(); }

        public float[] ToArray() { return new float[] { x, y, z }; }

        public static Vec3 FromArray(float[] a, Vec3 fallback)
        {
            if (a == null || a.Length < 3) return fallback;
            return new Vec3(a[0], a[1], a[2]);
        }

        public override string ToString()
        {
            return "(" + x.ToString("0.###") + ", " + y.ToString("0.###") + ", " + z.ToString("0.###") + ")";
        }
    }

    public static class ColliderAutoFit
    {
        public const float MinRadius = 0.001f;
        public const float MaxRadius = 1.0f;

        public static float FitSphereRadius(Vec3 center, Vec3[] samples, float padding)
        {
            if (samples == null || samples.Length == 0) return MinRadius;
            float maxSq = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float d = (samples[i] - center).LengthSq();
                if (d > maxSq) maxSq = d;
            }
            float radius = (float)Math.Sqrt(maxSq) * (1f + padding);
            return Clamp(radius);
        }

        public static float FitCapsuleRadius(Vec3 head, Vec3 tail, Vec3[] samples, float padding)
        {
            if (samples == null || samples.Length == 0) return MinRadius;
            float maxDist = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float d = DistanceToSegment(samples[i], head, tail);
                if (d > maxDist) maxDist = d;
            }
            float radius = maxDist * (1f + padding);
            return Clamp(radius);
        }

        public static float DistanceToSegment(Vec3 point, Vec3 a, Vec3 b)
        {
            Vec3 ab = b - a;
            float lenSq = ab.LengthSq();
            if (lenSq < 1e-10f) return Vec3.Distance(point, a);
            float t = Vec3.Dot(point - a, ab) / lenSq;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            Vec3 proj = a + ab * t;
            return Vec3.Distance(point, proj);
        }

        public static float Clamp(float radius)
        {
            if (radius < MinRadius) return MinRadius;
            if (radius > MaxRadius) return MaxRadius;
            return radius;
        }

        public static Vec3 Centroid(Vec3[] samples)
        {
            if (samples == null || samples.Length == 0) return new Vec3(0f, 0f, 0f);
            Vec3 sum = new Vec3(0f, 0f, 0f);
            for (int i = 0; i < samples.Length; i++) sum = sum + samples[i];
            return sum / samples.Length;
        }
    }
}