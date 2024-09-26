using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using System.Runtime.InteropServices;

namespace WorkerServiceCallingPowershell
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .UseWindowsService(opts =>
                {
                    opts.ServiceName = "WorkerServiceCallingPowershell";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
                    }
                    else
                    {
                        // only windows
                        throw new Exception("OS Platform is not Windows");
                    }
                    services.AddLogging(builder =>
                    {
                        builder.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    });

                    services.AddOptions();
                    services.AddHostedService<Worker>();
                });

            var host = builder.Build();
            host.Run();
        }
    }
}
   