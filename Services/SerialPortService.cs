using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fpn_scale_api.Services
  {
  /// <summary>
  /// Service for handling serial port communication with scales.
  /// </summary>
  public class SerialPortService
    {
    private readonly ILogger<SerialPortService> _logger;
    private readonly Dictionary<string, string> _portMapping;
    private readonly SerialPortSettings _settings;
    private SerialPort _serialPort;
    private List<(double value, string unit)> _dataSamples;
    private Regex _dataPattern;


    /// <summary>
    /// Initializes a new instance of the <see cref="SerialPortService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging messages.</param>
    /// <param name="portMapping">Dictionary mapping UUIDs to COM ports.</param>
    /// <param name="settings">Settings for the serial port service.</param>
    public SerialPortService(ILogger<SerialPortService> logger, Dictionary<string, string> portMapping, SerialPortSettings settings)
      {
      _logger = logger;
      _portMapping = portMapping;
      _settings = settings;
      _dataSamples = new List<(double value, string unit)>();
      _dataPattern = new Regex(settings.StandardDataPattern); // Initialize regex with pattern from settings
      }

    /// <summary>
    /// Gets consistent data from the specified serial port.
    /// </summary>
    /// <param name="uuid">UUID of the scale.</param>
    /// <param name="baudRate">Baud rate for the serial port.</param>
    /// <returns>Tuple containing success status, message, value, unit, and warning.</returns>
    public async Task<(bool success, string message, double? value, string? unit, object? warning)> GetConsistentData(string uuid, int baudRate)
      {
      if (!_portMapping.ContainsKey(uuid))
        {
        throw new ArgumentException("Invalid UUID");
        }

      string portName = _portMapping[uuid];
      _dataSamples.Clear();  // Clear previous samples

      try
        {
        _serialPort?.Close();  // Close any previously opened port
        _serialPort = new SerialPort(portName, baudRate)
          {
          Parity = Parity.None,
          DataBits = 8,
          StopBits = StopBits.One,
          Handshake = Handshake.None,
          ReadTimeout = 500,
          WriteTimeout = 500
          };
        _serialPort.DataReceived += DataReceivedHandler;
        _serialPort.Open();
        _logger.LogInformation("[INFO] Serial port {portName} opened", portName);

        // Add an initial delay to stabilize data reading
        await Task.Delay(500);

        // Asynchronously wait until the sample size is reached or timeout occurs
        var timeoutTask = Task.Delay(_settings.TimeoutMilliseconds);
        while (_dataSamples.Count < _settings.SampleSize && !timeoutTask.IsCompleted)
          {
          await Task.Delay(50);  // Check every 50ms
          }

        // Log the final samples collected
        _logger.LogInformation("[INFO] Final samples collected: {samples}", string.Join(", ", _dataSamples.Select(s => s.value.ToString())));

        // Check if the required sample size was reached
        if (!timeoutTask.IsCompleted)
          {
          var peakSample = GetPeakSample();
          if (peakSample != null)
            {
            _serialPort.Close();
            _logger.LogInformation("[INFO] Serial port closed");
            var (value, unit) = peakSample.Value;
            // Create a warning if the data distribution is inconsistent
            var warning = ValidateDistribution(value) ? null : new
              {
              message = "Inconsistent data distribution",
              sample = _dataSamples.Select(s => s.value).ToList()
              };
            return (true, "Success", value, unit, warning);
            }

          _logger.LogWarning("[WARN] Collected samples are not consistent. Extending sample size.");
          await ExtendSampleSize();
          }

        _serialPort.Close();
        _logger.LogWarning("[WARN] Data collection timed out");
        return (false, "Data collection timed out", null, null, null);
        }
      catch (FormatException ex)
        {
        _logger.LogError("[ERROR] Data format error: {message}", ex.Message);
        return (false, $"Data format error: {ex.Message}", null, null, null);
        }
      catch (Exception ex)
        {
        _logger.LogError("[ERROR] Error handling serial port {portName}: {message}", portName, ex.Message);
        return (false, $"Error handling serial port {portName}: {ex.Message}", null, null, null);
        }
      }


    /// <summary>
    /// Handles the DataReceived event of the serial port.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SerialDataReceivedEventArgs"/> instance containing the event data.</param>
    private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
      {
      try
        {
        if (_serialPort != null && _serialPort.IsOpen)
          {
          string data = _serialPort.ReadLine().Trim();
          _logger.LogInformation("[INFO] Data received: {data}", data);
          if (_dataPattern.IsMatch(data))
            {
            _logger.LogInformation("[INFO] Data matches pattern: {data}", data);
            var (value, unit) = ParseData(data);
            _dataSamples.Add((value, unit));
            }
          else
            {
            _logger.LogWarning("[WARN] Invalid data format: {data}", data);
            }
          }
        }
      catch (TimeoutException)
        {
        _logger.LogWarning("[WARN] Read timeout occurred");
        }
      catch (Exception ex)
        {
        _logger.LogError("[ERROR] Error reading from serial port: {message}", ex.Message);
        }
      }

    /// <summary>
    /// Parses the given data string to extract the value and unit.
    /// </summary>
    /// <param name="data">The data string to parse.</param>
    /// <returns>Tuple containing the value and unit.</returns>
    private (double value, string unit) ParseData(string data)
      {
      var match = _dataPattern.Match(data);
      _logger.LogInformation("[INFO] Attempting to parse data: {data}", data);
      if (match.Success)
        {
        if (double.TryParse(match.Groups[1].Value, out var value))
          {
          var unit = match.Groups[2].Value;
          _logger.LogInformation("[INFO] Successfully parsed data: value={value}, unit={unit}", value, unit);
          return (Math.Round(value, 1), unit);
          }
        else
          {
          _logger.LogError("[ERROR] Failed to parse value: {value}", match.Groups[1].Value);
          }
        }
      _logger.LogError("[ERROR] Invalid data format: {data}", data);
      throw new FormatException($"Invalid data format: {data}");
      }

    /// <summary>
    /// Extends the sample size by collecting additional samples.
    /// </summary>
    private async Task ExtendSampleSize()
      {
      var extendedSize = _settings.SampleSize + 5;
      var timeoutTask = Task.Delay(_settings.TimeoutMilliseconds);
      while (_dataSamples.Count < extendedSize && !timeoutTask.IsCompleted)
        {
        await Task.Delay(50);  // Check every 50ms
        }
      _logger.LogInformation("[INFO] Extended samples collected: {samples}", string.Join(", ", _dataSamples.Select(s => s.value.ToString())));
      }

    /// <summary>
    /// Gets the peak sample from the collected data samples.
    /// </summary>
    /// <returns>Tuple containing the value and unit of the peak sample.</returns>
    private (double value, string unit)? GetPeakSample()
      {
      var groupedSamples = _dataSamples.GroupBy(s => s.value)
                                       .Select(g => new { Value = g.Key, Count = g.Count(), Unit = g.First().unit })
                                       .OrderByDescending(g => g.Count)
                                       .ToList();

      if (groupedSamples.Count == 0) return null;

      var maxCount = groupedSamples.First().Count;
      var mostFrequent = groupedSamples.Where(g => g.Count == maxCount).ToList();

      if (mostFrequent.Count == 1 || ValidateDistribution(mostFrequent.First().Value))
        {
        return (mostFrequent.First().Value, mostFrequent.First().Unit);
        }

      return null;
      }

    /// <summary>
    /// Validates the distribution of the collected data samples.
    /// </summary>
    /// <param name="peakValue">The peak value to validate against.</param>
    /// <returns><c>true</c> if the distribution is valid; otherwise, <c>false</c>.</returns>
    private bool ValidateDistribution(double peakValue)
      {
      var validSamples = _dataSamples.Select(s => s.value).Where(v => Math.Abs(v - peakValue) <= _settings.ErrorTolerance).ToList();
      return validSamples.Count >= _settings.SampleSize;
      }


    /// <summary>
    /// Safely parses the value from the given data string.
    /// </summary>
    /// <param name="data">The data string to parse.</param>
    /// <returns>The parsed value as a string, or "Invalid" if parsing fails.</returns>
    private string ParseValueSafe(string data)
      {
      try
        {
        return ParseData(data).value.ToString("F1");
        }
      catch (FormatException ex)
        {
        _logger.LogWarning("[WARN] {message}", ex.Message);
        return "Invalid";
        }
      }
    }
  }
