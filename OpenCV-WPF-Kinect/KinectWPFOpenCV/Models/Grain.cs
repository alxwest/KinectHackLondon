using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectWPFOpenCV.Models
{
    class Grain
    {
        public Grain(Microsoft.Kinect.DepthImagePoint point, System.Drawing.Color color)
        {
            Point = point;
            Color = color;
        }
        public Microsoft.Kinect.DepthImagePoint Point { get; private set; }
        public System.Drawing.Color Color { get; private set; }

    }
}
