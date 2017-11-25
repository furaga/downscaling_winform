using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLib.ContenteBaseDownscaleUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib.ContenteBaseDownscaleUtils.Tests
{
    [TestClass()]
    public class ForTests
    {
        [TestMethod()]
        public void AllKernelsTest()
        {
            var config = new Config(4, 4, 2, 2);
            int counter = 0;
            For.AllKernels(config, (c, k) => { counter++; });
            Assert.AreEqual(counter, 4);
        }

        [TestMethod()]
        public void AllPixelsTest()
        {
            var config = new Config(4, 4, 2, 2);
            int counter = 0;
            For.AllPixels(config, (c, p) => { counter++; });
            Assert.AreEqual(counter, 16);
        }

        [TestMethod()]
        public void AllPixeelsOfRegion()
        {
            var config = new Config(6, 6, 3, 3);

            var kernel = new Kernel(config, 1, 1);
            int counter = 0;
            For.AllPixeelsOfRegion(config, kernel, (c, p) => { counter++; });
            Assert.AreEqual(counter, 49);
        }

        [TestMethod()]
        public void AllPixeelsOfRegion2()
        {
            var config = new Config(6, 6, 2, 2);
            var kernel = new Kernel(config, 1, 0);
            int counter = 0;
            For.AllPixeelsOfRegion(config, kernel, (c, p) => { counter++; });
            Assert.AreEqual(counter, 121);
        }

        [TestMethod()]
        public void AllKernelOfPixel()
        {
            var config = new Config(6, 6, 3, 3);
            var position = new Position(config, 2, 3);
            var answer = new[]
            {
                new [] { -1, 0 },
                new [] { 0, 0 },
                new [] { 1, 0 },
                new [] { 2, 0 },
                new [] { -1, 1 },
                new [] { 0, 1 },
                new [] { 1, 1 },
                new [] { 2, 1 },
                new [] { -1, 2 },
                new [] { 0, 2 },
                new [] { 1, 2 },
                new [] { 2, 2 },
            };

            int counter = 0;
            For.AllKernelOfPixel(config, position, (c, k) => {
                Assert.AreEqual(k.x, answer[counter][0]);
                Assert.AreEqual(k.y, answer[counter][1]);
                counter++;
            });

            Assert.AreEqual(counter, answer.Length);
        }
    }
}