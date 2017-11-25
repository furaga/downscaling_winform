using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLib.ContenteBaseDownscaleUtils
{
    public class Config
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

    public class Position
    {
        public Vec2m p;
        public int index;
        public void Set(Config config, int xi, int yi)
        {
            this.p = new Vec2m(xi, yi);
            this.index = xi + yi * config.wi;
        }

        public Position()
        {
        }

        public Position(Config config, int xi, int yi)
        {
            Set(config, xi, yi);
        }

    }

    public class Kernel
    {
        public int x, y;
        public int index;

        public Kernel()
        {
        }

        public Kernel(Config config, int xo, int yo)
        {
            Set(config, xo, yo);
        }

        public void Set(Config config, int xo, int yo)
        {
            this.x = xo;
            this.y = yo;
            this.index = x + y * config.wo;
        }
    }

    public class For
    {
        static public void AllKernels(Config config, Action<Config, Kernel> fnc)
        {
            Kernel k = new Kernel();
            for (int ky = 0; ky < config.ho; ky++)
            {
                for (int kx = 0; kx < config.wo; kx++)
                {
                    k.Set(config, kx, ky);
                    fnc(config, k);
                }
            }
        }

        static public void AllPixels(Config config, Action<Config, Position> fnc)
        {
            Position i = new Position();
            for (int y = 0; y < config.hi; y++)
            {
                for (int x = 0; x < config.wi; x++)
                {
                    i.Set(config, x, y);
                    fnc(config, i);
                }
            }
        }

        static public void AllKernelOfPixel(Config config, List<Kernel>[] i2k, Position i, Action<Config, Kernel> fnc)
        {
            var i2ki = i2k[i.index];
            for (int j = 0; j < i2ki.Count; j += 2)
            {
                fnc(config, i2ki[j]);
            }
        }

        static public void AllPixeelsOfRegion(Config config, Kernel k, Action<Config, Position> fnc)
        {
            int baseX = (int)((k.x + 0.5m) * config.rx);
            int baseY = (int)((k.y + 0.5m) * config.ry);
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

        static public void AllKernelOfPixel(Config config, Position i, Action<Config, Kernel> fnc)
        {
            var xo = (int)(i.p.x / config.rx);
            var yo = (int)(i.p.y / config.ry);

            var rx2 = 2m * config.rx;
            var ry2 = 2m * config.ry;

            Kernel k = new Kernel();
            for (int ky = yo - 2; ky <= yo + 2; ky++)
            {
                for (int kx = xo - 2; kx <= xo + 2; kx++)
                {
                    var x = (kx + 0.5m) * config.rx;
                    var y = (ky + 0.5m) * config.ry;
                    var dx = i.p.x - x;
                    var dy = i.p.y - y;
                    if (-rx2 < dx && dx < rx2 && -ry2 < dy && dy < ry2)
                    {
                        k.Set(config, kx, ky);
                        fnc(config, k);
                    }
                }
            }
        }

    }
}