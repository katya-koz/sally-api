using Serilog;

namespace SALLY_API
{
    public static class GlobalLogger
    {
      
        public static Serilog.ILogger Logger { get; private set; }

        public static void Initialize(string logFilePath)
        {
            
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() 
                .WriteTo.Console() 
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day) 
                .CreateLogger();
        }

        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
    }
}
