using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using radar.data;
using Unity.Android.Gradle.Manifest;

namespace radar.serial
{
    public class SerialHandler : MonoBehaviour
    {
        string[] ports;
        SerialPort current_sp;
        private byte[] receivedData = new byte[1024];
        public DataManager dataManager_;
        void Start()
        {
            // Debug.Log("--- Serial Init ---");
            // ports = ScanPorts();
            // Debug.Log("Scaned ports count: " + ports.Length);
            // foreach (string port in ports)
            // {
            //     if (port.Contains("COM1"))
            //     {
            //         if (ConnectToPort(ref current_sp, port, 9600, Parity.None, 8, StopBits.One))
            //         {
            //             Debug.Log("Connect to " + port);
            //             StartCoroutine(SendDataToSerialPort());
            //         }
            //     }
            // }
        }
        void Update()
        {
        }

        // send data to serial port every 2 seconds (DEBUG) ---->
        IEnumerator SendDataToSerialPort()
        {
            while (true)
            {
                yield return new WaitForSeconds(2);
                SendData(ref current_sp, "Hello");
                Debug.Log("Send data to " + current_sp.PortName);
            }
        }
        // send data to serial port every 2 seconds (DEBUG) <----

        public string[] ScanPorts()
        {
            string[] portList = SerialPort.GetPortNames();
            return portList;
        }
        public bool Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            return ConnectToPort(ref current_sp, portName, baudRate, parity, dataBits, stopBits);
        }
        public bool ClosePort()
        {
            try
            {
                if (current_sp.IsOpen)
                    current_sp.Close();
                StopAllCoroutines();
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
                sp.Open();
                Thread thread = new Thread(new ThreadStart(DataReceivedHandler));
                thread.Start();
                // DEBUG
                StartCoroutine(SendDataToSerialPort());
                // DEBUG
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return false;
            }
        }

        private void DataReceivedHandler()
        {
            while (true)
            {
                if (current_sp.IsOpen)
                {
                    int count = current_sp.BytesToRead;
                    if (count > 0)
                    {
                        byte[] readBuffer = new byte[count];
                        try
                        {
                            current_sp.Read(readBuffer, 0, count);
                            // StringBuilder sb = new StringBuilder();
                            // for (int i = 0; i < readBuffer.Length; i++)
                            // {
                            //     sb.AppendFormat("{0:x2}" + "", readBuffer[i]);
                            // }
                            // Debug.Log(sb.ToString());
                            receivedData = readBuffer;
                            StateDatas receivedStateDatas = ParseData(receivedData);
                            if (receivedStateDatas != null)
                            {
                                dataManager_.updatedStateQueue_.Enqueue(receivedStateDatas);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex.Message);
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        private StateDatas ParseData(byte[] readBuffer)
        {
            if (receivedData.Length > 0)
            {
                // Debug.Log("Received data: " + receivedData.Length);
                StateDatas stateData = new StateDatas();
                // TODO : Fill the stateData object with parsed data
                return stateData;
            }
            else
            {
                // Debug.Log("Received data is empty");
                return null;
            }
        }

        private void SendData(ref SerialPort sp, string _info)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.WriteLine(_info);
                }
                else
                {
                    sp.Open();
                    sp.WriteLine(_info);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }
        private void SendData(ref SerialPort sp, byte[] send, int offSet, int count)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Write(send, offSet, count);
                }
                else
                {
                    sp.Open();
                    sp.Write(send, offSet, count);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }

    }

}