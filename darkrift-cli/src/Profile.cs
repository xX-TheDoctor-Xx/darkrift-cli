using Newtonsoft.Json;
using System.IO;

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
            // Should this be called project.json? Im switching it to profile.json
            var path = Path.Combine(Config.USER_DR_DIR, "project.json");
            var profilePath = Path.Combine(Config.USER_DR_DIR, "profile.json");
            if (File.Exists(path))
            {
                // Converts the project.json to profile.json
                var text = File.ReadAllText(path);
                File.WriteAllText(profilePath, text);
                File.Delete(path);

                var project = JsonConvert.DeserializeObject<ProfileNotStatic>(text);
                mapStaticClass(project);
            }
            else if (File.Exists(profilePath))
            {
                var text = File.ReadAllText(profilePath);
                var project = JsonConvert.DeserializeObject<ProfileNotStatic>(text);
                mapStaticClass(project);
            }
        }

        /// <summary>
        /// Saves any edits to the user's profile to disk.
        /// </summary>
        public static void Save()
        {
            var text = JsonConvert.SerializeObject(mapNotStaticClass());
            File.WriteAllText(Path.Combine(Config.USER_DR_DIR, "project.json"), text);
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
