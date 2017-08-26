//#define ENABLE_MULTITHREADING

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace downscaling_winform
{
    public partial class Form1 : Form
    {
        // Content-Adaptive Downscaling [Koph, et al., SIGGRAPH Asia 2013]
        class ContentAdaptiveDownscale
        {
            class Vec2
            {
                public double x;
                public double y;
                public Vec2(double x, double y) { this.x = x; this.y = y; }
                public static Vec2 operator *(Vec2 v, Mat2x2 m)
                {
                    return new Vec2(v.x * m.m11 + v.y * m.m21, v.x * m.m12 + v.y * m.m22);
                }
                public static double operator *(Vec2 v1, Vec2 v2)
                {
                    return v1.x * v2.x + v1.y * v2.y;
                }

                public Vec2 NormalSafe()
                {
                    double lenSqr = x * x + y * y;
                    if (lenSqr <= 1e-16)
                    {
                        return new Vec2(0, 0);
                    }
                    double len = Math.Sqrt(lenSqr);
                    return new Vec2(x / len, y / len);
                }
            }
            class Vec3
            {
                public double x;
                public double y;
                public double z;
                public Vec3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
                public static double DistanceSqr(Vec3 v1, Vec3 v2)
                {
                    double dx = v1.x - v2.x;
                    double dy = v1.y - v2.y;
                    double dz = v1.z - v2.z;
                    return dx * dx + dy * dy + dz * dz;
                }
            }
            class Mat2x2
            {
                // [m11, m12]
                // [m21, m22]

                public double m11, m12, m21, m22;

                public static Mat2x2 operator *(Mat2x2 mat1, Mat2x2 mat2)
                {
                    return new Mat2x2(
                        mat1.m11 * mat2.m11 + mat1.m12 * mat2.m21,
                        mat1.m11 * mat2.m12 + mat1.m12 * mat2.m22,
                        mat1.m21 * mat2.m11 + mat1.m22 * mat2.m21,
                        mat1.m21 * mat2.m12 + mat1.m22 * mat2.m22
                    );
                }

                public Mat2x2(double m11, double m12, double m21, double m22)
                {
                    this.m11 = m11;
                    this.m12 = m12;
                    this.m21 = m21;
                    this.m22 = m22;
                }
                public Mat2x2 Inverse()
                {
                    double d = m11 * m22 - m12 * m21;
                    if (Math.Abs(d) <= 1e-8)
                    {
                        return new Mat2x2(0, 0, 0, 0);
                    }
                    double invd = 1.0 / d;
                    return new Mat2x2(m22 * invd, -m21 * invd, -m12 * invd, m11 * invd);
                }

                public void SVD(out Mat2x2 U, out Mat2x2 S, out Mat2x2 Vt)
                {
                    // accoding to the web page:
                    // http://www.lucidarme.me/?p=4624
                    double a = m11;
                    double b = m12;
                    double c = m21;
                    double d = m22;

                    double v1 = 2 * a * c + 2 * b * d;
                    double v2 = a * a + b * b - c * c - d * d;
                    double theta = 0.5 * Math.Atan2(v1, v2);
                    U = new Mat2x2(Math.Cos(theta), -Math.Sin(theta), Math.Sin(theta), Math.Cos(theta));

                    double S1 = a * a + b * b + c * c + d * d;
                    double S2 = Math.Sqrt(v2 * v2 + v1 * v1);
                    double s1 = Math.Sqrt((S1 + S2) * 0.5);
                    double s2 = Math.Sqrt((S1 - S2) * 0.5);
                    S = new Mat2x2(s1, 0, 0, s2);

                    double u1 = 2 * a * b + 2 * c * d;
                    double u2 = a * a - b * b + c * c - d * d;
                    double phi = 0.5 * Math.Atan2(u1, u2);
                    double cp = Math.Cos(phi);
                    double sp = Math.Sin(phi);
                    double ct = Math.Cos(theta);
                    double st = Math.Sin(theta);
                    double s11 = (a * ct + c * st) * cp + (b * ct + d * st) * sp;
                    double s22 = (a * st - c * ct) * sp + (-b * st + d * ct) * cp;
                    double sign_s11 = Math.Sign(s11);
                    double sign_s22 = Math.Sign(s22);
                    System.Diagnostics.Debug.Assert(sign_s11 == 1.0 || sign_s11 == -1.0);
                    Vt = new Mat2x2(sign_s11 * cp, sign_s11 * sp, -sign_s22 * sp, sign_s22 * cp);
                }
            }

            int wi, hi;
            int wo, ho;
            double rx, ry;

            Vec2[] m;
            Mat2x2[] S;
            Vec3[] v;
            double[] s;
            Vec3[] c; // CIELAB, [0, 1]
            double[] w;
            double[] g;
            int Rsize = 0;

            unsafe Bitmap kernel2bmp(bool normal)
            {
                var a = normal ? g : w;

                var bmp = new Bitmap(wi, hi, PixelFormat.Format32bppArgb);
                using (var it = new FLib.BitmapIterator(bmp, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
                {
                    byte* data = it.Data;
                    for (int ky = 0; ky < ho; ky++)
                    {
                        for (int kx = 0; kx < wo; kx++)
                        {
                            int k = kx + wo * ky;
                            double cx = rx * (kx + 0.5);
                            double cy = ry * (ky + 0.5);
                            int rx0 = Math.Max((int)(cx - 2 * rx), 0);
                            int rx1 = Math.Min((int)(cx + 2 * rx), wi - 1);
                            int ry0 = Math.Max((int)(cy - 2 * ry), 0);
                            int ry1 = Math.Min((int)(cy + 2 * ry), hi - 1);
                            double max = double.MinValue;
                            double min = double.MaxValue;
                            for (int iy = ry0; iy <= ry1; iy++)
                            {
                                for (int ix = rx0; ix <= rx1; ix++)
                                {
                                    System.Diagnostics.Debug.Assert(kidx(kx, ky, ix, iy) >= 0);

                                    double d = a[kidx(kx, ky, ix, iy)];
                                    max = Math.Max(max, Math.Log10(d));
                                    min = Math.Min(min, Math.Log10(d));
                                    if (double.IsNegativeInfinity(min))
                                    {
                                        min = double.MinValue;
                                    }
                                }
                            }
                            if (max == min)
                            {
                                max = min + 1.0;
                            }
                            for (int iy = ry0; iy <= ry1; iy++)
                            {
                                for (int ix = rx0; ix <= rx1; ix++)
                                {
                                    int ox = (int)(m[k].x - 2 * rx) + ix - (int)(cx - 2 * rx);
                                    int oy = (int)(m[k].y - 2 * ry) + iy - (int)(cy - 2 * ry);

                                    double d = a[kidx(kx, ky, ix, iy)];
                                    double n = (Math.Log10(d) - min) / (max - min);
                                    byte value = (byte)(n * 255.0);
                                    if ((kx + ky) % 2 == 0)
                                    {
                                        data[4 * ox + oy * it.Stride + 0] = value;
                                        data[4 * ox + oy * it.Stride + 1] = 0;
                                        data[4 * ox + oy * it.Stride + 2] = value;
                                        data[4 * ox + oy * it.Stride + 3] = 255;
                                    }
                                    else
                                    {
                                        data[4 * ox + oy * it.Stride + 0] = 0;
                                        data[4 * ox + oy * it.Stride + 1] = value;
                                        data[4 * ox + oy * it.Stride + 2] = 0;
                                        data[4 * ox + oy * it.Stride + 3] = 255;
                                    }
                                }
                            }
                        }
                    }
                }
                return bmp;
            }

            unsafe Bitmap filter(Bitmap input)
            {
                var a = g;

                var bmp = new Bitmap(wo, ho, PixelFormat.Format32bppArgb);
                using (var it_in = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
                using (var it = new FLib.BitmapIterator(bmp, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
                {
                    byte* data_in = it_in.Data;
                    byte* data = it.Data;
                    for (int ky = 0; ky < ho; ky++)
                    {
                        for (int kx = 0; kx < wo; kx++)
                        {
                            int k = kx + wo * ky;
                            double cx = rx * (kx + 0.5);
                            double cy = ry * (ky + 0.5);
                            int rx0 = Math.Max((int)(cx - 2 * rx), 0);
                            int rx1 = Math.Min((int)(cx + 2 * rx), wi - 1);
                            int ry0 = Math.Max((int)(cy - 2 * ry), 0);
                            int ry1 = Math.Min((int)(cy + 2 * ry), hi - 1);
                            double r = 0.0;
                            double g = 0.0;
                            double b = 0.0;
                            double sum = 0;
                            for (int iy = ry0; iy <= ry1; iy++)
                            {
                                for (int ix = rx0; ix <= rx1; ix++)
                                {
                                    int ox = (int)(m[k].x - 2 * rx) + ix - (int)(cx - 2 * rx);
                                    int oy = (int)(m[k].y - 2 * ry) + iy - (int)(cy - 2 * ry);


                                    double d = a[kidx(kx, ky, ix, iy)];
                                    sum += d;
                                    r += d * (data_in[4 * ox + oy * it_in.Stride + 2] / 255.0);
                                    g += d * (data_in[4 * ox + oy * it_in.Stride + 1] / 255.0);
                                    b += d * (data_in[4 * ox + oy * it_in.Stride + 0] / 255.0);
                                }
                            }
                            r /= sum;
                            g /= sum;
                            b /= sum;

                            data[4 * kx + ky * it.Stride + 2] = (byte)(255 * r);
                            data[4 * kx + ky * it.Stride + 1] = (byte)(255 * g);
                            data[4 * kx + ky * it.Stride + 0] = (byte)(255 * b);
                            data[4 * kx + ky * it.Stride + 3] = 255;
                        }
                    }
                }
                return bmp;
            }

            unsafe void saveKernel(Bitmap input)
            {
                string saveDir = "./kernels";

                try
                {
                    using (var bmp = kernel2bmp(false)) // w
                    {
                        string path = System.IO.Path.GetFullPath(saveDir + "/w-" + iteration + ".png");
                        bmp.Save(path);
                    }

                    using (var bmp = kernel2bmp(true)) // w
                    {
                        string path = System.IO.Path.GetFullPath(saveDir + "/g-" + iteration + ".png");
                        bmp.Save(path);
                    }
                    using (var bmp = filter(input)) // w
                    {
                        string path = System.IO.Path.GetFullPath(saveDir + "/output-" + iteration + ".png");
                        bmp.Save(path);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }


            System.Diagnostics.Stopwatch stopwatch = null;
            int stopwatchCnt = 0;

            void printElapsedTime(string prefix = "")
            {
                Console.WriteLine($"[{stopwatchCnt}]{prefix}{stopwatch.ElapsedMilliseconds} ms.");
                stopwatchCnt++;
            }

            unsafe void initialize(Bitmap input, Bitmap output)
            {
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                stopwatchCnt = 0;
                
                wi = input.Width;
                hi = input.Height;
                wo = output.Width;
                ho = output.Height;
                rx = (double)wi / wo;
                ry = (double)hi / ho;
                m = new Vec2[wo * ho];
                S = new Mat2x2[wo * ho];
                v = new Vec3[wo * ho];
                s = new double[wo * ho];
                int k = 0;
                for (int ky = 0; ky < ho; ky++)
                {
                    for (int kx = 0; kx < wo; kx++)
                    {
                        m[k] = new Vec2(rx * (kx + 0.5), ry * (ky + 0.5));
                        S[k] = new Mat2x2(rx / 3, 0, 0, ry / 3);
                        v[k] = new Vec3(0.5, 0.5, 0.5);
                        s[k] = 1e-4 * wi; // * wiしないと小さくなりすぎる？
                        k++;
                    }
                }
                

                c = new Vec3[wi * hi];
                using (var i_it = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
                {
                    byte* i_data = (byte*)i_it.PixelData;
                    for (int iy = 0; iy < input.Height; iy++)
                    {
                        for (int ix = 0; ix < input.Width; ix++)
                        {
                            int i = ix + iy * input.Width;
                            int i_idx = 4 * ix + iy * i_it.Stride;
                            double r = i_data[i_idx + 0] / 255.0;
                            double g = i_data[i_idx + 1] / 255.0;
                            double b = i_data[i_idx + 2] / 255.0;

                            // TODO: convert to CIELAB
                            c[i] = new Vec3(r, g, b);
                        }
                    }
                }
                

                // NOTE: (wo * ho) * (wi * hi) happened out of memory error.
                // We should save only the region Rk for each kernel k.
                Rsize = (int)(4 * rx + 1) * (int)(4 * ry + 1);
                w = new double[Rsize * wo * ho];
                g = new double[Rsize * wo * ho];


                for (int ky = 0; ky < ho; ky++)
                {
                    for (int kx = 0; kx < wo; kx++)
                    {
                        double cx = m[kx + ky * wo].x;
                        double cy = m[kx + ky * wo].y;
                        int rx0 = Math.Max((int)(cx - 2 * rx), 0);
                        int rx1 = Math.Min((int)(cx + 2 * rx), wi - 1);
                        int ry0 = Math.Max((int)(cy - 2 * ry), 0);
                        int ry1 = Math.Min((int)(cy + 2 * ry), hi - 1);
                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                w[kidx(kx, ky, ix, iy)] = 1.0 / Rsize;
                                g[kidx(kx, ky, ix, iy)] = 0.5 / Rsize;
                            }
                        }
                    }
                }




                Console.WriteLine("[Content-Adaptive] initialize()");
                
            }

            // get index with the value is wk[i] (or gk[i] in pseude code.
            int kidx(int kx, int ky, int ix, int iy)
            {
                double cx = (kx + 0.5) * rx;
                double cy = (ky + 0.5) * ry;
                int rx0 = Math.Max((int)(cx - 2 * rx), 0);
                int rx1 = Math.Min((int)(cx + 2 * rx), wi - 1);
                int ry0 = Math.Max((int)(cy - 2 * ry), 0);
                int ry1 = Math.Min((int)(cy + 2 * ry), hi - 1);
                int x = ix - rx0;
                int y = iy - ry0;
                if (0 <= x && x < 4 * rx + 1 && 0 <= y && y < 4 * ry + 1)
                {
                    int offset = (kx + ky * wo) * Rsize;
                    int idx = x + y * (rx1 - rx0 + 1) + offset;
                    return idx;
                }
                return -1;
            }

            void EStep()
            {
                int kNum = wo * ho;

                var i2k = new List<int>[wi * hi];
                for (int i = 0; i < i2k.Length; i++)
                {
                    i2k[i] = new List<int>();
                }
                
                // compute all kernels
#if ENABLE_MULTITHREADING
                System.Threading.Tasks.Parallel.For(0, ho, (ky) =>
#else
                for (int ky = 0; ky < ho; ky++)
#endif
                {
                    for (int kx = 0; kx < wo; kx++)
                    {
                        int k = kx + wo * ky;
                        double cx = (kx + 0.5) * rx;
                        double cy = (ky + 0.5) * ry;
                        int rx0 = Math.Max((int)(cx - 2 * rx), 0);
                        int rx1 = Math.Min((int)(cx + 2 * rx), wi - 1);
                        int ry0 = Math.Max((int)(cy - 2 * ry), 0);
                        int ry1 = Math.Min((int)(cy + 2 * ry), hi - 1);
                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                int i = ix + iy * wi;
                                var pi_uk = new Vec2(ix - m[k].x, iy - m[k].y);
                                var Skinv = S[k].Inverse();
                                double d = -0.5 * (pi_uk * Skinv * pi_uk) - Vec3.DistanceSqr(c[i], v[k]) / (2 * s[k] * s[k]);

                                // 微妙？
                                d = Math.Min(d, 700);

                                w[kidx(kx, ky, ix, iy)] = Math.Exp(d);

                                // save i -> k
                                i2k[i].Add(kx);
                                i2k[i].Add(ky);
                            }
                        }
                        double wsum = 0.0;
                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                int i = ix + iy * wi;
                                wsum += w[kidx(kx, ky, ix, iy)];
                            }
                        }
                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                int i = ix + iy * wi;
                                w[kidx(kx, ky, ix, iy)] /= wsum;
                            }
                        }
                    }
                }
#if ENABLE_MULTITHREADING
                );
#endif

                // Normalize per pixel
#if ENABLE_MULTITHREADING
                System.Threading.Tasks.Parallel.For(0, hi, (iy) =>
#else
                for (int iy = 0; iy < hi; iy++)
#endif
                {
                    for (int ix = 0; ix < wi; ix++)
                    {
                        int i = ix + iy * wi;
                        double wsum = 0.0;
                        var i2ki = i2k[i];
                        for (int j = 0; j < i2ki.Count; j += 2)
                        {
                            int kx = i2ki[j + 0];
                            int ky = i2ki[j + 1];
                            wsum += w[kidx(kx, ky, ix, iy)]; // bottle neck!
                        }
                        if (wsum == 0)
                        {
                            for (int j = 0; j < i2k[i].Count; j += 2)
                            {
                                int kx = i2ki[j + 0];
                                int ky = i2ki[j + 1];
                                var ki = kidx(kx, ky, ix, iy);
                                g[ki] = 0; // bottle neck!
                            }
                        }
                        else
                        {
                            for (int j = 0; j < i2k[i].Count; j += 2)
                            {
                                int kx = i2ki[j + 0];
                                int ky = i2ki[j + 1];
                                var ki = kidx(kx, ky, ix, iy);
                                g[ki] = w[ki] / wsum; // bottle neck!
                            }
                        }
                    }
                }
#if ENABLE_MULTITHREADING
                );
#endif
            }

            /// <returns>Has changed in M-Step</returns>
            bool MStep()
            {
                double diff = 0.0;

                // compute all kernels
#if ENABLE_MULTITHREADING
                System.Threading.Tasks.Parallel.For(0, ho, (ky) =>
#else
                for (int ky = 0; ky < ho; ky++)
#endif
                {
                    for (int kx = 0; kx < wo; kx++)
                    {
                        int k = kx + wo * ky;
                        double cx = (kx + 0.5) * rx;
                        double cy = (ky + 0.5) * ry;
                        int rx0 = Math.Max((int)(cx - 2 * rx), 0);
                        int rx1 = Math.Min((int)(cx + 2 * rx), wi - 1);
                        int ry0 = Math.Max((int)(cy - 2 * ry), 0);
                        int ry1 = Math.Min((int)(cy + 2 * ry), hi - 1);
                        double wsum = 0.0;
                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                int i = ix + iy * wi;
                                wsum += g[kidx(kx, ky, ix, iy)];
                            }
                        }

                        var mk = m[k];
                        S[k] = new Mat2x2(0, 0, 0, 0);
                        m[k] = new Vec2(0, 0);
                        v[k] = new Vec3(0, 0, 0);
                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                double gki = g[kidx(kx, ky, ix, iy)];
                                int i = ix + iy * wi;
                                double dx = ix - mk.x;
                                double dy = iy - mk.y;
                                S[k].m11 += gki * dx * dx;
                                S[k].m12 += gki * dx * dy;
                                S[k].m21 += gki * dy * dx;
                                S[k].m22 += gki * dy * dy;
                            }
                        }
                        S[k].m11 /= wsum;
                        S[k].m12 /= wsum;
                        S[k].m21 /= wsum;
                        S[k].m22 /= wsum;

                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                double gki = g[kidx(kx, ky, ix, iy)];
                                int i = ix + iy * wi;
                                m[k].x += gki * ix;
                                m[k].y += gki * iy;
                            }
                        }
                        m[k].x /= wsum;
                        m[k].y /= wsum;


                        var prevX = v[k].x;
                        var prevY = v[k].y;
                        var prevZ = v[k].z;
                        for (int iy = ry0; iy <= ry1; iy++)
                        {
                            for (int ix = rx0; ix <= rx1; ix++)
                            {
                                double gki = g[kidx(kx, ky, ix, iy)];
                                int i = ix + iy * wi;
                                v[k].x += gki * c[i].x;
                                v[k].y += gki * c[i].y;
                                v[k].z += gki * c[i].z;
                            }
                        }
                        v[k].x /= wsum;
                        v[k].y /= wsum;
                        v[k].z /= wsum;

                        double diffx = prevX - v[k].x;
                        double diffy = prevY - v[k].y;
                        double diffz = prevZ - v[k].z;
                        diff += diffx * diffx + diffy * diffy + diffz * diffz;

                        System.Diagnostics.Debug.Assert(double.IsNaN(diff) == false);
                    }
                }
#if ENABLE_MULTITHREADING
                );
#endif

                diff /= wo * ho;
                Console.WriteLine($"diff = {diff} (iter = {iteration})");

                return diff >= 0.05;
            }

            double clamp(double v, double min, double max)
            {
                return v < min ? min : v > max ? max : v;
            }

            /// <returns>Has changed in C-Step</returns>
            bool CStep()
            {
                try
                {
                    // Spatial constraints
                    var mAve = new Vec2[wo * ho];
                    for (int ky = 0; ky < ho; ky++)
                    {
                        for (int kx = 0; kx < wo; kx++)
                        {
                            int k = kx + ky * wo;
                            mAve[k] = new Vec2(0, 0);
                            int cnt = 0;
                            if (0 <= kx - 1 && 0 <= ky - 1)
                            {
                                mAve[k].x += m[(kx - 1) + (ky - 1) * wo].x;
                                mAve[k].y += m[(kx - 1) + (ky - 1) * wo].y;
                                cnt++;
                            }
                            if (0 <= kx - 1 && ky + 1 < ho)
                            {
                                mAve[k].x += m[(kx - 1) + (ky + 1) * wo].x;
                                mAve[k].y += m[(kx - 1) + (ky + 1) * wo].y;
                                cnt++;
                            }
                            if (kx + 1 < wo && 0 <= ky - 1)
                            {
                                mAve[k].x += m[(kx + 1) + (ky - 1) * wo].x;
                                mAve[k].y += m[(kx + 1) + (ky - 1) * wo].y;
                                cnt++;
                            }
                            if (kx + 1 < wo && ky + 1 < ho)
                            {
                                mAve[k].x += m[(kx + 1) + (ky + 1) * wo].x;
                                mAve[k].y += m[(kx + 1) + (ky + 1) * wo].y;
                                cnt++;
                            }
                            mAve[k].x /= cnt;
                            mAve[k].y /= cnt;
                        }
                    }
                    for (int ky = 0; ky < ho; ky++)
                    {
                        for (int kx = 0; kx < wo; kx++)
                        {
                            int k = kx + ky * wo;
                            m[k].x = 0.5 * (m[k].x + mAve[k].x);
                            m[k].y = 0.5 * (m[k].y + mAve[k].y);
                            double x0 = rx * (kx + 0.5) - rx / 4;
                            double x1 = rx * (kx + 0.5) + rx / 4;
                            double y0 = ry * (ky + 0.5) - ry / 4;
                            double y1 = ry * (ky + 0.5) + ry / 4;
                            m[k].x = clamp(m[k].x, x0, x1);
                            m[k].y = clamp(m[k].y, y0, y1);
                        }
                    }

                    // Constrain spatial variance
                    for (int k = 0; k < ho * wo; k++)
                    {
                        Mat2x2 _U, _S, _Vt;
                        S[k].SVD(out _U, out _S, out _Vt);
                        _S.m11 = clamp(_S.m11, 0.05, 0.1);
                        _S.m22 = clamp(_S.m22, 0.05, 0.1);
                        S[k] = _U * _S * _Vt;

                        System.Diagnostics.Debug.Assert(double.IsNaN(S[k].m11) == false);
                    }

                    // Shape constraints

                    for (int ky = 0; ky < ho; ky++)
                    {
                        for (int kx = 0; kx < wo; kx++)
                        {
                            double cx = (kx + 0.5) * rx;
                            double cy = (ky + 0.5) * ry;
                            int rx0 = Math.Max((int)(cx - 2 * rx), 0);
                            int rx1 = Math.Min((int)(cx + 2 * rx), wi - 1);
                            int ry0 = Math.Max((int)(cy - 2 * ry), 0);
                            int ry1 = Math.Min((int)(cy + 2 * ry), hi - 1);

                            int k = kx + ky * wo;

                            for (int ny = ky - 1; ny <= ky + 1; ny++)
                            {
                                for (int nx = kx - 1; nx <= kx + 1; nx++)
                                {
                                    if (nx == kx && ny == ky)
                                    {
                                        continue;
                                    }
                                    if (nx < 0 || wo <= nx || ny < 0 || ho <= ny)
                                    {
                                        continue;
                                    }

                                    int n = nx + ny * wo;

                                    var d = new Vec2(nx - kx, ny - ky);

                                    double _s = 0;
                                    for (int iy = ry0; iy <= ry1; iy++)
                                    {
                                        for (int ix = rx0; ix <= rx1; ix++)
                                        {
                                            int i = ix + iy * wi;

                                            double dpmx = ix - m[k].x;
                                            double dpmy = iy - m[k].y;
                                            double val = Math.Max(0, dpmx * d.x + dpmy * d.y);
                                            _s += g[kidx(kx, ky, ix, iy)] * val * val;
                                        }
                                    }

                                    double _f = 0.0;
                                    for (int iy = ry0; iy <= ry1; iy++)
                                    {
                                        for (int ix = rx0; ix <= rx1; ix++)
                                        {
                                            int i = ix + iy * wi;

                                            double dpmx = ix - m[k].x;
                                            double dpmy = iy - m[k].y;
                                            double val = Math.Max(0, dpmx * d.x + dpmy * d.y);
                                            int nidx = kidx(nx, ny, ix, iy);
                                            _f += g[kidx(kx, ky, ix, iy)] * (nidx < 0 ? 0.0 : g[nidx]);
                                        }
                                    }

                                    var o = new Vec2(0, 0);
                                    for (int iy = ry0 + 1; iy <= ry1; iy++)
                                    {
                                        for (int ix = rx0 + 1; ix <= rx1; ix++)
                                        {
                                            int nidx01 = kidx(nx, ny, ix - 1, iy);
                                            double gndix01 = nidx01 < 0 ? 0.0 : g[nidx01];
                                            int nidx10 = kidx(nx, ny, ix, iy - 1);
                                            double gndix10 = nidx10 < 0 ? 0.0 : g[nidx10];
                                            int nidx11 = kidx(nx, ny, ix, iy);
                                            double gndix11 = nidx11 < 0 ? 0.0 : g[nidx11];
                                            double val01 = g[kidx(kx, ky, ix - 1, iy)] / (g[kidx(kx, ky, ix - 1, iy)] + gndix01);
                                            double val10 = g[kidx(kx, ky, ix, iy - 1)] / (g[kidx(kx, ky, ix, iy - 1)] + gndix10);
                                            double val11 = g[kidx(kx, ky, ix, iy)] / (g[kidx(kx, ky, ix, iy)] + gndix11);
                                            o.x += val11 - val01;
                                            o.y += val11 - val10;
                                        }
                                    }

                                    var cos = o.NormalSafe() * d.NormalSafe();

                                    if (_s > 0.2 * rx || (_f < 0.08 && cos < Math.Cos(Math.PI * 25 / 180)))
                                    {
                                        s[k] = 1.1 * s[k];
                                        s[n] = 1.1 * s[n];
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                return true;
            }

            int iteration = 0;

            public unsafe Bitmap Downscale(Bitmap input, Size newSize)
            {
                iteration = 0;
                Bitmap output = new Bitmap(newSize.Width, newSize.Height, input.PixelFormat);

                initialize(input, output);
                printElapsedTime(" - initialize()");
                saveKernel(input);
                while (true)
                {
                    iteration++;

                    EStep();
                    printElapsedTime(" - EStep() ");
                    bool changedInMStep = MStep();
                    printElapsedTime(" - MStep() ");
                    bool changedInCStep = CStep();
                    printElapsedTime(" - CStep() ");

                    if (iteration % 1 == 0)
                    {
                        saveKernel(input);
                        printElapsedTime(" - saveKernel() ");
                    }
                    if (!changedInMStep || !changedInCStep)
                    {
                        break;
                    }
                }

                return output;
            }
        }

        unsafe Bitmap contentAdaptive(Bitmap input, Size newSize)
        {
            return new ContentAdaptiveDownscale().Downscale(input, newSize);
        }








#region DownscaleMethods
        class Kernel
        {
            public double[] Data { get; private set; } = null;
            public int Width { get; private set; }
            public int Height { get; private set; }
            public Kernel(double[] data, int w, int h)
            {
                Data = data;
                Width = w;
                Height = h;
                System.Diagnostics.Debug.Assert(Data.Length == Width * Height);
            }
        }

        unsafe Bitmap downscaling_kernel(Bitmap input, Size newSize, Kernel k)
        {
            Bitmap output = new Bitmap(newSize.Width, newSize.Height, input.PixelFormat);
            float rx = (float)input.Width / newSize.Width;
            float ry = (float)input.Height / newSize.Height;
            int kwh = k.Width / 2;
            int khh = k.Height / 2;
            using (var o_it = new FLib.BitmapIterator(output, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb))
            {
                byte* o_data = (byte*)o_it.PixelData;
                using (var i_it = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
                {
                    byte* i_data = (byte*)i_it.PixelData;
                    for (int iy = 0; iy < newSize.Height; iy++)
                    {
                        for (int ix = 0; ix < newSize.Width; ix++)
                        {
                            int cx = (int)((0.5 + ix) * rx);
                            int cy = (int)((0.5 + iy) * ry);
                            int o_idx = 4 * ix + iy * o_it.Stride;
                            double r = 0, g = 0, b = 0;
                            for (int ky = 0; ky < k.Height; ky++)
                            {
                                int py = cy - k.Height / 2 + ky;
                                for (int kx = 0; kx < k.Width; kx++)
                                {
                                    int px = cx - k.Width / 2 + kx;
                                    int i_idx = 4 * px + py * i_it.Stride;
                                    if (0 <= px && px < input.Width && 0 <= py && py < input.Height)
                                    {
                                        double w = k.Data[kx + ky * k.Width];
                                        b += w * i_data[i_idx + 0] / 255.0;
                                        g += w * i_data[i_idx + 1] / 255.0;
                                        r += w * i_data[i_idx + 2] / 255.0;
                                    }
                                }
                            }
                            o_data[o_idx + 0] = (byte)(255.0 * b);
                            o_data[o_idx + 1] = (byte)(255.0 * g);
                            o_data[o_idx + 2] = (byte)(255.0 * r);
                            o_data[o_idx + 3] = 255;
                        }
                    }
                }
            }
            return output;
        }

        unsafe Bitmap subsampling(Bitmap input, Size newSize)
        {
            Kernel k = new Kernel(new double[] { 1.0 }, 1, 1);
            return downscaling_kernel(input, newSize, k);
        }

        unsafe Bitmap box(Bitmap input, Size newSize)
        {
            int ksizex = 11;
            int ksizey = 11;
            var kData = Enumerable.Range(0, ksizex * ksizey).Select(_ => 1.0 / (ksizex * ksizey)).ToArray();
            Kernel k = new Kernel(kData, ksizex, ksizey);
            return downscaling_kernel(input, newSize, k);
        }

        unsafe Bitmap gaussian5x5(Bitmap input, Size newSize/*, int k*/)
        {
            var kData = new double[]
            {
                1, 4, 6, 4, 1,
                4, 16, 24, 16, 4,
                6,24,36,24,6,
                4, 16, 24, 16, 4,
                1, 4, 6, 4, 1,
            }.Select(v => v / 256.0).ToArray();
            Kernel k = new Kernel(kData, 5, 5);
            return downscaling_kernel(input, newSize, k);
        }

        unsafe Bitmap bicubic(Bitmap input, Size newSize, double a)
        {
            Bitmap bmp = input;
            int width = newSize.Width;
            int height = newSize.Height;
            int sw = bmp.Width, sh = bmp.Height;
            double wf = (double)sw / width, hf = (double)sh / height;
            Func<double, byte> trimByte = x => (byte)Math.Min(Math.Max(0, (int)Math.Round(x)), 255);

            Func<double, double> weightFunc = d =>
                 d <= 1.0 ? ((a + 2.0) * d * d * d) - ((a + 3.0) * d * d) + 1 :
                 d <= 2.0 ? (a * d * d * d) - (5.0 * a * d * d) + (8.0 * a * d) - (4.0 * a) : 0.0;
            
            var output = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            for (var iy = 0; iy < height; iy++)
            {
                for (var ix = 0; ix < width; ix++)
                {
                    double wfx = wf * ix, wfy = hf * iy;
                    var x = (int)Math.Truncate(wfx);
                    var y = (int)Math.Truncate(wfy);
                    double r = .0, g = .0, b = .0;

                    for (int jy = y - 5; jy <= y + 5; jy++)
                    {
                        for (int jx = x - 5; jx <= x + 5; jx++)
                        {
                            var w = weightFunc(Math.Abs(wfx - jx)) * weightFunc(Math.Abs(wfy - jy));
                            if (w == 0) continue;
                            var sx = (jx >= sw - 1) ? x : jx;
                            var sy = (jy >= sh - 1) ? y : jy;
                            var sc = bmp.GetPixel(sx, sy);
                            r += sc.R * w;
                            g += sc.G * w;
                            b += sc.B * w;
                        }
                    }

                    output.SetPixel(ix, iy, Color.FromArgb(trimByte(r), trimByte(g), trimByte(b)));
                }
            }

            return output;
        }

        Bitmap bicubic(Bitmap input, Size newSize)
        {
            double a;
            if (false == double.TryParse(bicubic_a_TBox.Text, out a))
            {
                a = -1.0f;
            }
            return bicubic(input, newSize, a);
        }



        /**
         *
         * Perceptual
         * 
         **/

        unsafe public class IMG
        {
            public int w;
            public int h;
            public double[] data;
            public IMG(int w, int h)
            {
                this.w = w;
                this.h = h;
                this.data = new double[w * h];
            }

            public static IMG operator +(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img1.data[i] + img2.data[i];
                }
                return img;
            }
            public static IMG operator -(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img1.data[i] - img2.data[i];
                }
                return img;
            }
            public static IMG operator *(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img1.data[i] * img2.data[i];
                }
                return img;
            }
            public static IMG operator /(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img2.data[i] <= 1e-4 ? 0.0 : img1.data[i] / img2.data[i];
                }
                return img;
            }
        }

        unsafe IMG subSample(IMG img1, Size newSize)
        {
            IMG img = new IMG(newSize.Width, newSize.Height);
            double pw = (double)img1.w / newSize.Width;
            double ph = (double)img1.h / newSize.Height;
            for (int y = 0; y < img.h; y++)
            {
                for (int x = 0; x < img.w; x++)
                {
                    // patchの真ん中をサンプリング
                    int sx = (int)((0.5 + x) * pw);
                    int sy = (int)((0.5 + y) * ph);
                    img.data[x + img.w * y] = img1.data[sx + img1.w * sy];
                }
            }
            return img;
        }

        unsafe IMG convValid(IMG input, Kernel k)
        {
            int iw = input.w;
            int ih = input.h;
            int kw = k.Width;
            int kh = k.Height;
            var kdata = k.Data;
            var img1data = input.data;

            IMG output = new IMG(iw - (kw - 1), ih - (kh - 1));
            int ow = output.w;
            int oh = output.h;
            var imgdata = output.data;
            for (int y = 0; y < oh; y++)
            {
                int ooffset = ow * y;
                for (int x = 0; x < ow; x++)
                {
                    double value = 0.0;
                    for (int ky = 0; ky < kh; ky++)
                    {
                        int koffset = ky * kw;
                        int offset = (y + ky) * iw + x;
                        for (int kx = 0; kx < kw; kx++)
                        {
                            value += kdata[kx + koffset] * img1data[kx + offset];
                        }
                    }
                    imgdata[x + ooffset] = value;
                }
            }
            return output;
        }

        unsafe IMG convFull(IMG img1, Kernel k)
        {
            IMG img = new IMG(img1.w + (k.Width - 1), img1.h + (k.Height - 1));
            for (int y = 0; y < img.h; y++)
            {
                for (int x = 0; x < img.w; x++)
                {
                    double value = 0.0;
                    for (int ky = 0; ky < k.Height; ky++)
                    {
                        for (int kx = 0; kx < k.Width; kx++)
                        {
                            int ix = (x + kx - k.Width + 1);
                            int iy = (y + ky - k.Height + 1);
                            if (0 <= ix && ix < img1.w && 0 <= iy && iy < img1.h)
                            {
                                value += k.Data[kx + ky * k.Width] * img1.data[ix + iy * img1.w];
                            }
                        }
                    }
                    img.data[x + img.w * y] = value;
                }
            }
            return img;
        }

        Kernel P(Size s)
        {
            // Average filter
            var data = Enumerable.Range(0, s.Width * s.Height).Select(_ => 1.0 / (s.Width * s.Height)).ToArray();
            return new Kernel(data, s.Width, s.Height);
        }

        IMG I(IMG img1)
        {
            var img = new IMG(img1.w, img1.h);
            for (int i = 0; i < img1.w * img1.h; i++)
            {
                img.data[i] = 1.0;
            }
            return img;
        }

        IMG Sqrt(IMG img1)
        {
            var img = new IMG(img1.w, img1.h);
            for (int i = 0; i < img1.w * img1.h; i++)
            {
                // 誤差の関係で0以下になることがある
                img.data[i] = img1.data[i] < 0 ? 0 : Math.Sqrt(img1.data[i]);
            }
            return img;
        }

        IMG Clamp01(IMG img1)
        {
            var img = new IMG(img1.w, img1.h);
            for (int i = 0; i < img1.w * img1.h; i++)
            {
                // 誤差の関係で0以下になることがある
                img.data[i] = img1.data[i] < 0 ? 0 : img1.data[i] > 1 ? 1 : img1.data[i];
            }
            return img;
        }

        unsafe IMG extractChannel(Bitmap input, int channel)
        {
            IMG img = new IMG(input.Width, input.Height);
            using (var i_it = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                byte* i_data = (byte*)i_it.PixelData;
                for (int iy = 0; iy < input.Height; iy++)
                {
                    for (int ix = 0; ix < input.Width; ix++)
                    {
                        int i_idx = 4 * ix + iy * i_it.Stride;
                        img.data[ix + iy * input.Width] = i_data[i_idx + channel] / 255.0;
                    }
                }
            }
            return img;
        }

        IMG perceptual(IMG H, Size newSize)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Follow Appendix B.
            Size s = new Size(H.w / newSize.Width, H.h / newSize.Height);
            Size np = new Size(2, 2);
            var hps = convValid(H, P(s));
            Console.WriteLine(string.Format("phase0.5: {0} ms", stopwatch.ElapsedMilliseconds));
            var L = subSample(hps, newSize);
            Console.WriteLine(string.Format("phase1: {0} ms", stopwatch.ElapsedMilliseconds));
            var L2 = subSample(convValid(H * H, P(s)), newSize);
            Console.WriteLine(string.Format("phase2: {0} ms", stopwatch.ElapsedMilliseconds));

            System.Diagnostics.Debug.Assert(L.w == newSize.Width);
            System.Diagnostics.Debug.Assert(L.h == newSize.Height);
            var M = convValid(L, P(np));
            Console.WriteLine(string.Format("phase3: {0} ms", stopwatch.ElapsedMilliseconds));

            var Sl = convValid(L * L, P(np)) - M * M;
            Console.WriteLine(string.Format("phase4: {0} ms", stopwatch.ElapsedMilliseconds));

            var Sh = convValid(L2, P(np)) - M * M;
            Console.WriteLine(string.Format("phase5: {0} ms", stopwatch.ElapsedMilliseconds));

            var R = Sqrt(Sh / Sl);
            Console.WriteLine(string.Format("phase6: {0} ms", stopwatch.ElapsedMilliseconds));

            R = Clamp01(R);
            Console.WriteLine(string.Format("phase7: {0} ms", stopwatch.ElapsedMilliseconds));

            var N = convFull(I(M), P(np));
            Console.WriteLine(string.Format("phase8: {0} ms", stopwatch.ElapsedMilliseconds));

            var T = convFull(R * M, P(np));
            Console.WriteLine(string.Format("phase9: {0} ms", stopwatch.ElapsedMilliseconds));

            M = convFull(M, P(np));
            Console.WriteLine(string.Format("phase10: {0} ms", stopwatch.ElapsedMilliseconds));

            R = convFull(R, P(np));
            Console.WriteLine(string.Format("phase11: {0} ms", stopwatch.ElapsedMilliseconds));

            var D = (M + R * L - T) / N;
            Console.WriteLine(string.Format("phase12: {0} ms", stopwatch.ElapsedMilliseconds));

            System.Diagnostics.Debug.Assert(D.w == newSize.Width);
            System.Diagnostics.Debug.Assert(D.h == newSize.Height);
            Console.WriteLine(string.Format("phase13: {0} ms", stopwatch.ElapsedMilliseconds));

            return D;
        }

        unsafe Bitmap toBitmap(IMG R, IMG G, IMG B, PixelFormat pixelFormat)
        {
            int w = R.w;
            int h = R.h;
            Bitmap output = new Bitmap(w, h, pixelFormat);
            using (var o_it = new FLib.BitmapIterator(output, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb))
            {
                byte* o_data = (byte*)o_it.PixelData;
                for (int iy = 0; iy < h; iy++)
                {
                    for (int ix = 0; ix < w; ix++)
                    {
                        int o_idx = 4 * ix + iy * o_it.Stride;
                        o_data[o_idx + 0] = (byte)(255.0 * B.data[ix + iy * w]);
                        o_data[o_idx + 1] = (byte)(255.0 * G.data[ix + iy * w]);
                        o_data[o_idx + 2] = (byte)(255.0 * R.data[ix + iy * w]);
                        o_data[o_idx + 3] = 255;
                    }
                }
            }
            return output;
        }

        unsafe Bitmap perceptual(Bitmap input, Size newSize)
        {
            var imgB = extractChannel(input, 0);
            var imgG = extractChannel(input, 1);
            var imgR = extractChannel(input, 2);
            var DB = perceptual(imgB, newSize);
            var DG = perceptual(imgG, newSize);
            var DR = perceptual(imgR, newSize);
            var output = toBitmap(DR, DG, DB, input.PixelFormat);
            return output;
        }
#endregion

        class ShowImageCollection
        {
            private Dictionary<string, Bitmap> outputDict_ = new Dictionary<string, Bitmap>();

            public ShowImageCollection()
            {
            }

            public void Clear()
            {
                foreach (var val in outputDict_.Values)
                {
                    if (val != null)
                    {
                        val.Dispose();
                    }
                }
                outputDict_.Clear();
            }

            public void Set(string key, Bitmap img)
            {
                if (outputDict_.ContainsKey(key) && outputDict_[key] != null)
                {
                    outputDict_[key].Dispose();
                    outputDict_.Remove(key);
                }
                outputDict_[key] = img;
            }

            public Bitmap Get(string key)
            {
                if (outputDict_.ContainsKey(key))
                {
                    return outputDict_[key];
                }
                return null;
            }
        }

        ShowImageCollection collection = new ShowImageCollection();
        string showImageKey = "input";
        Bitmap showImage = null;

        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            openImage(@"../../../../data/input/mario1_input.png");
        }

        void openImage(string fileName)
        {
            using (var bmp = new Bitmap(fileName))
            {
                // コピーしないと参照元の画像ファイルがロックされる
                collection.Clear();
                var input = new Bitmap(bmp);
                collection.Set(inputRButton.Text, input);
                donwscale(collection, input);
                inputRButton.Checked = true;
                radioButton_CheckedChanged(inputRButton, null);
            }
        }

        private void openImageOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog.FileName = "";
            openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(@"../../../../data/input");
            openFileDialog.Filter = "*.png;*.jpg;*.bmp|*.png;*.jpg;*.bmp|すべてのファイル(*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.Title = "Select Image File";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                openImage(openFileDialog.FileName);
            }
        }

        void updateShowImage()
        {
            var bmp = collection.Get(showImageKey);
            if (bmp != null)
            {
                if (showImage != null)
                {
                    showImage.Dispose();
                    showImage = null;
                }
                showImage = new Bitmap(bmp);
                canvas.Invalidate();
            }
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Checked)
            {
                showImageKey = radioButton.Text;
                updateShowImage();
            }
        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            if (showImage != null)
            {
                var clip = e.Graphics.VisibleClipBounds;
                int iw = showImage.Width;
                int ih = showImage.Height;
                float rw = (float)clip.Width / iw;
                float rh = (float)clip.Height / ih;
                float ratio = Math.Min(rw, Math.Min(rh, 100000.0f));
                float w = iw * ratio;
                float h = ih * ratio;
                float x = clip.X + clip.Width * 0.5f - w * 0.5f;
                float y = clip.Y + clip.Height * 0.5f - h * 0.5f;
                e.Graphics.DrawImage(showImage, x, y, w, h);
            }
        }

        private void bicubic_a_TBox_TextChanged(object sender, EventArgs e)
        {
            if (collection != null)
            {
                var input = collection.Get(inputRButton.Text);
                if (input != null)
                {
                    double scalePercentage = 10.0;
                    if (double.TryParse(scaleTBox.Text, out scalePercentage))
                    {
                        int w = (int)(input.Width * scalePercentage * 0.01);
                        int h = (int)(input.Height * scalePercentage * 0.01);
                        var size = new Size(w, h);
                        collection.Set(bicubicRButton.Text, bicubic(input, size));
                        updateShowImage();
                    }
                }
            }
        }

        private void downscaleButton_Click(object sender, EventArgs e)
        {
            var input = collection.Get(inputRButton.Text);
            donwscale(collection, input);
            updateShowImage();
        }

        class DonwscaleWorkArg
        {
            public Bitmap input;
            public ShowImageCollection collection;
        }

        void donwscale(ShowImageCollection collecton, Bitmap input)
        {
            //処理が行われているときは、何もしない
            if (backgroundWorker1.IsBusy)
            {
                return;
            }
            downscaleButton.Enabled = false;
            richTextBox1.Text = "";
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.RunWorkerAsync(new DonwscaleWorkArg()
            {
                collection = collection,
                input = input,
            });
        }

        private void downscale_BGWork(object sender, DoWorkEventArgs e)
        {
            var bgWorker = sender as BackgroundWorker;
            var args = e.Argument as DonwscaleWorkArg;
            if (args != null)
            {
                var input = args.input;
                var collection = args.collection;
                if (input != null && collection != null)
                {
                    double scalePercentage = 10.0;
                    if (double.TryParse(scaleTBox.Text, out scalePercentage))
                    {
                        int w = (int)(input.Width * scalePercentage * 0.01);
                        int h = (int)(input.Height * scalePercentage * 0.01);
                        var size = new Size(w, h);

                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        collection.Set(subsamplingRButton.Text, subsampling(input, size));
                        bgWorker.ReportProgress(0, "Subsampling: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(boxRButton.Text, box(input, size));
                        bgWorker.ReportProgress(25, "Box: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(gaussianRButton.Text, gaussian5x5(input, size));
                        bgWorker.ReportProgress(50, "Gaussian: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(bicubicRButton.Text, bicubic(input, size));
                        bgWorker.ReportProgress(75, "Bicubic: " + sw.Elapsed.TotalSeconds + " s");
                        //collection.Set(perceptualRButton.Text, perceptual(input, size));
                        //bgWorker.ReportProgress(100, "Perceptual: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(contentAdaptiveRButton.Text, contentAdaptive(input, size));
                        bgWorker.ReportProgress(100, "ContentAdaptive: " + sw.Elapsed.TotalSeconds + " s");
                    }
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            richTextBox1.Text += e.UserState as string + "\n";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            downscaleButton.Enabled = true;
            richTextBox1.Text += "All downscaling methods successfully finished!";
        }
    }
}
