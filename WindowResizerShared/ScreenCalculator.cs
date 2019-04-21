using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowResizerShared
{
    public class ScreenCalculator
    {
        public ScreenCalculator()
        {

        }

        public int ComputeForX(int screenWidth, SystemRect systemRect, int xPos)
        {
            return xPos + ((screenWidth - ComputeForWindowLength(systemRect.Right, systemRect.Left)) / 2);
        }

        public int ComputeForY(int screenHeight, SystemRect systemRect, int yPos)
        {
            return yPos + ((screenHeight - ComputeForWindowLength(systemRect.Bottom, systemRect.Top)) / 2);
        }

        public int ComputeForWindowLength(int windowOuter, int windowInner)
        {
            return windowOuter - windowInner;
        }

        public SystemRect ChangeWindowSize(int screenWidth, int screenHeight, double percentageWidth, double percentageHeight)
        {
            return new SystemRect
            {
                Left = 0,
                Top = 0,
                Right = (int)(screenWidth * percentageWidth),
                Bottom = (int)(screenHeight * percentageHeight)
            };
        }
    }
}
