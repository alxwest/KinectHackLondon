using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectWPFOpenCV.Service
{
    class CombineHarvester
    {
        private List<Models.Possible> possibles = new List<Models.Possible>();

        public void Harvest(Models.Grain grain)
        {
            bool ok = false;
            foreach(var possible in possibles)
            {
                if (CloseColor(possible.MedianColor, grain.Color)
                    && Near(possible.Grains.Select(x => x.Point).ToArray(), grain.Point))
                {
                    possible.Grains.Add(grain);
                    if (grain.Point.Depth < possible.Nearest.Point.Depth)
                    {
                        possible.Nearest = grain;
                    }

                    ok = true;
                }
            }
            if (!ok)
            {
                possibles.Add(new Models.Possible
                {
                    Grains = new List<Models.Grain>(new Models.Grain[]{grain}),
                    MedianColor = grain.Color,
                    Nearest = grain
                });
            }
        }

        private bool CloseColor(System.Drawing.Color a, System.Drawing.Color b)
        {
            return true;
        }

        //private System.Drawing.Color MedianColor(System.Drawing.Color[] colors)
        //{
        //    return System.Drawing.Color.FromArgb(colors.Select(x=>x.A).Average(),
        //}

        private bool Near(Microsoft.Kinect.DepthImagePoint[] points, Microsoft.Kinect.DepthImagePoint p)
        {
            return true;
        }
    }
}
