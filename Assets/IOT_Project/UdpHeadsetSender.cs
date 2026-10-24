using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UdpHeadsetSender : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("Enter the IP printed in the ESP32 Serial Monitor")]
    public string esp32Ip = "192.168.1.50";
    public int esp32Port = 4210;

    [Header("Update Frequency")]
    [Range(10, 60)]
    public int sendRateHz = 30; // 30 packets/second is smooth and avoids flooding Wi-Fi

    private UdpClient _udpClient;
    private float _timer;

    void Start()
    {
        _udpClient = new UdpClient();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= (1f / sendRateHz))
        {
            _timer = 0f;
            SendOrientation();
        }
    }

    void SendOrientation()
    {
        // Get camera's local Euler angles
        Vector3 rot = transform.localEulerAngles;

        // Convert Unity's 0..360 range into standard +/- 90 range
        float pitch = (rot.x > 180) ? rot.x - 360 : rot.x;
        float yaw = (rot.y > 180) ? rot.y - 360 : rot.y;

        // Map pitch/yaw (-45 to +45 deg) to servo range (0 to 180 deg, centered at 90)
        float servoPitch = Mathf.Clamp(90f + pitch, 10f, 170f);
        float servoYaw = Mathf.Clamp(90f + yaw, 10f, 170f);

        // Format as "pitch,yaw"
        string payload = $"{servoPitch:F1},{servoYaw:F1}";
        byte[] data = Encoding.UTF8.GetBytes(payload);

        try
        {
            _udpClient.Send(data, data.Length, esp32Ip, esp32Port);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UDP Send Error: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        _udpClient?.Close();
    }
}
