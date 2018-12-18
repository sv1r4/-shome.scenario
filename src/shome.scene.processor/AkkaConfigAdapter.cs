using System;
using System.Collections.Generic;
using System.Text;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace shome.scene.processor
{
    internal class AkkaConfigAdapter
    {
        private readonly IConfigurationRoot _config;

        private static readonly IDictionary<LogLevel, string> MsAkkaLogLevelMap = new Dictionary<LogLevel, string>
        {
            {LogLevel.Debug, "DEBUG"},
            {LogLevel.Trace, "DEBUG"},
            {LogLevel.Information, "INFO"},
            {LogLevel.Warning, "WARN"},
            {LogLevel.Error, "ERROR"},
            {LogLevel.Critical, "ERROR"},
            {LogLevel.None, "ERROR"}
        };


        public AkkaConfigAdapter(IConfigurationRoot config)
        {
            _config = config;
        }


        public Config GetAkkaConfig()
        {
            var logConfig = _config.GetSection("Logging");
            var logLevels = logConfig.GetSection("LogLevel").Get<IDictionary<string, LogLevel>>();

            if (!logLevels.TryGetValue("Akka", out var msAkkaLevel))
            {
                if (!logLevels.TryGetValue("Default", out msAkkaLevel))
                {
                    msAkkaLevel = LogLevel.Debug;
                }
            }

            var config = ConfigurationFactory.ParseString($@"
  akka{{
    stdout-loglevel = ""{MsAkkaLogLevelMap[msAkkaLevel]}""
    loglevel = ""{MsAkkaLogLevelMap[msAkkaLevel]}""
  }}")
                .WithFallback(ConfigurationFactory.Default());
            return config;
        }

    }
}
