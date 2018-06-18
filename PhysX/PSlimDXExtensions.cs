using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PMath = StillDesign.PhysX.MathPrimitives;
using SDX = SlimDX;

namespace PhysX
{
    public static class PSlimDXExtensions
    {
        public static PMath.Vector2 ToPhx(this SDX.Vector2 v)
        {
            return new PMath.Vector2(v.X, v.Y);
        }

        public static PMath.Vector3 ToPhx(this SDX.Vector3 v)
        {
            return new PMath.Vector3(v.X, v.Y, v.Z);
        }

        public static PMath.Vector4 ToPhx(this SDX.Vector4 v)
        {
            return new PMath.Vector4(v.X, v.Y, v.Z, v.W);
        }
        
        public static PMath.Quaternion ToPhx(this SDX.Quaternion q)
        {
            return new PMath.Quaternion(q.X, q.Y, q.Z, q.W);
        }
        public static PMath.Plane ToPhx(this SDX.Plane p)
        {
            return new PMath.Plane(p.Normal.ToPhx(), p.D);
        }

        public static PMath.Matrix ToPhx(this SDX.Matrix M)
        {
            return new PMath.Matrix(M.M11, M.M12, M.M13, M.M14, M.M21, M.M22, M.M23, M.M24, M.M31, M.M32, M.M33, M.M34, M.M41, M.M42, M.M43, M.M44);
        }
            

    }
}
