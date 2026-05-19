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
    
    // We now track two sets of data for the two ESP32 scales, along with their timestamps
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> Device1Queue { get; } = new();
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> Device2Queue { get; } = new();
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> AverageQueue { get; } = new();

    // Track when we last heard from *any* device
    public DateTime LastPacketTime { get; private set; } = DateTime.MinValue;
    
    // Track the latest weights to compute the average
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
                            // ESP32 sends epoch time in seconds, convert to DateTime
                            var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampElement.GetInt64()).DateTime.ToLocalTime();
                            
                            LastPacketTime = DateTime.Now;

                            // Route the data based on the device ID
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
                            
                            // Calculate and queue the average, using the current time as the timestamp for the average
                            var average = (_lastWeight1 + _lastWeight2) / 2.0;
                            AverageQueue.Enqueue((average, DateTime.Now));
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