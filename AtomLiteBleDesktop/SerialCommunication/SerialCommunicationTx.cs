using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace AtomLiteBleDesktop.SerialCommunication
{
    class SerialCommunicationTx
    {

        /// <summary>
        /// log4net用インスタンス
        /// </summary>
        private static readonly log4net.ILog logger = LogHelper.GetInstanceLog4net(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /*
        private static SerialDevice device;

        public static async void InitSerialPort()
        {
            string portName = "COM6";
            string aqs = SerialDevice.GetDeviceSelector(portName);

            var myDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(aqs, null);
            if (myDevices.Count == 0)
            {
                return;
            }
            else
            {
                //TODO: 下記がnullになると落ちてしまうので対策が必要・・・
                device = await SerialDevice.FromIdAsync(myDevices[0].Id);
                if (device.IsRequestToSendEnabled)
                {
                    device.BaudRate = 115200;
                    device.DataBits = 8;
                    device.StopBits = SerialStopBitCount.One;
                    device.Parity = SerialParity.None;
                    device.Handshake = SerialHandshake.None;
                    device.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    device.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                }
                device.ErrorReceived += SerialDeviceError;

            }

        }
        */
        public static async Task< Dictionary<int, Dictionary<string, DeviceInformation>>> FindDevices()
        {
            var device = new Dictionary<int, Dictionary<string, DeviceInformation>>();
            

            string portNameDefine = "COM";
            string portName;
            for (int i = 0; i < 16; i++)
            {
                portName = portNameDefine + i.ToString();
                string aqs = SerialDevice.GetDeviceSelector(portName);
                if (aqs != null)
                {
                    var serialDeviceInfos = await DeviceInformation.FindAllAsync(aqs);
                    if (serialDeviceInfos != null)
                    {
                        if (serialDeviceInfos.Count > 0)
                        {

                            foreach(var serialDeviceInfo in serialDeviceInfos)
                            {
                                if(serialDeviceInfo.IsEnabled)
                                {
                                    var tmp = new Dictionary<string, DeviceInformation>();
                                    tmp.Add(portName, serialDeviceInfo);
                                    device.Add(i, tmp);
                                    break;

                                }
                            }
                        }
                    }
                }

            }
            return device;
        }

        /// <summary>
        /// Portが有効か判定する（デバイスマネージャーに出てこないポートはfalseとなる）
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        public static async Task<bool> IsFindSerialPort(string portName)
        {
            string aqs = SerialDevice.GetDeviceSelector(portName);
            try
            {
                var serialDeviceInfos = await DeviceInformation.FindAllAsync(aqs);
                if (serialDeviceInfos != null)
                {
                    foreach (var serialDeviceInfo in serialDeviceInfos)
                    {
                        if (serialDeviceInfo.IsEnabled)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public static async void DriveSerial()
        {
            string portName = "COM6";
            string aqs = SerialDevice.GetDeviceSelector(portName);
            SerialDevice serialDevice=null;
            DataWriter dw=null;

            try
            {
                var serialDeviceInfos = await DeviceInformation.FindAllAsync(aqs);
                if (serialDeviceInfos != null)
                {
                    foreach (DeviceInformation serialDeviceInfo in serialDeviceInfos)
                    {
                        serialDevice = await SerialDevice.FromIdAsync(serialDeviceInfo.Id);
                        if (serialDevice != null && serialDevice.IsRequestToSendEnabled)
                        {
                            serialDevice.BaudRate = 115200;
                            serialDevice.DataBits = 8;
                            serialDevice.StopBits = SerialStopBitCount.One;
                            serialDevice.Parity = SerialParity.None;
                            serialDevice.Handshake = SerialHandshake.None;
                            serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                            serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                            serialDevice.ErrorReceived += SerialDeviceError;
                            break;

                        }
                        else
                        {
                            logger.Info("SerialDevice is null :" + serialDeviceInfo.Name);
                        }

                    }
                }
                if (serialDeviceInfos != null && serialDevice!=null)
                {
                    using (dw = new DataWriter(serialDevice.OutputStream))
                    {
                        // Found a valid serial device.

                        // Reading a byte from the serial device.
                        //DataReader dr = new DataReader(serialDevice.InputStream);
                        //int readByte = dr.ReadByte();

                        // Writing a byte to the serial device.
                        dw = new DataWriter(serialDevice.OutputStream);
                        //dw.WriteByte(0x0a);
                        dw.WriteString("a");
                        var ret = dw.StoreAsync();

                    }
                }
            }
            catch (Exception)
            {
                serialDevice.OutputStream.Dispose();
                // Couldn't instantiate the device
            }

        }


        public static void SerialDeviceError(SerialDevice sender, ErrorReceivedEventArgs e)
        {
            ;
        }
    }
}
