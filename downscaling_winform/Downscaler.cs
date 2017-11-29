//#define ENABLE_MULTITHREADING
#define PERFORMANCE_MEASURE

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

using Config = FLib.ContenteBaseDownscaleUtils.Config;
using For = FLib.ContenteBaseDownscaleUtils.For;
using Position = FLib.ContenteBaseDownscaleUtils.Position;
using Kernel = FLib.ContenteBaseDownscaleUtils.Kernel;

namespace FLib
{
    public class ContentBasedDownscale2
    {
        Vec2m[] m;
        Mat2x2m[] S;
        Vec3m[] v;
        double[] s;
        Vec3m[] c; // CIELAB, [0, 1]
        List<double[]> w_;
        List<double[]> g_;
        double[] w(Kernel k)
        {
            return w_[k.index];
        }
        double[] g(Kernel k)
        {
            return g_[k.index];
        }


        //--------------------------------------------------------------------------

        public void RunIteration(Config config)
        {
            eStep(config);
            printElapsedTime(" - EStep() ");
            mStep(config);
            printElapsedTime(" - MStep() ");
            cStep(config);
            printElapsedTime(" - CStep() ");
        }


        // return image whose size is [config.wo, config.ho].
        public unsafe Bitmap CreateOutputImage(Config config)
        {
            var bmp = new Bitmap(config.wo, config.ho, PixelFormat.Format32bppArgb);
            using (var it = new FLib.BitmapIterator(bmp, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb))
            {
                byte* data = it.Data;

                For.AllKernels(config, (_, k) =>
                {
                    List<Vec3m> debug = new List<Vec3m>();

                    Vec3m sumColor = new Vec3m(0, 0, 0);
                    double sumWeight = 0;
                    int count = 0;
                    For.AllPixelsOfRegion(config, k, (_0, i) =>
                    {
                        double weight = w(k)[index(config, i, k)];
                        int x = (int)(i.p.x + m[k.index].x - k.xi - (int)(1.5 * config.rx));
                        int y = (int)(i.p.y + m[k.index].y - k.yi - (int)(1.5 * config.rx));
                        if (x < 0 || config.wi <= x || y < 0 || config.hi <= y)
                        {
                            return;
                        }
                        int idx = x + y * config.wi;
                        sumColor += weight * c[idx];
                        sumWeight += weight;
                        count++;
                        debug.Add(weight * c[idx]);
                    });

                    Vec3m aveColor = sumColor / sumWeight;
                    System.Diagnostics.Debug.Assert(0 <= aveColor.x && aveColor.x <= 1);
                    System.Diagnostics.Debug.Assert(0 <= aveColor.y && aveColor.y <= 1);
                    System.Diagnostics.Debug.Assert(0 <= aveColor.z && aveColor.z <= 1);
                    byte r = (byte)(255 * aveColor.x);
                    byte g = (byte)(255 * aveColor.y);
                    byte b = (byte)(255 * aveColor.z);
                    data[4 * k.x + k.y * it.Stride + 0] = b;
                    data[4 * k.x + k.y * it.Stride + 1] = g;
                    data[4 * k.x + k.y * it.Stride + 2] = r;
                    data[4 * k.x + k.y * it.Stride + 3] = 255;
                });
            }
            return bmp;
        }

        int iteration = 0;
        public Bitmap Downscale(Bitmap input, Size newSize)
        {
            if (!System.IO.Directory.Exists("../output"))
            {
                System.IO.Directory.CreateDirectory("../output");
            }
            System.Diagnostics.Process.Start(System.IO.Path.GetFullPath("../output"));

            iteration = 0;
            Bitmap output = new Bitmap(newSize.Width, newSize.Height, input.PixelFormat);
            Config config = new Config(input.Width, input.Height, newSize.Width, newSize.Height);
            initialize(config, input);
            printElapsedTime(" - initialize()");

            for (int i = 0; i < 30; i++)
            {
                iteration++;

                try
                {
                    RunIteration(config);
                    if (iteration % 1 == 0)
                    {
                        string path = "../output/downscaled-" + iteration + ".png";
                        using (var bmp = CreateOutputImage(config))
                        {
                            bmp.Save(path);
                        }
                        printElapsedTime(" - saveKernel(): " + path + ", ");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return output;
        }

        unsafe void initialize(Config config, Bitmap input)
        {
            int Rsize = (int)(4 * config.rx + 1) * (int)(4 * config.ry + 1);

            // copy color
            c = new Vec3m[config.wi * config.hi];
            System.Diagnostics.Debug.Assert(c.Length == input.Width * input.Height);
            using (BitmapIterator iter = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                byte* data = (byte*)iter.PixelData;
                for (int y = 0; y < input.Height; y++)
                {
                    for (int x = 0; x < input.Width; x++)
                    {
                        int idx = 4 * x + y * iter.Stride;
                        byte b = data[idx + 0];
                        byte g = data[idx + 1];
                        byte r = data[idx + 2];
                        c[x + y * input.Width] = new Vec3m(r / 255.0, g / 255.0, b / 255.0);
                    }
                }
            }

            // init
            w_ = new List<double[]>();
            g_ = new List<double[]>();
            m = new Vec2m[config.wo * config.ho];
            S = new Mat2x2m[config.wo * config.ho];
            v = new Vec3m[config.wo * config.ho];
            s = new double[config.wo * config.ho];
            For.AllKernels(config, (_, k) =>
            {
                w_.Add(new double[Rsize]);
                g_.Add(new double[Rsize]);
                m[k.index] = new Vec2m((0.5 + k.x) * config.rx, (0.5 + k.y) * config.ry);
                S[k.index] = new Mat2x2m(config.rx / 3, 0, 0, config.ry / 3);
                v[k.index] = new Vec3m(0.5, 0.5, 0.5);
                s[k.index] = 1e-4;
            });
        }

        int index(Config config, Position i, Kernel k)
        {
            int rx = (int)(4 * config.rx + 1);
            int ry = (int)(4 * config.ry + 1);
            int ox = -(int)(1.5 * config.rx);
            int oy = -(int)(1.5 * config.ry);
            int x = (int)i.p.x - k.xi - ox;
            int y = (int)i.p.y - k.yi - oy;
            int w = (int)(4 * config.rx + 1);
            int h = (int)(4 * config.ry + 1);
            if (0 <= x && x < w && 0 <= y && y < h)
            {
                return x + y * (int)(4 * config.rx + 1);
            }
            return -1;
        }

        void eStep(Config config)
        {
            double[] sum_w = new double[config.wi * config.hi];

            For.AllKernels(config, (_, k) =>
            {
                double sum = 0;
                For.AllPixelsOfRegion(config, k, (_1, i) =>
                {
                    int idx = index(config, i, k);
                    System.Diagnostics.Debug.Assert(idx >= 0);
                    System.Diagnostics.Debug.Assert(idx < w(k).Length);
                    w(k)[index(config, i, k)] = calcGaussian(k, i);
                    sum += w(k)[index(config, i, k)];
                });

                For.AllPixelsOfRegion(config, k, (_1, i) =>
                {
                    w(k)[index(config, i, k)] = div(w(k)[index(config, i, k)], sum);
                    sum_w[i.index] += w(k)[index(config, i, k)];
                });
            });

            For.AllPixels(config, (_, i) =>
            {
                For.AllKernelOfPixel(config, i, (_1, k) =>
                {
                    g(k)[index(config, i, k)] = div(w(k)[index(config, i, k)], sum_w[i.index]);
                });
            });
        }

        void mStep(Config config)
        {
            For.AllKernels(config, (_, k) =>
            {
                var gsum = sumInRegion(config, k, i => g(k)[index(config, i, k)]);
                S[k.index] = sumInRegion(config, k, i => g(k)[index(config, i, k)] * Mat2x2m.FromVecVec(i.p - m[k.index], i.p - m[k.index])) / gsum;
                m[k.index] = sumInRegion(config, k, i => g(k)[index(config, i, k)] * i.p) / gsum;
                v[k.index] = sumInRegion(config, k, i => g(k)[index(config, i, k)] * c[i.index]) / gsum;
            });
        }

        void cStep(Config config)
        {
            // Spatial constraints
            var aveM = new Vec2m[config.KernelSize];
            For.AllKernels(config, (_, k) =>
            {
                aveM[k.index] = new Vec2m(0, 0);
                var neighbors = k.Neighbors4(config);
                foreach (var n in neighbors)
                {
                    aveM[k.index] += m[n.index];
                }
                aveM[k.index] /= neighbors.Count;
            });
            For.AllKernels(config, (_, k) =>
            {
                m[k.index] = 0.5 * (aveM[k.index] + m[k.index]);
                double halfWidth = 0.25 * config.rx;
                double halfHeight = 0.25 * config.ry;
                m[k.index] = clampBox(m[k.index], k.xi - halfWidth, k.yi - halfHeight, 2 * halfWidth, 2 * halfHeight);
            });

            // Constrain spatial variance

            For.AllKernels(config, (_, k) =>
            {
                Mat2x2m _U, _S, _Vt;
                S[k.index].SVD(out _U, out _S, out _Vt);
                _S.m11 = clamp(_S.m11, 0.05, 0.1);
                _S.m22 = clamp(_S.m22, 0.05, 0.1);
                var newS = _U * _S * _Vt;
                if (double.IsNaN(newS.Inverse().m11) == false)
                {
                    S[k.index] = newS;
                }
            });


            // Shape constraints
            For.AllKernels(config, (_, k) =>
            {
                var neighbors = k.Neighbors8(config);
                foreach (var n in neighbors)
                {
                    var d = new Vec2m(n.xi - k.xi, n.yi - k.yi);
                    var sv = sumInRegion(config, k, (i) =>
                    {
                        double gki = g(k)[index(config, i, k)];
                        double dot = (i.p - m[k.index]) * d;
                        return gki * Math.Max(0, dot);
                    });
                    var f = sumInRegion(config, k, (i) =>
                    {
                        try
                        {
                            double gki = g(k)[index(config, i, k)];
                            double gni = index(config, i, n) >= 0 ? g(k)[index(config, i, n)] : 0;
                            return gki * gni;
                        }catch (Exception e)
                        {
                            return 0;
                        }
                    });
                    var o = sumInRegion(config, k, (i) =>
                    {
                        if (i.p.x >= config.wi)
                        {
                            return new Vec2m(0, 0);
                        }
                        if (i.p.y >= config.hi)
                        {
                            return new Vec2m(0, 0);
                        }
                        double gki = g(k)[index(config, i, k)];
                        double gni = index(config, i, n) >= 0 ? g(k)[index(config, i, n)] : 0;
                        var p10 = new Position(config, (int)i.p.x + 1, (int)i.p.y);
                        var p01 = new Position(config, (int)i.p.x, (int)i.p.y + 1);
                        double gki10 = g(k)[index(config, p10, k)];
                        double gki01 = g(k)[index(config, p01, k)];
                        double val00 = gki / (gki + gni);
                        double val10 = gki10 / (gki10 + gni);
                        double val01 = gki01 / (gki01 + gni);
                        return new Vec2m(val10 - val00, val01 - val00);
                    });

                    double cos25 = Math.Cos(Math.PI * 25 / 180.0);
                    if (sv > 0.2 * config.rx || (f < 0.08 && d.NormalSafe() * o.NormalSafe() < cos25))
                    {
                        s[k.index] *= 1.1;
                        s[n.index] *= 1.1;
                    }
                }
            });
        }

        Vec2m clampBox(Vec2m p, double left, double top, double width, double height)
        {
            return new Vec2m(clamp(p.x, left, left + width), clamp(p.y, top, top + height));
        }

        double clamp(double val, double min, double max)
        {
            return Math.Max(min, Math.Min(max, val));
        }

        double div(double a, double b)
        {
            if (Math.Abs(b) > 0)
            {
                return a / b;
            }
            return a;
        }

        double calcGaussian(Kernel k, Position i)
        {
            var dpos = i.p - m[k.index];
            var invS = S[k.index].Inverse();
            var posTerm = -0.5 * dpos * invS * dpos;
            var dcol = c[i.index] - v[k.index];
            var colTerm = -Vec3m.DistanceSqr(c[i.index], v[k.index]) / (2 * s[k.index] * s[k.index]);
            try
            {
                double val = Math.Max(-1e2, Math.Min(1e2, posTerm + colTerm));
                var result = Math.Exp(val);
                System.Diagnostics.Debug.Assert(double.IsNaN(result) == false);
                return result;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        double sumInRegion(Config config, Kernel k, Func<Position, double> pos2val)
        {
            double result = 0;
            For.AllPixelsOfRegion(config, k, (_, i) =>
            {
                result += pos2val(i);
            });
            return result;
        }

        Mat2x2m sumInRegion(Config config, Kernel k, Func<Position, Mat2x2m> pos2val)
        {
            var result = new Mat2x2m(0, 0, 0, 0);
            For.AllPixelsOfRegion(config, k, (_, i) =>
            {
                result += pos2val(i);
            });
            return result;
        }

        Vec2m sumInRegion(Config config, Kernel k, Func<Position, Vec2m> pos2val)
        {
            var result = new Vec2m(0, 0);
            For.AllPixelsOfRegion(config, k, (_, i) =>
            {
                result += pos2val(i);
            });
            return result;
        }


        Vec3m sumInRegion(Config config, Kernel k, Func<Position, Vec3m> pos2val)
        {
            var result = new Vec3m(0, 0, 0);
            For.AllPixelsOfRegion(config, k, (_, i) =>
            {
                result += pos2val(i);
            });
            return result;
        }




        System.Diagnostics.Stopwatch stopwatch = null;
        int stopwatchCnt = 0;

        void printElapsedTime(string prefix = "")
        {
            if (null == stopwatch)
            {
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
            }
            Console.WriteLine($"[{stopwatchCnt}]{prefix}{stopwatch.ElapsedMilliseconds} ms.");
            stopwatchCnt++;
        }

    }
}