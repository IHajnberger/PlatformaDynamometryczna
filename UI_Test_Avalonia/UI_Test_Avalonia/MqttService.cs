using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Linq;
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
                        var root = document.RootElement;

                        // New batch format: { "timestamp_s": ..., "timestamp_ms": ..., "left": [...], "right": [...] }
                        if (root.TryGetProperty("timestamp_s", out var tsSecElement) &&
                            root.TryGetProperty("timestamp_ms", out var tsMsElement) &&
                            root.TryGetProperty("left", out var leftArrayElement) &&
                            root.TryGetProperty("right", out var rightArrayElement) &&
                            leftArrayElement.ValueKind == JsonValueKind.Array &&
                            rightArrayElement.ValueKind == JsonValueKind.Array)
                        {
                            var seconds = tsSecElement.GetInt64();
                            var milliseconds = tsMsElement.GetInt32();
                            var leftArray = leftArrayElement.EnumerateArray().Select(je => je.GetDouble()).ToList();
                            var rightArray = rightArrayElement.EnumerateArray().Select(je => je.GetDouble()).ToList();

                            if (leftArray.Count > 0 && leftArray.Count == rightArray.Count)
                            {
                                LastPacketTime = DateTime.Now;
                                
                                // The timestamp from the ESP marks the END of the batch collection.
                                var batchEndTimestamp = DateTimeOffset.FromUnixTimeSeconds(seconds).AddMilliseconds(milliseconds).DateTime.ToLocalTime();
                                
                                // The ESP code sends samples approx. every 12ms in simulation mode.
                                // We must back-calculate the timestamp for each sample in the batch.
                                const int SAMPLE_PERIOD_MS = 12; 
                                int batchSize = leftArray.Count;

                                for (int i = 0; i < batchSize; i++)
                                {
                                    var leftWeight = leftArray[i];
                                    var rightWeight = rightArray[i];
                                    var sum = leftWeight + rightWeight;

                                    // Calculate timestamp for this specific sample by offsetting from the end time
                                    int timeOffset = (batchSize - 1 - i) * SAMPLE_PERIOD_MS;
                                    var sampleTimestamp = batchEndTimestamp.AddMilliseconds(-timeOffset);

                                    Device1Queue.Enqueue((leftWeight, sampleTimestamp));
                                    Device2Queue.Enqueue((rightWeight, sampleTimestamp));
                                    SumQueue.Enqueue((sum, sampleTimestamp));
                                }
                            }
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