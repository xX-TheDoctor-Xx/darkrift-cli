using DarkRift.PMF;
using HttpMock;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace pmf_test
{
    class Program
    {
        static string BuildJsonObject()
        {
            //Package package = Package.Parse("{ \"ID\": \"webrtcplugin\", \"Type\": \"Plugin\", \"Name\": \"WebRTC Plugin\", \"Author\": \"Sourcey\", \"Description\": \"This plugin provides Spot with WebRTC support, so you can view high quality video surveillance streams in a modern web browser.\", \"Assets\": [{ \"Version\": \"0.1.1\", \"SdkVersion\": \"0.6.2\", \"Checksum\": \"068ffaddc290d0fb1caf22cbd0e4d909\", \"FileName\": \"webrtcplugin-0.1.1-sdk-0.6.2-win32.zip\", \"FileSize\": 0, \"Url\": \"http stuff\" }] }");
            //return JsonConvert.SerializeObject(package);
            return "";
        }

        static void Main(string[] args)
        {
            var _stubHttp = HttpMockRepository.At("http://localhost:9191");

            _stubHttp.Stub(x => x.Get("/endpoint"))
                .Return(BuildJsonObject())
                .OK();

            Console.ReadKey();
        }
    }
}
