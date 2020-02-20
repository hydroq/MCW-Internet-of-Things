using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Text;

namespace SmartMeterSimulator
{
    /// <summary>
    /// A sensor represents a Smart Meter in the simulator.
    /// </summary>
    internal class Sensor
    {
        private DeviceClient _DeviceClient;
        private string _IotHubUri { get; set; }
        public string DeviceId { get; set; }
        public string DeviceKey { get; set; }
        public DeviceState State { get; set; }
        public string StatusWindow { get; set; }
        public string ReceivedMessage { get; set; }
        public double? ReceivedTemperatureSetting { get; set; }

        public double CurrentTemperature
        {
            get
            {
                double avgTemperature = 70;
                Random rand = new Random();
                double currentTemperature = avgTemperature + rand.Next(-6, 6);

                if (ReceivedTemperatureSetting.HasValue)
                {
                    // If we received a cloud-to-device message that sets the temperature, override with the received value.
                    currentTemperature = ReceivedTemperatureSetting.Value;
                }

                if (currentTemperature <= 68)
                    TemperatureIndicator = SensorState.Cold;
                else if (currentTemperature > 68 && currentTemperature < 72)
                    TemperatureIndicator = SensorState.Normal;
                else if (currentTemperature >= 72)
                    TemperatureIndicator = SensorState.Hot;

                return currentTemperature;
            }
        }

        public SensorState TemperatureIndicator { get; set; }

        public Sensor(string iotHubUri, string deviceId, string deviceKey)
        {
            _IotHubUri = iotHubUri;
            DeviceId = deviceId;
            DeviceKey = deviceKey;
            State = DeviceState.Registered;
        }

        public void InstallDevice(string statusWindow)
        {
            StatusWindow = statusWindow;
            State = DeviceState.Installed;
        }

        /// <summary>
        /// Connect a device to the IoT Hub by instantiating a DeviceClient for that Device by Id and Key.
        /// </summary>
        public void ConnectDevice()
        {
            _DeviceClient = DeviceClient.Create(_IotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey));

            //Set the Device State to Ready
            State = DeviceState.Ready;
        }

        public void DisconnectDevice()
        {
            //Delete the local device client
            _DeviceClient = null;

            //Set the Device State to Activate
            State = DeviceState.Activated;
        }

        /// <summary>
        /// Send a message to the IoT Hub from the Smart Meter device
        /// </summary>
        public async void SendMessageAsync()
        {
            var telemetryDataPoint = new
            {
                id = DeviceId,
                time = DateTime.UtcNow.ToString("o"),
                temp = CurrentTemperature
            };

            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);

            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            var sendEventAsync = _DeviceClient?.SendEventAsync(message);
            if (sendEventAsync != null)
            {
                await sendEventAsync;
            }
        }

        /// <summary>
        /// Check for new messages sent to this device through IoT Hub.
        /// </summary>
        public async void ReceiveMessageAsync()
        {
            try
            {
                // Receive a message from IoT Hub
                var receivedMessage = await _DeviceClient?.ReceiveAsync();
                if (receivedMessage == null)
                {
                    ReceivedMessage = null;

                    return;
                }

                ReceivedMessage = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                if (double.TryParse(ReceivedMessage, out var requestedTemperature))
                {
                    ReceivedTemperatureSetting = requestedTemperature;
                }
                else
                {
                    ReceivedTemperatureSetting = null;
                }

                // Send acknowledgement to IoT Hub that the has been successfully processed.
                // The message can be safely removed from the device queue. If something happened
                // that prevented the device app from completing the processing of the message,
                // IoT Hub delivers it again.

                await _DeviceClient?.CompleteAsync(receivedMessage);
            }
            catch (NullReferenceException ex)
            {
                // The device client is null, likely due to it being disconnected since this method was called.
                System.Diagnostics.Debug.WriteLine("The DeviceClient is null. This is likely due to it being disconnected since the ReceiveMessageAsync message was called.");
            }
        }
    }

    public enum DeviceState
    {
        Registered,
        Installed,
        Activated,
        Ready,
        Transmit
    }

    public enum SensorState
    {
        Cold,
        Normal,
        Hot
    }
}