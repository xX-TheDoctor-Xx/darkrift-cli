using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DarkRift.Cli
{
    public static class Config
    {
        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        public static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");
    }
}
