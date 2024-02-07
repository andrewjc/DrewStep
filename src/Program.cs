// import windows apis used to enumerate open windows from other processes, used to create a taskbar
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// import win32 apis using interop


namespace ConsoleApp
{
    class Program
    {




        [STAThread]
        static void Main(string[] args)
        {
            var configPath = "config.yaml"; // Adjust the path to where your config.yaml is located
            var config = AppConfigManager.LoadConfig(configPath);

            var app = new System.Windows.Application();
            var taskbar = new Taskbar(config);
            taskbar.Create();
            app.Run();
        }
    }



}

