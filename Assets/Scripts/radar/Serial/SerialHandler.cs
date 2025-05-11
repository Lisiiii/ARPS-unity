using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using radar.data;
using Unity.Android.Gradle.Manifest;
using System.Runtime.InteropServices;
using radar.serial.crc;
using radar.serial.package;

namespace radar.serial
{
    public class SerialHandler : MonoBehaviour
    {
        public static SerialHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SerialHandler>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("SerialHandler");
                        _instance = obj.AddComponent<SerialHandler>();
                    }
                }
                return _instance;
            }
        }
        private static SerialHandler _instance;
        private SerialPort current_sp_;
        private Thread receiveThread_;

        public string[] ScanPorts()
        {
            string[] portList = SerialPort.GetPortNames();
            return portList;
        }
        public bool Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            return ConnectToPort(ref current_sp_, portName, baudRate, parity, dataBits, stopBits);
        }
        public bool ClosePort()
        {
            try
            {
                StopAllCoroutines();
                if (receiveThread_ != null && receiveThread_.IsAlive)
                {
                    receiveThread_.Abort();
                    receiveThread_ = null;
                }
                if (current_sp_ != null)
                {
                    current_sp_.Close();
                    current_sp_.Dispose();
                    current_sp_ = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return false;
            }
        }
        private bool ConnectToPort(ref SerialPort sp, string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            try
            {
                sp = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                sp.ReadTimeout = 50;
                sp.Open();

                receiveThread_ = new Thread(new ThreadStart(DataReceiveHandler));
                receiveThread_.Start();
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return false;
            }
        }

        public void SendData(int commandId, byte[] data)
        {
            if (current_sp_ == null || !current_sp_.IsOpen)
                return;

            byte[] dataToSend = packageData(commandId, data);
            try
            {
                if (current_sp_.IsOpen)
                {
                    current_sp_.Write(dataToSend, 0, dataToSend.Length);
                }
                else
                {
                    current_sp_.Open();
                    current_sp_.Write(dataToSend, 0, dataToSend.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }

        private void DataReceiveHandler()
        {
            int headerSize = Marshal.SizeOf(typeof(FrameHeader));

            while (true)
            {
                // Check if the thread is alive before proceeding
                if (current_sp_ == null || !current_sp_.IsOpen) continue;
                int count = current_sp_.BytesToRead;
                if (count < headerSize) continue;

                // Read all data in one time
                byte[] totalBuffer = new byte[Constants.FrameDataMaxLength];
                current_sp_.Read(totalBuffer, 0, count);
                byte[] headerBuffer = totalBuffer[..headerSize];

                // Check if the first byte is 0xA5 (SOF) and CRC8 is valid
                if (totalBuffer[0] != 0xA5) continue;
                if (!DjiCrc.VerifyCrc8(headerBuffer)) continue;

                // Reinterpret the header and body
                FrameHeader header = Marshal.PtrToStructure<FrameHeader>(Marshal.UnsafeAddrOfPinnedArrayElement(headerBuffer, 0));
                FrameBody body;
                body.Data = new byte[header.DataLength];

                byte[] bodyBuffer = totalBuffer[headerSize..(headerSize + header.DataLength + 2 * sizeof(ushort))];
                if (!DjiCrc.VerifyCrc16(totalBuffer[..(headerBuffer.Length + bodyBuffer.Length)])) continue;

                body = Marshal.PtrToStructure<FrameBody>(Marshal.UnsafeAddrOfPinnedArrayElement(bodyBuffer, 0));
                switch (body.CommandId)
                {
                    // case 0x0001: // Game Status
                    //     break;
                    // case 0x0003: // Game Robot HP
                    //     break;
                    // case 0x0101: // Game Buff
                    //     break;
                    // case 0x020E: // Radar Status
                    //     break;
                    // case 0x0301: // Allied Robot Info
                    //     break;
                    default:
                        Debug.Log("command ID: " + body.CommandId.ToString("X4"));
                        break;
                }

            }
        }

        private byte[] packageData(int commandId, byte[] data)
        {
            Frame frame = new();
            frame.Header.SOF = 0xA5;
            frame.Header.DataLength = (ushort)data.Length;
            frame.Header.Sequence = 0x00;
            frame.Header.Crc8 = 0x00; // Placeholder

            byte[] headerBuffer = new byte[Marshal.SizeOf(typeof(FrameHeader))];
            IntPtr headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FrameHeader)));
            try
            {
                Marshal.StructureToPtr(frame.Header, headerPtr, false);
                Marshal.Copy(headerPtr, headerBuffer, 0, headerBuffer.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(headerPtr);
            }
            DjiCrc.AppendCrc8(ref headerBuffer);

            frame.Body.CommandId = (ushort)commandId;
            frame.Body.Data = data;
            frame.Body.Crc16 = 0x00; // Placeholder

            byte[] bodyBuffer = new byte[2 * sizeof(ushort) + data.Length];
            IntPtr bodyPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FrameBody)));
            try
            {
                Marshal.StructureToPtr(frame.Body, bodyPtr, false);
                Marshal.Copy(bodyPtr, bodyBuffer, 0, bodyBuffer.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(bodyPtr);
            }
            DjiCrc.AppendCrc16(ref bodyBuffer);

            byte[] totalBuffer = new byte[headerBuffer.Length + bodyBuffer.Length];
            Buffer.BlockCopy(headerBuffer, 0, totalBuffer, 0, headerBuffer.Length);
            Buffer.BlockCopy(bodyBuffer, 0, totalBuffer, headerBuffer.Length, bodyBuffer.Length);

            return totalBuffer;
        }

        private void OnDestroy()
        {
            ClosePort();
        }
    }

}