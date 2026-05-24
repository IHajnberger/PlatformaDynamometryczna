using System;
using System.IO.Ports;
using UI_Test_Avalonia.Models;

namespace UI_Test_Avalonia.Services;

//do serial wrzucone do testów 

public class SerialService
{
    private SerialPort? _serialPort;

    public event Action<PlatformData>? DataReceived;

    public void Connect(string port)
    {
        _serialPort = new SerialPort(port, 115200);

        _serialPort.DataReceived += OnDataReceived;

        _serialPort.Open();
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            string line = _serialPort!.ReadLine();

            string[] parts = line.Trim().Split(';');

            if (parts.Length != 2)
                return;

            float left = float.Parse(parts[0]);
            float right = float.Parse(parts[1]);

            DataReceived?.Invoke(new PlatformData
            {
                LeftWeight = left,
                RightWeight = right
            });
        }
        catch
        {
        }
    }
}