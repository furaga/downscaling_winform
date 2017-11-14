using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib.ContenteBaseDownscaleUtils
{
    internal class Config
    {
        public int wi, hi, wo, ho;
        public decimal rx, ry;
        public Config(int wi, int hi, int wo, int ho)
        {
            this.wi = wi;
            this.hi = hi;
            this.wo = wo;
            this.ho = ho;
            this.rx = (decimal)wi / wo;
            this.ry = (decimal)hi / ho;
        }
    }

    internal class Position
    {
        public int x, y;
        public int index;
        public void Set(Config config, int xi, int yi)
        {
            this.x = xi;
            this.y = yi;
            this.index = x + y * config.wi;
        }
    }

    internal class Kernel
    {
        public int x, y;
        public int index;
        public void Set(Config config, int xo, int yo)
        {
            this.x = xo;
            this.y = yo;
            this.index = x + y * config.wo;
        }
    }

    internal class For
    {
        static internal void AllKernels(Config config, Action<Config, Kernel> fnc)
        {
            Kernel k = new Kernel();
            for (int y = 0; y < config.ho; y++)
            {
                for (int x = 0; x < config.wo; x++)
                {
                    k.Set(config, x, y);
                    fnc(config, k);
                }
            }
        }

        static internal void AllInRegion(Config config, Kernel k, Action<Config, Position> fnc)
        {
            int baseX = (int)(k.x * config.rx);
            int baseY = (int)(k.y * config.ry);
            Position i = new Position();
            for (int dy = (int)(-2 * config.ry + 1); dy <= (int)(2 * config.ry - 1); dy++)
            {
                int y = baseY + dy;
                for (int dx = (int)(-2 * config.rx + 1); dx <= (int)(2 * config.rx - 1); dx++)
                {
                    int x = baseX + dx;
                    i.Set(config, x, y);
                    fnc(config, i);
                }
            }
        }
    }
}