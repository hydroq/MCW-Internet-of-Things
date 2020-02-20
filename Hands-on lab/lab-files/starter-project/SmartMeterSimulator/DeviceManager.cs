using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartMeterSimulator
{
    internal class DeviceManager
    {
        private static string connectionString;
        private static RegistryManager registryManager;

        public static string HostName { get; set; }

        public static void IotHubConnect(string cnString)
        {
            connectionString = cnString;

            registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            var builder = IotHubConnectionStringBuilder.Create(cnString);

            HostName = builder.HostName;
        }

        /// <summary>
        /// Register a single device with the IoT hub. The device is initially registered in a
        /// disabled state.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async static Task<string> RegisterDevicesAsync(string connectionString, string deviceId)
        {
            //Make sure we're connected
            if (registryManager == null)
            {
                IotHubConnect(connectionString);
            }

            Device device = new Device(deviceId)
            {
                Status = DeviceStatus.Disabled
            };

            try
            {
                device = await registryManager.AddDeviceAsync(device);
            }
            catch (Exception ex)
            {
                if (ex is DeviceAlreadyExistsException ||
                    ex.Message.Contains("DeviceAlreadyExists"))
                {
                    device = await registryManager.GetDeviceAsync(deviceId);
                    device.Status = DeviceStatus.Disabled;

                    await registryManager.UpdateDeviceAsync(device);
                }
                else
                {
                    MessageBox.Show($"An error occurred while registering one or more devices:\r\n{ex.Message}");
                }
            }

            //return the device key
            return device.Authentication.SymmetricKey.PrimaryKey;
        }

        /// <summary>
        /// Activate an already registered device by changing its status to Enabled.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        public async static Task<bool> ActivateDeviceAsync(string connectionString, string deviceId, string deviceKey)
        {
            //Server-side management function to enable the provisioned device
            //to connect to IoT Hub after it has been installed locally.
            //If device id device key are valid, Activate (enable) the device.

            //Make sure we're connected
            if (registryManager == null)
            {
                IotHubConnect(connectionString);
            }

            bool success = false;
            Device device = null;

            try
            {
                device = await registryManager.GetDeviceAsync(deviceId);

                if (device.Authentication.SymmetricKey.PrimaryKey == deviceKey)
                {
                    device.Status = DeviceStatus.Enabled;

                    await registryManager.UpdateDeviceAsync(device);

                    success = true;
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Deactivate a single device in the IoT Hub registry.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async static Task<bool> DeactivateDeviceAsync(string connectionString, string deviceId)
        {
            //Make sure we're connected
            if (registryManager == null)
            {
                IotHubConnect(connectionString);
            }

            bool success = false;
            Device device;

            try
            {
                device = await registryManager.GetDeviceAsync(deviceId);
                device.Status = DeviceStatus.Disabled;

                await registryManager.UpdateDeviceAsync(device);

                success = true;
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Unregister a single device from the IoT Hub Registry
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async static Task UnregisterDevicesAsync(string connectionString, string deviceId)
        {
            //Make sure we're connected
            if (registryManager == null)
            {
                IotHubConnect(connectionString);
            }

            await registryManager.RemoveDeviceAsync(deviceId);
        }

        /// <summary>
        /// Unregister all the devices managed by the Smart Meter Simulator
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async static Task UnregisterAllDevicesAsync(string connectionString)
        {
            //Make sure we're connected
            if (registryManager == null)
            {
                IotHubConnect(connectionString);
            }

            for (int i = 0; i <= 9; i++)
            {
                string deviceId = "Device" + i.ToString();

                try
                {
                    await registryManager.RemoveDeviceAsync(deviceId);
                }
                catch
                { }
            }
        }
    }
}