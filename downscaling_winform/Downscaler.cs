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
using For= FLib.ContenteBaseDownscaleUtils.For;

namespace FLib
{
    public class ContentBasedDownscale2
    {
        Vec2m[] m;
        Mat2x2m[] S;
        Vec3m[] v;
        decimal[] s;
        Vec3m[] c; // CIELAB, [0, 1]
        decimal[] w;
        decimal[] g;

        internal void Intiailize(Config config_)
        {
            For.AllKernels(config_, (config, k) => {
                m[k.index] = new Vec2m(k.x, k.y);
                S[k.index] = new Mat2x2m(config.rx / 3m, 0, 0, config.ry / 3m);
                v[k.index] = new Vec3m(0.5m, 0.5m, 0.5m);
                s[k.index] = 1e-4m;
            });
        }

        // TODO:
    }


    public class ContentBasedDownscaler
    {
#if PERFORMANCE_MEASURE
        const bool MeasurePerformance = true;
#else
            const bool MeasurePerformance = false;
#endif

        int wi, hi;
        int wo, ho;
        decimal rx, ry;

        Vec2m[] m;
        Mat2x2m[] S;
        Vec3m[] v;
        decimal[] s;
        Vec3m[] c; // CIELAB, [0, 1]
        decimal[] w;
        decimal[] g;
        int Rsize = 0;



        public unsafe Bitmap kernel2bmp(bool normal)
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
                        var set = KR(kx, ky);
                        int mx = (int)m[set.k].x;
                        int my = (int)m[set.k].y;
                        for (int dy = (int)(-2 * ry); dy <= (int)(2 * ry); dy++)
                        {
                            for (int dx = (int)(-2 * rx); dx <= (int)(2 * rx); dx++)
                            {
                                int px = mx + kx;
                                int py = my + ky;
                                if (px < 0 || wi <= px || py < 0 || hi <= py)
                                {
                                    continue;
                                }
                                int kid = kidx(kx, ky, (int)set.cx + dx, (int)set.cy + dy);
                                if (kid < 0)
                                {
                                    continue;
                                }
                                decimal d = a[kid];
                                decimal n = (decimal)Math.Pow(0.1, (double)d);
                                byte value = (byte)(n * 255.0m);
                                if ((kx + ky) % 2 == 0)
                                {
                                    data[4 * px + py * it.Stride + 0] += value;
                                    data[4 * px + py * it.Stride + 1] = 0;
                                    data[4 * px + py * it.Stride + 2] += value;
                                    data[4 * px + py * it.Stride + 3] = 255;
                                }
                                else
                                {
                                    data[4 * px + py * it.Stride + 0] = 0;
                                    data[4 * px + py * it.Stride + 1] += value;
                                    data[4 * px + py * it.Stride + 2] = 0;
                                    data[4 * px + py * it.Stride + 3] = 255;
                                }
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        class KernelRegion
        {
            public int k, kx, ky, rx0, rx1, ry0, ry1;
            public decimal cx, cy;
        }

        KernelRegion KR(int kx, int ky)
        {
            decimal cx = rx * (kx + 0.5m);
            decimal cy = ry * (ky + 0.5m);
            return new KernelRegion()
            {
                k = kx + wo * ky,
                cx = cx,
                cy = cy,
                rx0 = Math.Max((int)(cx - 2 * rx), 0),
                rx1 = Math.Min((int)(cx + 2 * rx), wi - 1),
                ry0 = Math.Max((int)(cy - 2 * ry), 0),
                ry1 = Math.Min((int)(cy + 2 * ry), hi - 1),
            };
        }

        public unsafe Bitmap filter(Bitmap input)
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
                        var kreg = KR(kx, ky);
                        var set = KR(kx, ky);
                        int mx = (int)m[set.k].x;
                        int my = (int)m[set.k].y;
                        decimal r = 0.0m;
                        decimal g = 0.0m;
                        decimal b = 0.0m;
                        decimal sum = 0;
                        for (int dy = (int)(-2 * ry); dy <= (int)(2 * ry); dy++)
                        {
                            for (int dx = (int)(-2 * rx); dx <= (int)(2 * rx); dx++)
                            {
                                int px = mx + kx;
                                int py = my + ky;
                                if (px < 0 || wi <= px || py < 0 || hi <= py)
                                {
                                    continue;
                                }
                                int kid = kidx(kx, ky, (int)set.cx + dx, (int)set.cy + dy);
                                if (kid < 0)
                                {
                                    continue;
                                }
                                decimal d = a[kid];
                                sum += d;
                                r += d * (data_in[4 * px + py * it_in.Stride + 2] / 255.0m);
                                g += d * (data_in[4 * px + py * it_in.Stride + 1] / 255.0m);
                                b += d * (data_in[4 * px + py * it_in.Stride + 0] / 255.0m);
                            }
                        }
                        if (sum <= 0)
                        {
                            r = g = b = 0;
                        }
                        else
                        {
                            r /= sum;
                            g /= sum;
                            b /= sum;
                        }
                        data[4 * kx + ky * it.Stride + 2] = (byte)(255 * r);
                        data[4 * kx + ky * it.Stride + 1] = (byte)(255 * g);
                        data[4 * kx + ky * it.Stride + 0] = (byte)(255 * b);
                        data[4 * kx + ky * it.Stride + 3] = 255;
                    }
                }
            }
            return bmp;
        }

        int iteration = 0;
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
            if (MeasurePerformance)
            {
                Console.WriteLine($"[{stopwatchCnt}]{prefix}{stopwatch.ElapsedMilliseconds} ms.");
                stopwatchCnt++;
            }
        }

        public unsafe void initialize(Bitmap input, Bitmap output)
        {
            if (MeasurePerformance)
            {
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                stopwatchCnt = 0;
            }

            wi = input.Width;
            hi = input.Height;
            wo = output.Width;
            ho = output.Height;
            rx = (decimal)wi / wo;
            ry = (decimal)hi / ho;
            m = new Vec2m[wo * ho];
            S = new Mat2x2m[wo * ho];
            v = new Vec3m[wo * ho];
            s = new decimal[wo * ho];
            int k = 0;
            for (int ky = 0; ky < ho; ky++)
            {
                for (int kx = 0; kx < wo; kx++)
                {
                    m[k] = new Vec2m(rx * (kx + 0.5m), ry * (ky + 0.5m));
                    S[k] = new Mat2x2m(rx / 3, 0, 0, ry / 3);
                    v[k] = new Vec3m(0.5m, 0.5m, 0.5m);
                    s[k] = 1e-4m * wi * hi; // * wiしないと小さくなりすぎる？
                    k++;
                }
            }

            c = new Vec3m[wi * hi];
            using (var i_it = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                byte* i_data = (byte*)i_it.PixelData;
                for (int iy = 0; iy < input.Height; iy++)
                {
                    for (int ix = 0; ix < input.Width; ix++)
                    {
                        int i = ix + iy * input.Width;
                        int i_idx = 4 * ix + iy * i_it.Stride;
                        decimal r = i_data[i_idx + 0] / 255.0m;
                        decimal g = i_data[i_idx + 1] / 255.0m;
                        decimal b = i_data[i_idx + 2] / 255.0m;

                        // TODO: convert to CIELAB
                        c[i] = new Vec3m(r, g, b);
                    }
                }
            }

            // NOTE: (wo * ho) * (wi * hi) happened out of memory error.
            //       We should save only the region R_k for each kernel k.
            Rsize = (int)(4 * rx + 1) * (int)(4 * ry + 1);
            w = new decimal[Rsize * wo * ho];
            g = new decimal[Rsize * wo * ho];

            Console.WriteLine("[Content-Adaptive] initialize()");
        }

        // get index with the value is wk[i] (or gk[i] in pseude code.
        public int kidx(int kx, int ky, int ix, int iy)
        {
            if (!(0 <= kx && kx <= wo && 0 <= ky && ky <= ho))
                return -1;
            if (!(0 <= iy && ix <= wi && 0 <= iy && iy <= hi))
                return -1;
            decimal cx = (kx + 0.5m) * rx;
            decimal cy = (ky + 0.5m) * ry;
            int xmin = (int)(cx - 2 * rx);
            int xmax = (int)(cx + 2 * rx);
            int ymin = (int)(cy - 2 * ry);
            int ymax = (int)(cy + 2 * ry);
            int x = ix - xmin;
            int y = iy - ymin;
            if (0 <= x && x < 4 * rx + 1 && 0 <= y && y < 4 * ry + 1)
            {
                int offset = (kx + ky * wo) * Rsize;
                int idx = x + y * (xmax - xmin + 1) + offset;
                return idx;
            }
            return -1;
        }

        decimal sumInKernel(decimal[] array, int kx, int ky)
        {
            var kreg = KR(kx, ky);
            decimal wsum = 0.0m;
            for (int iy = kreg.ry0; iy <= kreg.ry1; iy++)
            {
                for (int ix = kreg.rx0; ix <= kreg.rx1; ix++)
                {
                    wsum += array[kidx(kx, ky, ix, iy)];
                }
            }
            return wsum;
        }
        void divInKernel(decimal[] array, decimal value, int kx, int ky)
        {
            var kreg = KR(kx, ky);
            if (Math.Abs(value) < epsilon)
            {
                Console.WriteLine("|value| = " + Math.Abs(value));
                for (int iy = kreg.ry0; iy <= kreg.ry1; iy++)
                {
                    for (int ix = kreg.rx0; ix <= kreg.rx1; ix++)
                    {
                        array[kidx(kx, ky, ix, iy)] = 0.0m;
                    }
                }
            }
            else
            {
                for (int iy = kreg.ry0; iy <= kreg.ry1; iy++)
                {
                    for (int ix = kreg.rx0; ix <= kreg.rx1; ix++)
                    {
                        array[kidx(kx, ky, ix, iy)] /= value;
                    }
                }
            }
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
                    var kreg = KR(kx, ky);
                    for (int iy = kreg.ry0; iy <= kreg.ry1; iy++)
                    {
                        for (int ix = kreg.rx0; ix <= kreg.rx1; ix++)
                        {
                            int i = ix + iy * wi;
                            var pi_uk = new Vec2m(ix - m[kreg.k].x, iy - m[kreg.k].y);
                            var Skinv = S[kreg.k].Inverse();
                            decimal d = -0.5m * (pi_uk * Skinv * pi_uk) - Vec3m.DistanceSqr(c[i], v[kreg.k]) / (2 * s[kreg.k] * s[kreg.k]);

                            // 微妙？
                            // TODO
                            d = Math.Min(d, 700);

                            w[kidx(kx, ky, ix, iy)] = (decimal)Math.Exp((double)d);
                            //System.Diagnostics.Debug.Assert(false == decimal.IsNaN(w[kidx(kx, ky, ix, iy)]));
                            //System.Diagnostics.Debug.Assert(false == decimal.IsInfinity(w[kidx(kx, ky, ix, iy)]));

                            // save i -> k
                            i2k[i].Add(kx);
                            i2k[i].Add(ky);
                        }
                    }
                    decimal wsum = sumInKernel(w, kx, ky);
                    divInKernel(w, wsum, kx, ky);
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
                    decimal wsum = 0.0m;
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
//                            System.Diagnostics.Debug.Assert(false == decimal.IsNaN(g[ki]));
                        }
                    }
                }
            }
#if ENABLE_MULTITHREADING
                );
#endif
        }

        const decimal epsilon = 1e-102m;
        /// <returns>Has changed in M-Step</returns>
        bool MStep()
        {
            // compute all kernels
#if ENABLE_MULTITHREADING
                System.Threading.Tasks.Parallel.For(0, ho, (ky) =>
#else
            for (int ky = 0; ky < ho; ky++)
#endif
            {
                for (int kx = 0; kx < wo; kx++)
                {
                    var set = KR(kx, ky);
                    decimal gsum = sumInKernel(g, kx, ky);

                    var mk = m[set.k];
                    S[set.k] = new Mat2x2m(0, 0, 0, 0);
                    for (int iy = set.ry0; iy <= set.ry1; iy++)
                    {
                        for (int ix = set.rx0; ix <= set.rx1; ix++)
                        {
                            decimal gki = g[kidx(kx, ky, ix, iy)];
                            var d = new Vec2m(ix - mk.x, iy - mk.y);
                            S[set.k] += gki * Mat2x2m.FromVecVec(d, d);
//                            System.Diagnostics.Debug.Assert(false == decimal.IsNaN(S[set.k].m11));
                        }
                    }
                    if (Math.Abs(gsum) > epsilon)
                    {
                        S[set.k] /= gsum;
                    }
                    else
                    {
                        Console.WriteLine("|gsum| = " + Math.Abs(gsum));
                    }
  //                  System.Diagnostics.Debug.Assert(false == decimal.IsNaN(S[set.k].m11));

                    m[set.k] = new Vec2m(0, 0);
                    for (int iy = set.ry0; iy <= set.ry1; iy++)
                    {
                        for (int ix = set.rx0; ix <= set.rx1; ix++)
                        {
                            decimal gki = g[kidx(kx, ky, ix, iy)];
                            m[set.k] += gki * new Vec2m(ix, iy);
                        }
                    }
                    if (Math.Abs(gsum) > epsilon)
                        m[set.k] /= gsum;

                    v[set.k] = new Vec3m(0, 0, 0);
                    for (int iy = set.ry0; iy <= set.ry1; iy++)
                    {
                        for (int ix = set.rx0; ix <= set.rx1; ix++)
                        {
                            decimal gki = g[kidx(kx, ky, ix, iy)];
                            int i = ix + iy * wi;
                            v[set.k] += gki * c[i];
                        }
                    }
                    if (Math.Abs(gsum) > epsilon)
                        v[set.k] /= gsum;
                }
            }
#if ENABLE_MULTITHREADING
                );
#endif
            return true;
        }

        decimal clamp(decimal v, decimal min, decimal max)
        {
            return v < min ? min : v > max ? max : v;
        }

        /// <returns>Has changed in C-Step</returns>
        bool CStep()
        {
            try
            {
                // Spatial constraints
                var mAve = new Vec2m[wo * ho];
                for (int ky = 0; ky < ho; ky++)
                {
                    for (int kx = 0; kx < wo; kx++)
                    {
                        int k = kx + ky * wo;
                        mAve[k] = new Vec2m(0, 0);
                        int cnt = 0;
                        if (0 <= kx - 1 && 0 <= ky - 1)
                        {
                            mAve[k] += m[(kx - 1) + (ky - 1) * wo];
                            cnt++;
                        }
                        if (0 <= kx - 1 && ky + 1 < ho)
                        {
                            mAve[k] += m[(kx - 1) + (ky + 1) * wo];
                            cnt++;
                        }
                        if (kx + 1 < wo && 0 <= ky - 1)
                        {
                            mAve[k] += m[(kx + 1) + (ky - 1) * wo];
                            cnt++;
                        }
                        if (kx + 1 < wo && ky + 1 < ho)
                        {
                            mAve[k] += m[(kx + 1) + (ky + 1) * wo];
                            cnt++;
                        }
                        mAve[k] /= cnt;
                    }
                }
                for (int ky = 0; ky < ho; ky++)
                {
                    for (int kx = 0; kx < wo; kx++)
                    {
                        int k = kx + ky * wo;
                        m[k] = 0.5m * (m[k] + mAve[k]);
                        decimal x0 = rx * (kx + 0.5m) - rx / 4;
                        decimal x1 = rx * (kx + 0.5m) + rx / 4;
                        decimal y0 = ry * (ky + 0.5m) - ry / 4;
                        decimal y1 = ry * (ky + 0.5m) + ry / 4;
                        m[k].x = clamp(m[k].x, x0, x1);
                        m[k].y = clamp(m[k].y, y0, y1);
                    }
                }

                // Constrain spatial variance
                for (int k = 0; k < ho * wo; k++)
                {
                    Mat2x2m _U, _S, _Vt;
                    S[k].SVD(out _U, out _S, out _Vt);
                    _S.m11 = clamp(_S.m11, 0.05m, 0.1m);
                    _S.m22 = clamp(_S.m22, 0.05m, 0.1m);
                    S[k] = _U * _S * _Vt;

//                    System.Diagnostics.Debug.Assert(decimal.IsNaN(S[k].m11) == false);
                }

                // Shape constraints

                for (int ky = 0; ky < ho; ky++)
                {
                    for (int kx = 0; kx < wo; kx++)
                    {
                        var set = KR(kx, ky);
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

                                var d = new Vec2m(nx - kx, ny - ky);

                                decimal _s = 0;
                                for (int iy = set.ry0; iy <= set.ry1; iy++)
                                {
                                    for (int ix = set.rx0; ix <= set.rx1; ix++)
                                    {
                                        int i = ix + iy * wi;

                                        decimal dpmx = ix - m[set.k].x;
                                        decimal dpmy = iy - m[set.k].y;
                                        decimal val = Math.Max(0, dpmx * d.x + dpmy * d.y);
                                        _s += g[kidx(kx, ky, ix, iy)] * val * val;
                                    }
                                }

                                decimal _f = 0.0m;
                                for (int iy = set.ry0; iy <= set.ry1; iy++)
                                {
                                    for (int ix = set.rx0; ix <= set.rx1; ix++)
                                    {
                                        int i = ix + iy * wi;

                                        decimal dpmx = ix - m[set.k].x;
                                        decimal dpmy = iy - m[set.k].y;
                                        decimal val = Math.Max(0, dpmx * d.x + dpmy * d.y);
                                        int nidx = kidx(nx, ny, ix, iy);
                                        _f += g[kidx(kx, ky, ix, iy)] * (nidx < 0 ? 0.0m : g[nidx]);
                                    }
                                }

                                var o = new Vec2m(0, 0);
                                for (int iy = set.ry0 + 1; iy <= set.ry1; iy++)
                                {
                                    for (int ix = set.rx0 + 1; ix <= set.rx1; ix++)
                                    {
                                        int nidx01 = kidx(nx, ny, ix - 1, iy);
                                        decimal gndix01 = nidx01 < 0 ? 0.0m : g[nidx01];
                                        int nidx10 = kidx(nx, ny, ix, iy - 1);
                                        decimal gnidx10 = nidx10 < 0 ? 0.0m : g[nidx10];
                                        int nidx11 = kidx(nx, ny, ix, iy);
                                        decimal gnidx11 = nidx11 < 0 ? 0.0m : g[nidx11];
                                        decimal val01 = g[kidx(kx, ky, ix - 1, iy)] / (g[kidx(kx, ky, ix - 1, iy)] + gndix01);
                                        decimal val10 = g[kidx(kx, ky, ix, iy - 1)] / (g[kidx(kx, ky, ix, iy - 1)] + gnidx10);
                                        decimal val11 = g[kidx(kx, ky, ix, iy)] / (g[kidx(kx, ky, ix, iy)] + gnidx11);
                                        o.x += val11 - val01;
                                        o.y += val11 - val10;
                                    }
                                }

                                var cos = o.NormalSafe() * d.NormalSafe();

                                if (_s > 0.2m * rx || (_f < 0.08m && (double)cos < Math.Cos(Math.PI * 25 / 180)))
                                {
                                    s[set.k] = 1.1m * s[set.k];
                                    s[n] = 1.1m * s[n];
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
}