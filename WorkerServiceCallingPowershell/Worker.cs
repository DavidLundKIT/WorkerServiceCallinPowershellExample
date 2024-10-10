using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace WorkerServiceCallingPowershell
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WSCP starting at: {time}", DateTime.Now);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WSCP stopping at: {time}", DateTime.Now);
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("WSCP running at: {time}", DateTimeOffset.Now);
                }

                await CallPowershellTask(1234, true);
                await Task.Delay(1000, stoppingToken);
                await CallPowershellTask(1234, false);
                _logger.LogInformation("WSCP sleeping for 20 sec. at: {time}", DateTimeOffset.Now);
                await Task.Delay(20000, stoppingToken);
            }
        }

        public async Task<bool> CallPowershellTask(int companyID, bool asPath)
        {
            try
            {
                InitialSessionState iss = InitialSessionState.CreateDefault();
                iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

                Runspace runspace = RunspaceFactory.CreateRunspace(iss);
                runspace.Open();

                using PowerShell powershell = PowerShell.Create();
                powershell.Runspace = runspace;
                string script = @"C:\Work\psbin\see_quartr_apikeys.ps1";

                if (!File.Exists(script))
                {
                    throw new Exception($"Can't find file: {script}");
                }
                if (asPath)
                {
                    // path to file
                    _logger.LogInformation("Calling powershell with path: {path}", script);
                    powershell.AddScript(script);
                }
                else
                {
                    // read in the script source
                    _logger.LogInformation("Calling powershell with the source from path: {path}", script);
                    string scriptSrc = File.ReadAllText(script);
                    powershell.AddScript(scriptSrc);
                }
                powershell.AddParameter("companyid", companyID);
                var processes = await powershell.InvokeAsync();

                bool result = powershell.HadErrors;
                if (powershell.HadErrors)
                {
                    _logger.LogError("Powershell error.");
                    if (powershell.Streams.Error.Count > 0)
                    {
                        foreach (ErrorRecord errorRecord in powershell.Streams.Error)
                        {
                            _logger.LogError(errorRecord.Exception, errorRecord.Exception.Message);
                        }
                    }
                }
                runspace.Close();
                _logger.LogInformation("Powershell executed for company {id}, asPath: {asPath}, result(hadErrors): {result}", companyID, asPath, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CallPowershellTask");
                return false;
            }
        }
    }
}
