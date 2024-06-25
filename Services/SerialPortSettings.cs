namespace fpn_scale_api.Services
  {
  public class SerialPortSettings
    {
    public required int SampleSize { get; set; }
    public required int TimeoutMilliseconds { get; set; }
    public required int DefaultBaudRate { get; set; }
    public required double ErrorTolerance { get; set; }
    public required string HttpUrl { get; set; }
    public required string StandardDataPattern { get; set; }
    }
  }
