using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowResizerShared;

namespace WindowResizer.Tests
{
    [TestClass]
    public class ScreenCalculatorTests
    {
        private ScreenCalculator _screenCalculator;
        
        public ScreenCalculatorTests()
        {
            _screenCalculator = new ScreenCalculator();
        }

        [TestMethod]
        public void ComputeForX_Test()
        {
            SystemRect rect = new SystemRect
            {
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 400,
            };

            int x = _screenCalculator.ComputeForX(1920, rect, 0);

            Assert.AreEqual(x, 560);
        }

        [TestMethod]
        public void ComputeForY_Test()
        {
            SystemRect rect = new SystemRect
            {
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 400,
            };

            int y = _screenCalculator.ComputeForY(1040, rect, 0);

            Assert.AreEqual(y, 320);
        }

        [TestMethod]
        public void ComputeForWindowLength_Test()
        {
            SystemRect rect = new SystemRect
            {
                Left = 0,
                Top = 0,
                Right = 800,
                Bottom = 400,
            };

            int length = _screenCalculator.ComputeForWindowLength(rect.Right, rect.Left);

            Assert.AreEqual(length, 800);
        }
    }
}
