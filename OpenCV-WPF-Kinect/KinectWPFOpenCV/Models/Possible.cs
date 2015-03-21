using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectWPFOpenCV.Models
{
    class Possible
    {
        public List<Grain> Grains = new List<Grain>();
        public Grain Nearest { get; set; }
        public System.Drawing.Color MedianColor { get; set; }
    }
}
