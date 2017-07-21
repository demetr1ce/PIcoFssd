using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace PIcoFssd
{
    internal class PicoStatusHv3
    {
        private I2cDevice I2CHat;

        public async void InitPicoStatus()
        {
            try
            {
                var settings = new I2cConnectionSettings(0x69);

                /* 400KHz bus speed */
                settings.BusSpeed = I2cBusSpeed.FastMode;

                /* Get a selector string that will return all I2C controllers on the system */
                string aqs = I2cDevice.GetDeviceSelector();
                Debug.WriteLine("AQS: " + aqs);

                /* Find the I2C bus controller devices with our selector string */
                var dis = await DeviceInformation.FindAllAsync(aqs);
                foreach (var d in dis)
                {
                    Debug.WriteLine("Device: " + d.Name + " -> " + d.Kind + ", " + d.Id);

                    foreach (var p in d.Properties)
                    {
                        Debug.WriteLine("     Property: " + p.Key + " -> " + p.Value);
                    }
                }

                /* Create an I2cDevice with our selected bus controller and I2C settings    */
                I2CHat = await I2cDevice.FromIdAsync(dis[0].Id, settings);
                Debug.WriteLine("HAT: " + I2CHat.DeviceId);
                Debug.WriteLine("Address: " + settings.SlaveAddress);
                Debug.WriteLine("");
                Task.Delay(100);

                byte[] readData = new byte[2];
                byte[] writeData;
                string output = String.Empty;

                writeData = new byte[] { 1, 0x77 };
                I2CHat.Read(writeData);
                Debug.WriteLine("Data: ");
                if (I2CHat == null)
                {
                    Debug.WriteLine(string.Format(
                        "Slave address {0} on I2C Controller {1} is currently in use by " +
                        "another application. Please ensure that no other applications are using I2C.",
                        settings.SlaveAddress,
                        dis[0].Id));
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
 
        }

        public string fwVersion()
        {
            Task.Delay(100);

            byte[] readData = new byte[6];
            byte[] writeData;
            string output = String.Empty;

            writeData = new byte[] { 0x26 };
            //I2CHat.WriteRead(writeData, readData);
            return output = readData.ToString();
        }
    }
}
