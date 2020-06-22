using PMF;
using Newtonsoft.Json;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DarkRift.Cli
{
    public static class Project
    {
        private class ProjectNotStatic
        {
            public Runtime Runtime { get; set; }
        }

        /// <summary>
        ///     The runtime settings.
        /// </summary>
        public static Runtime Runtime { get; set; }

        public static bool Loaded { get; set; }

        /// <summary>
        /// Load's the project from disk.
        /// </summary>
        /// <returns>The project.</returns>
        public static void Load()
        {
            // Checks for project.json in current folder
            if (File.Exists("project.json"))
            {
                var project = JsonConvert.DeserializeObject<ProjectNotStatic>(File.ReadAllText("project.json"));
                mapStaticClass(project);
                Loaded = true;
            }
            else
                Runtime = new Runtime();
        }

        /// <summary>
        /// Saves any edits to the project to disk.
        /// </summary>
        public static void Save(string path)
        {
            var text = JsonConvert.SerializeObject(mapNotStaticClass());
            File.WriteAllText(Path.Combine(path, "project.json"), text);
        }

        private static void mapStaticClass(ProjectNotStatic pns)
        {
            Runtime = pns.Runtime;
        }

        private static ProjectNotStatic mapNotStaticClass()
        {
            var pns = new ProjectNotStatic
            {
                Runtime = Runtime
            };
            return pns;
        }
    }
}
