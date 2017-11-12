using System;

namespace FLib
{
    public class Vec2m
    {
        public decimal x;
        public decimal y;
        public Vec2m(decimal x, decimal y) { this.x = x; this.y = y; }

        public static Vec2m operator +(Vec2m v1, Vec2m v2)
        {
            return new Vec2m(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vec2m operator *(decimal value, Vec2m v2)
        {
            return new Vec2m(value + v2.x, value + v2.y);
        }

        public static Vec2m operator *(Vec2m v, Mat2x2m m)
        {
            return new Vec2m(v.x * m.m11 + v.y * m.m21, v.x * m.m12 + v.y * m.m22);
        }
        public static decimal operator *(Vec2m v1, Vec2m v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        public Vec2m NormalSafe()
        {
            decimal lenSqr = x * x + y * y;
            if (lenSqr <= 1e-16m)
            {
                return new Vec2m(0, 0);
            }
            decimal len = (decimal)Math.Sqrt((double)lenSqr);
            return new Vec2m(x / len, y / len);
        }
        public static Vec2m operator /(Vec2m v, decimal value)
        {
            return new Vec2m(v.x / value, v.y / value);
        }
    }

    public class Vec3m
    {
        public decimal x;
        public decimal y;
        public decimal z;
        public Vec3m(decimal x, decimal y, decimal z) { this.x = x; this.y = y; this.z = z; }

        public static decimal DistanceSqr(Vec3m v1, Vec3m v2)
        {
            decimal dx = v1.x - v2.x;
            decimal dy = v1.y - v2.y;
            decimal dz = v1.z - v2.z;
            return dx * dx + dy * dy + dz * dz;
        }

        public static Vec3m operator +(Vec3m v1, Vec3m v2)
        {
            return new Vec3m(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vec3m operator *(decimal value, Vec3m v2)
        {
            return new Vec3m(value * v2.x, value * v2.y, value * v2.z);
        }

        public static Vec3m operator /(Vec3m v, decimal value)
        {
            return new Vec3m(v.x / value, v.y / value, v.z / value);
        }

    }

    public class Mat2x2m
    {
        // [m11, m12]
        // [m21, m22]

        public decimal m11, m12, m21, m22;

        public static Mat2x2m FromVecVec(Vec2m v1, Vec2m v2)
        {
            return new Mat2x2m(
                v1.x * v2.x,
                v1.x * v2.y,
                v1.y * v2.x,
                v1.y * v2.y);
        }

        public static Mat2x2m operator *(Mat2x2m mat1, decimal value)
        {
            return new Mat2x2m(mat1.m11 * value, mat1.m12 * value, mat1.m21 * value, mat1.m22 * value);
        }

        public static Mat2x2m operator *(decimal value, Mat2x2m mat1)
        {
            return mat1 * value;
        }

        public static Mat2x2m operator +(Mat2x2m mat1, Mat2x2m mat2)
        {
            return new Mat2x2m(
                mat1.m11 + mat2.m11,
                mat1.m12 + mat2.m12,
                mat1.m21 + mat2.m21,
                mat1.m22 + mat2.m22);
        }

        public static Mat2x2m operator /(Mat2x2m mat1, decimal value)
        {
            return new Mat2x2m(mat1.m11 / value, mat1.m12 / value, mat1.m21 / value, mat1.m22 / value);
        }

        public static Mat2x2m operator *(Mat2x2m mat1, Mat2x2m mat2)
        {
            return new Mat2x2m(
                mat1.m11 * mat2.m11 + mat1.m12 * mat2.m21,
                mat1.m11 * mat2.m12 + mat1.m12 * mat2.m22,
                mat1.m21 * mat2.m11 + mat1.m22 * mat2.m21,
                mat1.m21 * mat2.m12 + mat1.m22 * mat2.m22
            );
        }

        public Mat2x2m(decimal m11, decimal m12, decimal m21, decimal m22)
        {
            this.m11 = m11;
            this.m12 = m12;
            this.m21 = m21;
            this.m22 = m22;
        }

        public Mat2x2m Inverse()
        {
            const decimal epsilon = 1e-102m;
            decimal d = m11 * m22 - m12 * m21;
            if (Math.Abs(d) < epsilon)
            {
                return new Mat2x2m(0, 0, 0, 0);
            }
            decimal invd = 1.0m / d;
            return new Mat2x2m(m22 * invd, -m21 * invd, -m12 * invd, m11 * invd);
        }

        public void SVD(out Mat2x2m U, out Mat2x2m S, out Mat2x2m Vt)
        {
            // accoding to the web page:
            // http://www.lucidarme.me/?p=4624
            decimal a = m11;
            decimal b = m12;
            decimal c = m21;
            decimal d = m22;

            decimal v1 = 2 * a * c + 2 * b * d;
            decimal v2 = a * a + b * b - c * c - d * d;
            decimal theta = 0.5m * (decimal)Math.Atan2((double)v1, (double)v2);
            U = new Mat2x2m(
                (decimal)Math.Cos((double)theta),
                -(decimal)Math.Sin((double)theta),
                (decimal)Math.Sin((double)theta),
                (decimal)Math.Cos((double)theta));

            decimal S1 = a * a + b * b + c * c + d * d;
            decimal S2 = (decimal)Math.Sqrt((double)(v2 * v2 + v1 * v1));
            decimal s1 = (decimal)Math.Sqrt((double)((S1 + S2) * 0.5m));
            decimal s2 = (decimal)Math.Sqrt((double)((S1 - S2) * 0.5m));
            S = new Mat2x2m(s1, 0, 0, s2);

            decimal u1 = 2 * a * b + 2 * c * d;
            decimal u2 = a * a - b * b + c * c - d * d;
            decimal phi = 0.5m * (decimal)Math.Atan2((double)u1, (double)u2);
            decimal cp = (decimal)Math.Cos((double)phi);
            decimal sp = (decimal)Math.Sin((double)phi);
            decimal ct = (decimal)Math.Cos((double)theta);
            decimal st = (decimal)Math.Sin((double)theta);
            decimal s11 = (a * ct + c * st) * cp + (b * ct + d * st) * sp;
            decimal s22 = (a * st - c * ct) * sp + (-b * st + d * ct) * cp;
            decimal sign_s11 = Math.Sign(s11);
            decimal sign_s22 = Math.Sign(s22);
            Vt = new Mat2x2m(sign_s11 * cp, sign_s11 * sp, -sign_s22 * sp, sign_s22 * cp);
        }
    }
}