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

        // min position of kernel
        public int xi, yi;
        public int indexi; 

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
            this.index = xo + yo * config.wo;

            this.xi = (int)((xo) * config.rx);
            this.yi = (int)((yo) * config.ry);
            this.indexi = xi + yi * config.wi;
        }
    }

    public class For
    {
        static public int AllKernels(Config config, Action<Config, Kernel> fnc)
        {
            int counter = 0;
            Kernel k = new Kernel();
            for (int ky = 0; ky < config.ho; ky++)
            {
                for (int kx = 0; kx < config.wo; kx++)
                {
                    k.Set(config, kx, ky);
                    fnc(config, k);
                    counter++;
                }
            }
            return counter;
        }

        static public int AllPixels(Config config, Action<Config, Position> fnc)
        {
            int counter = 0;
            Position i = new Position();
            for (int y = 0; y < config.hi; y++)
            {
                for (int x = 0; x < config.wi; x++)
                {
                    i.Set(config, x, y);
                    fnc(config, i);
                    counter++;
                }
            }
            return counter;
        }

        static public int AllPixeelsOfRegion(Config config, Kernel k, Action<Config, Position> fnc)
        {
            int counter = 0;
            int baseX = (int)((k.x + 0.5m) * config.rx);
            int baseY = (int)((k.y + 0.5m) * config.ry);
            Position i = new Position();
            for (int dy = (int)(-2 * config.ry + 1); dy <= (int)(2 * config.ry - 1); dy++)
            {
                int y = baseY + dy;
                if(y < 0 || config.hi <= y)
                {
                    continue;
                }
                for (int dx = (int)(-2 * config.rx + 1); dx <= (int)(2 * config.rx - 1); dx++)
                {
                    int x = baseX + dx;
                    if (x < 0 || config.wi <= x)
                    {
                        continue;
                    }
                    i.Set(config, x, y);
                    fnc(config, i);
                    counter++;
                }
            }
            return counter;
        }

        static public int AllKernelOfPixel(Config config, Position i, Action<Config, Kernel> fnc)
        {
            int counter = 0;
            var xo = (int)(i.p.x / config.rx);
            var yo = (int)(i.p.y / config.ry);

            var rx2 = 2m * config.rx;
            var ry2 = 2m * config.ry;

            Kernel k = new Kernel();
            for (int ky = yo - 2; ky <= yo + 2; ky++)
            {
                if (ky < 0 || config.ho <= ky)
                {
                    continue;
                }
                for (int kx = xo - 2; kx <= xo + 2; kx++)
                {
                    if (kx < 0 || config.wo <= kx)
                    {
                        continue;
                    }
                    var x = (kx + 0.5m) * config.rx;
                    var y = (ky + 0.5m) * config.ry;
                    var dx = i.p.x - x;
                    var dy = i.p.y - y;
                    if (-rx2 < dx && dx < rx2 && -ry2 < dy && dy < ry2)
                    {
                        k.Set(config, kx, ky);
                        fnc(config, k);
                        counter++;
                    }
                }
            }
            return counter;
        }

    }
}