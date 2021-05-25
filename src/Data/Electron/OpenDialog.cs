using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PhotoOrganizer.Data.Electron {
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class OpenDialogOption {
        public string Title { get; set; }
        public string DefaultPath { get; set; }
        public string ButtonLabel { get; set; }
        public IEnumerable<FileFilter> Filters { get; set; }
        public IEnumerable<string> Properties { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class OpenDialogResult {
        public bool Canceled { get; set; }
        public IEnumerable<string> FilePaths { get; set; }
    }
}
