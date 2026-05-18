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
    public ConcurrentQueue<double> DataQueue { get; } = new();
    public DateTime LastPacketTime { get; private set; } = DateTime.MinValue;

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
                        if (document.RootElement.TryGetProperty("weight", out var weightElement))
                        {
                            var weight = weightElement.GetDouble();
                            DataQueue.Enqueue(weight);
                            LastPacketTime = DateTime.Now;
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