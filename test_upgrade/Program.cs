using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace test_upgrade
{
    internal class Program
    {
        public static string GetApplicationRoot()
        {
            var exePath = Path.GetDirectoryName(System.Reflection
                              .Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;
            return appRoot;
        }

        private static void Main(string[] args)
        {
            try
            {
                //File.Delete("test.upgrade.dll");

                string path = Path.Combine(GetApplicationRoot(), "test_upgrade.dll");
                Console.WriteLine(path);
                File.Move("C:\\Users\\Utilizador\\Desktop\\darkrift-cli\\test_upgrade\\bin\\Debug\\test_upgrade.dll", path, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("nope");
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                    Console.WriteLine(e.InnerException.Message);
            }

            Console.ReadKey();
        }
    }
}
