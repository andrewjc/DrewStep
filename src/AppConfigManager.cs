using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.IO;

namespace ConsoleApp
{
    internal class AppConfigManager
    {

        public class AppConfig
        {
            public required List<string> BlacklistedClasses { get; set; }
            public required List<string> BlacklistedTitles { get; set; }
        }
        public static AppConfig LoadConfig(string filePath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance) // Adjust naming convention if necessary
                .Build();

            using (var reader = File.OpenText(filePath))
            {
                var config = deserializer.Deserialize<AppConfig>(reader);
                return config;
            }
        }
    }
}
