using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace fpn_scale_api.Helpers
  {
  public static class ConfigReader
    {
    private static readonly List<string> RequiredKeys = new List<string>
        {
            "PortMapping",
            "SampleSize",
            "TimeoutMilliseconds",
            "DefaultBaudRate",
            "ErrorTolerance",
            "HttpUrl",
            "StandardDataPattern"
        };

    public static Dictionary<string, string> ReadConfig(string filePath, ILogger logger)
      {
      var config = new Dictionary<string, string>();

      foreach (var line in File.ReadLines(filePath))
        {
        var parts = line.Split(new[] { '=' }, 2);  // Split on the first '=' only
        if (parts.Length == 2)
          {
          config[parts[0].Trim()] = parts[1].Trim();
          }
        }

      foreach (var key in RequiredKeys)
        {
        if (!config.ContainsKey(key))
          {
          logger.LogError("[ERROR] Missing required configuration key: {key}", key);
          throw new ArgumentException($"Missing required configuration key: {key}");
          }
        }

      return config;
      }

    public static Dictionary<string, string> ParsePortMapping(string mappingString)
      {
      return mappingString.Split(',')
                          .Select(part => part.Split(':'))
                          .ToDictionary(split => split[0], split => split[1]);
      }
    }
  }
