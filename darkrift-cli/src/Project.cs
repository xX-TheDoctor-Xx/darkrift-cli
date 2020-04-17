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

        /// <summary>
        /// Load's the project from disk.
        /// </summary>
        /// <returns>The project.</returns>
        public static void Load()
        {
            if (IsCurrentDirectoryAProject())
            {
                var project = JsonConvert.DeserializeObject<ProjectNotStatic>(File.ReadAllText("project.json"));
                mapStaticClass(project);
            }
            else
                Project.Runtime = new Runtime();
        }

        /// <summary>
        /// Saves any edits to the project to disk.
        /// </summary>
        public static void Save(string path)
        {
            var text = JsonConvert.SerializeObject(mapNotStaticClass());
            File.WriteAllText(Path.Combine(path, "project.json"), text);
        }

        /// <summary>
        /// Returns if the current directory is the directory where project is located
        /// by checking existence of Project.xml file.
        /// </summary>
        /// <returns>Returns if the current directory is a project directory</returns>
        public static bool IsCurrentDirectoryAProject()
        {
            return File.Exists("project.json");
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
