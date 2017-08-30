using Microsoft.VisualStudio.TestTools.UnitTesting;
using downscaling_winform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace downscaling_winform.Form1Tests
{
    [TestClass()]
    public class ContentAdaptiveDownscaleTests
    {
        [TestMethod()]
        public void kidxTest()
        {
            using (var input = new Bitmap(400, 400, PixelFormat.Format32bppArgb))
            using (var output = new Bitmap(40, 40, PixelFormat.Format32bppArgb))
            {
                var inst = new Form1.ContentAdaptiveDownscale();
                inst.initialize(input, output);
                Assert.AreEqual(-1, inst.kidx(-1, -1, 0, 0));
                Assert.AreEqual(-1, inst.kidx(0, 0, -1, -1));
                Assert.AreEqual(630, inst.kidx(0, 0, 0, 0));
                Assert.AreEqual(41 * 24 + 24, inst.kidx(0, 0, 9, 9));
                Assert.AreEqual(41 * 41 - 1, inst.kidx(0, 0, 25, 25));
                Assert.AreEqual(-1, inst.kidx(0, 0, 26, 25));
                Assert.AreEqual(-1, inst.kidx(0, 0, 26, 26));
                Assert.AreEqual(41 * 41 * 122 + 41 * 2 + 3, inst.kidx(2, 3, 8, 17));
            }
        }
    }
}