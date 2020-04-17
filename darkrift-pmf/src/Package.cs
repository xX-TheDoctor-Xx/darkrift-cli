using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DarkRift.PMF
{
    public class Package
    {
        public string ID { get; set; }

        [JsonConverter(typeof(StringEnumConverter))] // This converts enum to string and vice versa when generating or parsing json
        public PackageType Type { get; set; }

        public string Name { get; set; }

        public string Author { get; set; }

        public string Description { get; set; }

        public List<Asset> Assets { get; set; } // If the package is a local one the list will only have one asset which is the version installed

        public Asset GetAssetVersion(Version version)
        {
            if (version == null)
                throw new ArgumentNullException();

            foreach (var asset in Assets)
            {
                if (asset.Version == version)
                    return asset;
            }

            return null;
        }

        // A valid package must have:
        //      - an id
        //      - a type
        //      - a name
        //      - an author
        //      - a description
        //      - at least one asset
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ID) &&
                   Type != PackageType.None &&
                   !string.IsNullOrEmpty(Name) &&
                   !string.IsNullOrEmpty(Author) &&
                   !string.IsNullOrEmpty(Description) &&
                   Assets.Count > 0;
        }
    }
}
