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
        decimal[] s;
        Vec3m[] c; // CIELAB, [0, 1]
        List<decimal[]> w_;
        List<decimal[]> g_;
        decimal[] w(Kernel k)
        {
            return w_[k.index];
        }
        decimal[] g(Kernel k)
        {
            return g_[k.index];
        }


        //--------------------------------------------------------------------------

        int iteration = 0;
        public Bitmap Downscale(Bitmap input, Size newSize)
        {
            iteration = 0;
            Bitmap output = new Bitmap(newSize.Width, newSize.Height, input.PixelFormat);
            Config config = new Config(input.Width, input.Height, newSize.Width, newSize.Height);
            initialize(config, input);
            printElapsedTime(" - initialize()");
            while (true)
            {
                iteration++;

                try
                {
                    eStep(config);
                    printElapsedTime(" - EStep() ");
                    mStep(config);
                    printElapsedTime(" - MStep() ");
                    cStep(config);
                    printElapsedTime(" - CStep() ");

                    if (iteration % 1 == 0)
                    {
                        printElapsedTime(" - saveKernel() ");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                break;
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
                        c[x + y * input.Width] = new Vec3m(b / 255m, g / 255m, r / 255m);
                    }
                }
            }

            // init
            w_ = new List<decimal[]>();
            g_ = new List<decimal[]>();
            m = new Vec2m[config.wo * config.ho];
            S = new Mat2x2m[config.wo * config.ho];
            v = new Vec3m[config.wo * config.ho];
            s = new decimal[config.wo * config.ho];
            For.AllKernels(config, (_, k) =>
            {
                w_.Add(new decimal[Rsize]);
                g_.Add(new decimal[Rsize]);
                m[k.index] = new Vec2m(k.x, k.y);
                S[k.index] = new Mat2x2m(config.rx / 3m, 0, 0, config.ry / 3m);
                v[k.index] = new Vec3m(0.5m, 0.5m, 0.5m);
                s[k.index] = 1e-4m;
            });
        }

        void eStep(Config config)
        {
            decimal[] sum_w = new decimal[config.wi * config.hi];

            For.AllKernels(config, (_, k) =>
            {
                decimal wsum = 0m;
                For.AllPixeelsOfRegion(config, k, (_1, i) =>
                {
                    w(k)[i.index - k.indexi] = calcGaussian(k, i);
                    wsum += w(k)[i.index - k.indexi];
                });

                For.AllPixeelsOfRegion(config, k, (_1, i) =>
                {
                    w(k)[i.index - k.indexi] /= wsum;
                    sum_w[i.index - k.indexi] += w(k)[i.index - k.indexi];
                });
            });

            For.AllPixels(config, (_, i) =>
            {
                For.AllKernelOfPixel(config, i, (_1, k) =>
                {
                    g(k)[i.index - k.indexi] = w(k)[i.index - k.indexi] / sum_w[i.index - k.indexi];
                });
            });
        }

        void mStep(Config config)
        {
            For.AllKernels(config, (_, k) =>
            {
                var gsum = sumInRegion(config, k, i => g(k)[i.index - k.indexi]);
                S[k.index] = sumInRegion(config, k, i => g(k)[i.index - k.indexi] * Mat2x2m.FromVecVec(i.p - m[i.index], i.p - m[i.index]));
                m[k.index] = sumInRegion(config, k, i => g(k)[i.index - k.indexi] * i.p);
                v[k.index] = sumInRegion(config, k, i => g(k)[i.index - k.indexi] * c[i.index]);
            });
        }

        void cStep(Config config)
        {
            // TODO:
        }

        decimal calcGaussian(Kernel k, Position i)
        {
            var dpos = i.p - m[k.index];
            var invS = S[k.index].Inverse();
            var posTerm = -0.5m * dpos * invS * dpos;
            var dcol = c[i.index] - v[k.index];
            var colTerm = -Vec3m.DistanceSqr(c[i.index], v[k.index]) / (2 * s[k.index] * s[k.index]);
            var result = (decimal)Math.Exp((double)(posTerm + colTerm));
            return result;
        }

        decimal sumInRegion(Config config, Kernel k, Func<Position, decimal> pos2val)
        {
            decimal result = 0m;
            For.AllPixeelsOfRegion(config, k, (_, i) =>
            {
                result += pos2val(i);
            });
            return result;
        }

        Mat2x2m sumInRegion(Config config, Kernel k, Func<Position, Mat2x2m> pos2val)
        {
            var result = new Mat2x2m(0, 0, 0, 0);
            For.AllPixeelsOfRegion(config, k, (_, i) =>
            {
                result += pos2val(i);
            });
            return result;
        }

        Vec2m sumInRegion(Config config, Kernel k, Func<Position, Vec2m> pos2val)
        {
            var result = new Vec2m(0, 0);
            For.AllPixeelsOfRegion(config, k, (_, i) =>
            {
                result += pos2val(i);
            });
            return result;
        }


        Vec3m sumInRegion(Config config, Kernel k, Func<Position, Vec3m> pos2val)
        {
            var result = new Vec3m(0, 0, 0);
            For.AllPixeelsOfRegion(config, k, (_, i) =>
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