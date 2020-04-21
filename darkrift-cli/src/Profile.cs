using Crayon;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DarkRift.Cli
{
    internal class ProfileNotStatic
    {
        public string InvoiceNumber { get; set; }
        public string LatestKnownDarkRiftVersion { get; set; }
    }

    /// <summary>
    /// Holds a user's profile settings.
    /// </summary>
    public static class Profile
    {
        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        private static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        /// <summary>
        /// The user's Unity Asset Store invoice number.
        /// </summary>
        public static string InvoiceNumber { get; set; }

        /// <summary>
        /// The latest version we know of for DarkRift.
        /// </summary>
        public static string LatestKnownDarkRiftVersion { get; set; }

        /// <summary>
        /// Load's the user's profile from disk.
        /// </summary>
        /// <returns>The user's profile.</returns>
        public static void Load()
        {
            var path = Path.Combine(USER_DR_DIR, "project.json");
            if (File.Exists(path))
            {
                var project = JsonConvert.DeserializeObject<ProfileNotStatic>(File.ReadAllText(path));
                mapStaticClass(project);
            }
        }

        /// <summary>
        /// Saves any edits to the user's profile to disk.
        /// </summary>
        public static void Save()
        {
            Directory.CreateDirectory(USER_DR_DIR);
            var text = JsonConvert.SerializeObject(mapNotStaticClass());
            File.WriteAllText(Path.Combine(USER_DR_DIR, "project.json"), text);
        }

        private static void mapStaticClass(ProfileNotStatic pns)
        {
            InvoiceNumber = pns.InvoiceNumber;
            LatestKnownDarkRiftVersion = pns.LatestKnownDarkRiftVersion;
        }

        private static ProfileNotStatic mapNotStaticClass()
        {
            var pns = new ProfileNotStatic
            {
                InvoiceNumber = InvoiceNumber,
                LatestKnownDarkRiftVersion = LatestKnownDarkRiftVersion
            };
            return pns;
        }
    }
}
