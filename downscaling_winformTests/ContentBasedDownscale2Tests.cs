using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib.Tests
{
    [TestClass()]
    public class ContentBasedDownscale2Tests
    {
        System.Drawing.Bitmap cols2bmp(Vec3m[] cols, int w, int h)
        {
            var bmp = new System.Drawing.Bitmap(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double r = cols[x + y * w].x;
                    double g = cols[x + y * w].y;
                    double b = cols[x + y * w].z;
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(255, (int)(255 * r), (int)(255 * g), (int)(255 * b)));
                }
            }
            return bmp;
        }

        System.Drawing.Bitmap createBitmap(int w, int h)
        {
            var cols = new Vec3m[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    cols[x + y * w] = new Vec3m(
                        (double)x / w,
                        (double)y / h,
                        1.0 - (double)x / w);
                }
            }
            return cols2bmp(cols, w, h);
        }


        [TestMethod()]
        public void initializeTest()
        {
            const int w = 16;
            const int h = 12;

            using (var bmp = createBitmap(w, h))
            {
                var config = new ContenteBaseDownscaleUtils.Config(w, h, 4, 4);
                var downscaler = new ContentBasedDownscale2();
                downscaler.AsDynamic().initialize(config, bmp);

                Assert.AreEqual(downscaler.AsDynamic().w_.Count, 16);
                foreach (var wk in downscaler.AsDynamic().w_)
                {
                    // rx = 4, ry = 3
                    // (4rx + 1) * (4ry + 1) = 17 * 13 = 221
                    Assert.AreEqual(wk.Length, 221);
                }

                Assert.AreEqual(downscaler.AsDynamic().g_.Count, 16);
                foreach (var gk in downscaler.AsDynamic().g_)
                {
                    Assert.AreEqual(gk.Length, 221);
                }

                Assert.AreEqual(downscaler.AsDynamic().m.Length, 16);
                foreach (var val in downscaler.AsDynamic().m)
                {
                    Assert.AreEqual(val.x % 4, 2.0, 1e-4);
                    Assert.AreEqual(val.y % 3, 1.5, 1e-4);
                }

                Assert.AreEqual(downscaler.AsDynamic().S.Length, 16);
                foreach (var val in downscaler.AsDynamic().S)
                {
                    Assert.AreEqual(val.m11, 1.3333333, 1e-4);
                    Assert.AreEqual(val.m12, 0);
                    Assert.AreEqual(val.m21, 0);
                    Assert.AreEqual(val.m22, 1.0, 1e-4);
                }

                Assert.AreEqual(downscaler.AsDynamic().v.Length, 16);
                foreach (var val in downscaler.AsDynamic().v)
                {
                    Assert.AreEqual(val.x, 0.5);
                    Assert.AreEqual(val.y, 0.5);
                    Assert.AreEqual(val.z, 0.5);
                }

                Assert.AreEqual(downscaler.AsDynamic().s.Length, 16);
                foreach (var val in downscaler.AsDynamic().s)
                {
                    Assert.AreEqual(val, 1e-4);
                }
            }
        }

        [TestMethod()]
        public void calcGaussianTest()
        {
            const int w = 4;
            const int h = 4;
            using (var bmp = createBitmap(w, h))
            {
                var config = new ContenteBaseDownscaleUtils.Config(w, h, 2, 2);
                var downscaler = new ContentBasedDownscale2();
                downscaler.AsDynamic().initialize(config, bmp);

                double val = Math.Max(-1e2, Math.Min(1e2, -0.5 * 3.0 - 1.5 * 1e8));
                var want = Math.Exp(val);
                var got = downscaler.AsDynamic().calcGaussian(
                    new ContenteBaseDownscaleUtils.Kernel(config, 0, 0),
                    new ContenteBaseDownscaleUtils.Position(config, 0, 0));
                Assert.AreEqual(want, got, 1e-8);
            }
        }

        [TestMethod()]
        public void eStepTest()
        {
            const int w = 4;
            const int h = 4;
            using (var bmp = createBitmap(w, h))
            {
                var config = new ContenteBaseDownscaleUtils.Config(w, h, 2, 2);
                var downscaler = new ContentBasedDownscale2();
                downscaler.AsDynamic().initialize(config, bmp);
                downscaler.AsDynamic().eStep(config);

                var ws = downscaler.AsDynamic().w_;

                Assert.IsTrue(ws[0][2 + 9 * 2] == 0);
                Assert.IsTrue(ws[0][2 + 9 * 3] == 0);
                Assert.IsTrue(ws[0][3 + 9 * 2] == 0);
                Assert.IsTrue(ws[0][3 + 9 * 3] > 0);

                double sum = 0;
                foreach (var val in ws[0])
                {
                    sum += val;
                }
                Assert.AreEqual(sum, 1.0, 1e-4);

                var gs = downscaler.AsDynamic().g_;
                Assert.IsTrue(gs[0][2 + 9 * 2] == 0);
                Assert.IsTrue(gs[0][2 + 9 * 3] == 0);
                Assert.IsTrue(gs[0][3 + 9 * 2] == 0);
                Assert.IsTrue(gs[0][3 + 9 * 3] > 0);
            }
        }

        [TestMethod()]
        public void mStepTest()
        {
            const int w = 16;
            const int h = 12;
            using (var bmp = createBitmap(w, h))
            {
                var config = new ContenteBaseDownscaleUtils.Config(w, h, 4, 4);
                var downscaler = new ContentBasedDownscale2();
                downscaler.AsDynamic().initialize(config, bmp);
                downscaler.AsDynamic().eStep(config);
                downscaler.AsDynamic().mStep(config);
            }
        }

        [TestMethod()]
        public void cStepTest()
        {
            const int w = 16;
            const int h = 12;
            using (var bmp = createBitmap(w, h))
            {
                var config = new ContenteBaseDownscaleUtils.Config(w, h, 4, 4);
                var downscaler = new ContentBasedDownscale2();
                downscaler.AsDynamic().initialize(config, bmp);
                downscaler.AsDynamic().eStep(config);
                downscaler.AsDynamic().mStep(config);
                downscaler.AsDynamic().cStep(config);
            }
        }
        
    }
}