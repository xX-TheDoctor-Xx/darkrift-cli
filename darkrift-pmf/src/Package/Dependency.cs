using System;
using System.Collections.Generic;
using System.Text;

namespace DarkRift.PMF
{
    public class Dependency
    {
        public string Checksum { get; set; }

        public string FileName { get; set; }

        public double FileSize { get; set; }

        public string Url { get; set; }
    }
}
