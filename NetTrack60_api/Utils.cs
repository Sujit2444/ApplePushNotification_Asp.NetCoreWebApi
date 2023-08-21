using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetTrack60_api
{
    public static class Utils
    {
        private static string _logFilePath;
    
        public static void LogToFile(string messageType,string message)
        {
            Object _object = new object();
            IConfigurationBuilder builder = new ConfigurationBuilder()
                                   .AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();
            var path = configuration["appsettings:LogFileLocation"];
            _logFilePath = configuration["appsettings:LogFileLocation"] + DateTime.Now.ToString("yyyyMMdd") + configuration["appsettings:LogFileName"];
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    File.Create(_logFilePath).Close();
                }

                lock (_object)
                {
                    using (var sw = new StreamWriter(_logFilePath, true))
                    {
                        sw.WriteLine($"{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.ffff tt ")} | {messageType} | {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_object)
                {
                    using (var sw = new StreamWriter(_logFilePath, true))
                    {
                        sw.WriteLine($"{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.ffff tt ")} | [EXCEPTION] | Exception LogToFile(): { ex.Message}");
                    }
                }
                
            }
        }

    }
}
