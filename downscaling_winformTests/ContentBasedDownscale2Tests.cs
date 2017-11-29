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
        [TestMethod()]
        public void initializeTest()
        {
            var downscaler = new ContentBasedDownscale2();
        }
    }
}