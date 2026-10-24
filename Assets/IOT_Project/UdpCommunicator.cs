using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UdpCommunicator : MonoBehaviour
{
    [Header("Network Settings")]
    public int listenPort = 5005;
    public int targetPort = 5005;

    [Header("UI References")]
    [Tooltip("Input field where the user types the target IP address.")]
    public TMP_InputField ipInputField;

    [Tooltip("Text field to display received messages and status.")]
    public TMP_Text statusText;

    [Tooltip("Text field to display THIS device's local IP address.")]
    public TMP_Text localIpText; // NEW: Field to show your system IP

    public Button connectButton;
    public Button sendButton;

    private UdpClient udpReceiver;
    private UdpClient udpSender;
    private IPEndPoint targetEndPoint;
    private Thread receiveThread;
    private bool isRunning = false;

    private readonly ConcurrentQueue<string> mainThreadQueue = new();

    private void Start()
    {
        // 1. Fetch and display this device's IP Address automatically
        if (localIpText != null)
        {
            localIpText.text = $"My IP: {GetLocalIPAddress()}";
        }

        if (ipInputField != null && string.IsNullOrEmpty(ipInputField.text))
        {
            ipInputField.text = "192.168.1.20";
        }

        if (connectButton != null) connectButton.onClick.AddListener(StartUdp);
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendTestData);
            sendButton.interactable = false;
        }

        UpdateStatus("Ready. Enter target IP and press Connect.");
    }

    // NEW: Method to find the system's local IPv4 address
    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                // Look for an IPv4 address and filter out the local loopback address (127.0.0.1)
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IP Fetch Error]: {e.Message}");
        }
        return "Not connected to Wi-Fi";
    }

    public void StartUdp()
    {
        if (isRunning) return;

        string targetIP = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(targetIP))
        {
            UpdateStatus("Error: IP Address cannot be empty.");
            return;
        }

        try
        {
            udpSender = new UdpClient();
            targetEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);

            udpReceiver = new UdpClient(listenPort);
            isRunning = true;

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();

            UpdateStatus($"Listening on {listenPort}.\nSending to {targetIP}:{targetPort}");

            if (sendButton != null) sendButton.interactable = true;
            if (connectButton != null) connectButton.interactable = false;
        }
        catch (FormatException)
        {
            UpdateStatus($"Error: '{targetIP}' is not a valid IP.");
        }
        catch (Exception e)
        {
            UpdateStatus($"Failed to start UDP: {e.Message}");
        }
    }

    void Update()
    {
        while (mainThreadQueue.TryDequeue(out string message))
        {
            UpdateStatus($"Received: {message}");
        }
    }

    public void SendTestData()
    {
        SendPacket($"Hello from Android at {Time.time:F1}s");
    }

    public void SendPacket(string message)
    {
        if (udpSender == null || !isRunning) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpSender.Send(data, data.Length, targetEndPoint);
        }
        catch (Exception e)
        {
            UpdateStatus($"Send Error: {e.Message}");
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteSender = new(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] receivedBytes = udpReceiver.Receive(ref remoteSender);
                string receivedText = Encoding.UTF8.GetString(receivedBytes);
                mainThreadQueue.Enqueue($"{receivedText}");
            }
            catch (SocketException) { break; }
            catch (Exception) { /* Handle hidden thread errors */ }
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void StopUdp()
    {
        isRunning = false;
        if (udpReceiver != null) { udpReceiver.Close(); udpReceiver = null; }
        if (udpSender != null) { udpSender.Close(); udpSender = null; }
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join(100);
    }

    void OnDestroy() => StopUdp();
    void OnApplicationQuit() => StopUdp();
}