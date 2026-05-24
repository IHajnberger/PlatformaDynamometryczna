using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;

namespace UI_Test_Avalonia;

public sealed class MqttService
{
    // --- Singleton Implementation ---
    private static readonly Lazy<MqttService> lazy = new(() => new MqttService());
    public static MqttService Instance => lazy.Value;

    private MqttServer? _mqttServer;

    // --- Public Data Access ---
    
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> Device1Queue { get; } = new();
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> Device2Queue { get; } = new();
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> SumQueue { get; } = new();

    public DateTime LastPacketTime { get; private set; } = DateTime.MinValue;
    
    private double _lastWeight1 = 0;
    private double _lastWeight2 = 0;

    private string deviceOneId = "Left";
    private string deviceTwoId = "Right";
    private MqttService()
    {
        // Private constructor for singleton
    }

    public async Task StartAsync()
    {
        if (_mqttServer != null && _mqttServer.IsStarted)
        {
            Debug.WriteLine("[MqttService] Server is already running.");
            return;
        }

        try
        {
            var mqttFactory = new MqttFactory();
            var mqttServerOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(1883)
                .Build();

            _mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

            _mqttServer.InterceptingPublishAsync += e =>
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                var topic = e.ApplicationMessage.Topic;
                
                Debug.WriteLine($"[MqttService] RAW MQTT Packet received on {topic}: '{payload}'");

                if (topic == "esp32/scale/telemetry")
                {
                    try
                    {
                        using var document = JsonDocument.Parse(payload);
                        
                        if (document.RootElement.TryGetProperty("deviceId", out var idElement) && 
                            document.RootElement.TryGetProperty("weight", out var weightElement) &&
                            document.RootElement.TryGetProperty("timestamp", out var timestampElement))
                        {
                            var deviceId = idElement.GetString();
                            var weight = weightElement.GetDouble();
                            var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampElement.GetInt64()).DateTime.ToLocalTime();
                            
                            LastPacketTime = DateTime.Now;

                            if (deviceId == deviceOneId)
                            {
                                _lastWeight1 = weight;
                                Device1Queue.Enqueue((weight, timestamp));
                            }
                            else if (deviceId == deviceTwoId)
                            {
                                _lastWeight2 = weight;
                                Device2Queue.Enqueue((weight, timestamp));
                            }
                            
                            var sum = _lastWeight1 + _lastWeight2;
                            SumQueue.Enqueue((sum, DateTime.Now));
                        }
                    }
                    catch (Exception ex)
                    {
                         Debug.WriteLine($"[MqttService] Failed to parse JSON: {ex.Message}");
                    }
                }
                
                return Task.CompletedTask;
            };

            await _mqttServer.StartAsync();
            Debug.WriteLine("[MqttService] MQTT Server started successfully on port 1883.");
        }
        catch(Exception ex)
        {
             Debug.WriteLine($"[MqttService] FATAL MQTT server error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public async Task StopAsync()
    {
        if (_mqttServer != null && _mqttServer.IsStarted)
        {
            Debug.WriteLine("[MqttService] Stopping MQTT Server...");
            await _mqttServer.StopAsync();
            _mqttServer.Dispose();
            Debug.WriteLine("[MqttService] MQTT Server stopped.");
        }
    }
}