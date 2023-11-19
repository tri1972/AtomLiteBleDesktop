using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace AtomLiteBleDesktop.Bluetooth
{
    class BluetoothSender
    {
        #region Error Codes
        readonly static int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly static int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly static int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly static int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        /// <summary>
        /// Characteristicによってデータを送信
        /// </summary>
        /// <param name="characteristicstring"></param>
        /// <param name="sendData"></param>
        public async static void WriteCharacteristic(GattCharacteristic characteristicstring,string sendData)
        {
            if (!String.IsNullOrEmpty(sendData))
            {
                byte[] bytes;
                bytes = Encoding.ASCII.GetBytes(sendData);
                var writer = new DataWriter();
                writer.ByteOrder = ByteOrder.LittleEndian;


                writer.WriteString(sendData);
                var writeSuccessful = await WriteBufferToSelectedCharacteristicAsync(characteristicstring, writer.DetachBuffer());

                /*//一文字づつ送信する方法（あまり利点はない・・・）
                foreach(var bytedata in bytes)
                {
                    writer.WriteInt32(bytedata);
                    var writeSuccessful = await WriteBufferToSelectedCharacteristicAsync(characteristicstring, writer.DetachBuffer());

                }
                */
                /*
                var isValidValue =  Int32.TryParse(sendData, out int readValue);
                if (isValidValue)
                {
                    var writer = new DataWriter();
                    writer.ByteOrder = ByteOrder.LittleEndian;
                    writer.WriteInt32(readValue);

                    var writeSuccessful = await WriteBufferToSelectedCharacteristicAsync(characteristicstring,writer.DetachBuffer());
                }
                else
                {
                    Debug.WriteLine("Data to write has to be an int32");
                }
                */
            }
            else
            {
                Debug.WriteLine("No data to write to device");
            }
        }

        private async static Task<bool> WriteBufferToSelectedCharacteristicAsync(GattCharacteristic characteristicstring,IBuffer buffer)
        {
            try
            {
                // BT_Code: Writes the value from the buffer to the characteristic.
                var result = await characteristicstring.WriteValueWithResultAsync(buffer);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    Debug.WriteLine("Successfully wrote value to device");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"Write failed: {result.Status}");
                    return false;
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                // This usually happens when a device reports that it support writing, but it actually doesn't.
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
