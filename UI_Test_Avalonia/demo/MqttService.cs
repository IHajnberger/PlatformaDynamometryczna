using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;

namespace UI_Test_Avalonia;

public sealed class MqttService
{
    private static readonly Lazy<MqttService> lazy = new(() => new MqttService());
    public static MqttService Instance => lazy.Value;

    private MqttServer? _mqttServer;
    private CancellationTokenSource? _cancellationTokenSource;

    private readonly ConcurrentQueue<(double left, double right, DateTime timestamp)> _internalSampleBuffer = new();

    public ConcurrentQueue<(double Weight, DateTime Timestamp)> Device1Queue { get; } = new();
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> Device2Queue { get; } = new();
    public ConcurrentQueue<(double Weight, DateTime Timestamp)> SumQueue { get; } = new();

    public DateTime LastPacketTime { get; private set; } = DateTime.MinValue;
    
    // Auto-adaptive drip feed delay
    private double _currentDripFeedDelayMs = 12.5; 

    private MqttService()
    {
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

                if (topic == "esp32/scale/telemetry")
                {
                    try
                    {
                        using var document = JsonDocument.Parse(payload);
                        
                        if (document.RootElement.TryGetProperty("timestamp_s", out var tsSecElement) &&
                            document.RootElement.TryGetProperty("left", out var leftElement) &&
                            document.RootElement.TryGetProperty("right", out var rightElement))
                        {
                            long ts_s = tsSecElement.GetInt64();
                            long ts_ms = document.RootElement.TryGetProperty("timestamp_ms", out var tsMsElement) ? tsMsElement.GetInt64() : 0;

                            var baseTimestamp = DateTimeOffset.FromUnixTimeSeconds(ts_s).DateTime.ToLocalTime().AddMilliseconds(ts_ms);
                            
                            // Auto-detect if hardware is running at 10Hz or 80Hz
                            var now = DateTime.Now;
                            if (LastPacketTime != DateTime.MinValue)
                            {
                                int arrCount = leftElement.GetArrayLength();
                                if (arrCount > 0)
                                {
                                    double timeSinceLastPacket = (now - LastPacketTime).TotalMilliseconds;
                                    double calculatedDelay = timeSinceLastPacket / arrCount;
                                    
                                    // Clamp between 10ms (100Hz) and 120ms (~8Hz)
                                    if (calculatedDelay >= 10 && calculatedDelay <= 120)
                                    {
                                        // Smooth transition for the delay
                                        _currentDripFeedDelayMs = (_currentDripFeedDelayMs * 0.7) + (calculatedDelay * 0.3);
                                    }
                                }
                            }
                            LastPacketTime = now;

                            if (leftElement.ValueKind == JsonValueKind.Array && rightElement.ValueKind == JsonValueKind.Array)
                            {
                                int count = Math.Min(leftElement.GetArrayLength(), rightElement.GetArrayLength());
                                for (int i = 0; i < count; i++)
                                {
                                    double leftWeight = leftElement[i].GetDouble();
                                    double rightWeight = rightElement[i].GetDouble();
                                    
                                    var itemTimestamp = baseTimestamp.AddMilliseconds(i * _currentDripFeedDelayMs);
                                    _internalSampleBuffer.Enqueue((leftWeight, rightWeight, itemTimestamp));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                         Debug.WriteLine($"[MqttService] Failed to parse JSON. Payload size: {payload.Length} bytes. Error: {ex.Message}");
                    }
                }
                
                return Task.CompletedTask;
            };

            await _mqttServer.StartAsync();
            Debug.WriteLine("[MqttService] MQTT Server started successfully on port 1883.");

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => DripFeedSamplesToUI(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        catch(Exception ex)
        {
             Debug.WriteLine($"[MqttService] FATAL MQTT server error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task DripFeedSamplesToUI(CancellationToken token)
    {
        Debug.WriteLine("[MqttService] Drip-feed task started.");
        
        var sw = Stopwatch.StartNew();
        double expectedTotalMs = 0;

        while (!token.IsCancellationRequested)
        {
            if (_internalSampleBuffer.TryDequeue(out var sample))
            {
                Device1Queue.Enqueue((sample.left, sample.timestamp));
                Device2Queue.Enqueue((sample.right, sample.timestamp));
                SumQueue.Enqueue((sample.left + sample.right, sample.timestamp));
                
                expectedTotalMs += _currentDripFeedDelayMs;

                double delayNeeded = expectedTotalMs - sw.Elapsed.TotalMilliseconds;
                
                if (delayNeeded > 0)
                {
                    await Task.Delay((int)delayNeeded, token);
                }
                else if (delayNeeded < -200) 
                {
                    expectedTotalMs = sw.Elapsed.TotalMilliseconds;
                }
            }
            else
            {
                await Task.Delay(10, token);
                sw.Restart();
                expectedTotalMs = 0;
            }
        }
        Debug.WriteLine("[MqttService] Drip-feed task stopped.");
    }

    public async Task StopAsync()
    {
        if (_mqttServer != null && _mqttServer.IsStarted)
        {
            Debug.WriteLine("[MqttService] Stopping MQTT Server...");
            _cancellationTokenSource?.Cancel();
            await _mqttServer.StopAsync();
            _mqttServer.Dispose();
            Debug.WriteLine("[MqttService] MQTT Server stopped.");
        }
    }
}