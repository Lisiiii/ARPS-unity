using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using radar.data;
using System.Runtime.InteropServices;
using radar.serial.crc;
using radar.serial.package;
using System.Linq;
using Unity.VisualScripting;

namespace radar.serial
{
    public class SerialHandler : MonoBehaviour
    {
        public static SerialHandler Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = FindAnyObjectByType<SerialHandler>();
                    if (instance_ == null)
                    {
                        GameObject obj = new("SerialHandler");
                        instance_ = obj.AddComponent<SerialHandler>();
                    }
                }
                return instance_;
            }
        }
        private static SerialHandler instance_;
        private SerialPort current_sp_;
        private Thread receiveThread_;
        public bool isConnected => current_sp_ != null && current_sp_.IsOpen;

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

                LogManager.Instance.log("[SerialHandler]Serial port closed.");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.error(ex.ToString());
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

                LogManager.Instance.log($"[SerialHandler]Serial port opened: {portName}");
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.error($"[SerialHandler]Failed to open serial port: {ex}");
                return false;
            }
        }

        public void SendData(ushort commandId, byte[] data)
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
                LogManager.Instance.error("[SerialHandler]" + ex.ToString());
            }
        }

        private bool readBytesUntilFilled(List<byte> bufferList, int count, bool isReadingHead = false)
        {
            if (current_sp_ == null || !current_sp_.IsOpen) return false;

            if (isReadingHead)
            {
                while (true)
                {
                    if (current_sp_.BytesToRead == 0) { Thread.Sleep(1); continue; }
                    byte[] buffer = new byte[current_sp_.BytesToRead];
                    try
                    {
                        current_sp_.Read(buffer, 0, buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.error("[SerialHandler]" + ex.ToString());
                        return false;
                    }
                    if (buffer[0] != 0xA5)
                        continue;
                    else
                    {
                        bufferList.AddRange(buffer);
                        break;
                    }
                }
            }
            while (bufferList.Count < count)
            {
                if (current_sp_.BytesToRead == 0) { Thread.Sleep(1); continue; }

                byte[] buffer = new byte[current_sp_.BytesToRead];
                try
                {
                    current_sp_.Read(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    LogManager.Instance.error("[SerialHandler]" + ex.ToString());
                    return false;
                }
                bufferList.AddRange(buffer);
            }
            return true;
        }



        private byte[] packageData(ushort commandId, byte[] data)
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

            byte[] totalBuffer = new byte[headerBuffer.Length + bodyBuffer.Length];
            Buffer.BlockCopy(headerBuffer, 0, totalBuffer, 0, headerBuffer.Length);
            Buffer.BlockCopy(bodyBuffer, 0, totalBuffer, headerBuffer.Length, bodyBuffer.Length);

            DjiCrc.AppendCrc16(ref totalBuffer);

            return totalBuffer;
        }

        private void DataReceiveHandler()
        {
            int headerSize = Marshal.SizeOf(typeof(FrameHeader));
            List<byte> totalBuffer = new();

            while (true)
            {
                totalBuffer.Clear();
                if (!readBytesUntilFilled(totalBuffer, headerSize, isReadingHead: true)) continue;
                byte[] headerBuffer = totalBuffer.Take(headerSize).ToArray();

                // Check if the first byte is 0xA5 (SOF) and CRC8 is valid
                if (totalBuffer[0] != 0xA5)
                {
                    LogManager.Instance.warning("[SerialHandler]Invalid header or CRC8");
                    continue;
                }
                if (!DjiCrc.VerifyCrc8(headerBuffer))
                {
                    continue;
                }

                // Reinterpret the header and body
                FrameHeader header = Marshal.PtrToStructure<FrameHeader>(Marshal.UnsafeAddrOfPinnedArrayElement(headerBuffer, 0));

                if (!readBytesUntilFilled(totalBuffer, headerSize + header.DataLength + 2 * sizeof(ushort))) continue;

                FrameBody body = new();
                body.Data = new byte[header.DataLength];
                byte[] bodyBuffer = totalBuffer.Skip(headerSize).Take(header.DataLength + 2 * sizeof(ushort)).ToArray();

                if (!DjiCrc.VerifyCrc16(totalBuffer.Take(headerSize + bodyBuffer.Length).ToArray()))
                {
                    LogManager.Instance.warning("[SerialHandler]Invalid body CRC16");
                    continue;
                }

                body = Marshal.PtrToStructure<FrameBody>(Marshal.UnsafeAddrOfPinnedArrayElement(bodyBuffer, 0));

                switch (body.CommandId)
                {
                    case 0x0001: // Game Status
                        getGameStatus(body.Data);
                        break;
                    case 0x0003: // Game Robot HP
                        getGameRobotHp(body.Data);
                        break;
                    case 0x0101: // Game Buff
                        getGameBuff(body.Data);
                        break;
                    case 0x020E: // Radar Status
                        getRadarStatus(body.Data);
                        break;
                    // case 0x0301: // Allied Robot Info
                    //     break;
                    default:
                        // LogManager.Instance.log($"[SerialHandler]CMD_ID:{body.CommandId:X4}");
                        break;
                }
            }
        }
        private void getGameStatus(byte[] data)
        {
            IntPtr dataPtr = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            GameStatus gameStatus = Marshal.PtrToStructure<GameStatus>(dataPtr);
            DataManager.Instance.UploadData(gameStatus,
                (gameStatus) =>
                {
                    DataManager.Instance.stateData.gameState.GameStage = gameStatus.Stage;
                    DataManager.Instance.stateData.gameState.GameTimeSeconds = gameStatus.StageRemainTime;
                    DataManager.Instance.lastRecordTime = DateTime.Now;
                    DataManager.Instance.lastRecordTimeSeconds = gameStatus.StageRemainTime;
                }
            );

            LogManager.Instance.log("[SerialHandler] <0x0001> GameStatus: " +
                "GameType: " + gameStatus.GameType.ToString() +
                ", GameStage: " + gameStatus.Stage.ToString() +
                ", StageRemainTime: " + gameStatus.StageRemainTime.ToString() +
                ", SyncTimestamp: " + gameStatus.SyncTimestamp.ToString());
        }

        private void getGameRobotHp(byte[] data)
        {
            IntPtr dataPtr = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            GameRobotHp gameRobotHp = Marshal.PtrToStructure<GameRobotHp>(dataPtr);
            DataManager.Instance.UploadData(gameRobotHp, UpdateRobotHp);

            LogManager.Instance.log("[SerialHandler] <0x0003> GameRobotHp: " +
                "Red1: " + gameRobotHp.Red1.ToString() +
                ", Red2: " + gameRobotHp.Red2.ToString() +
                ", Red3: " + gameRobotHp.Red3.ToString() +
                ", Red4: " + gameRobotHp.Red4.ToString() +
                ", Red5: " + gameRobotHp.Red5.ToString() +
                ", Red7: " + gameRobotHp.Red7.ToString() +
                ", RedOutpost: " + gameRobotHp.RedOutpost.ToString() +
                ", RedBase: " + gameRobotHp.RedBase.ToString() +
                ", Blue1: " + gameRobotHp.Blue1.ToString() +
                ", Blue2: " + gameRobotHp.Blue2.ToString() +
                ", Blue3: " + gameRobotHp.Blue3.ToString() +
                ", Blue4: " + gameRobotHp.Blue4.ToString() +
                ", Blue5: " + gameRobotHp.Blue5.ToString() +
                ", Blue7: " + gameRobotHp.Blue7.ToString() +
                ", BlueOutpost: " + gameRobotHp.BlueOutpost.ToString() +
                ", BlueBase: " + gameRobotHp.BlueBase.ToString());
        }

        private void UpdateRobotHp(GameRobotHp gameRobotHp)
        {
            bool isEnemyRedSide = DataManager.Instance.stateData.gameState.EnemySide == Team.Red;
            DataManager.Instance.stateData.enemyRobots.Data[RobotType.Hero].HP = isEnemyRedSide ? gameRobotHp.Red1 : gameRobotHp.Blue1;
            DataManager.Instance.stateData.enemyRobots.Data[RobotType.Engineer].HP = isEnemyRedSide ? gameRobotHp.Red2 : gameRobotHp.Blue2;
            DataManager.Instance.stateData.enemyRobots.Data[RobotType.Infantry3].HP = isEnemyRedSide ? gameRobotHp.Red3 : gameRobotHp.Blue3;
            DataManager.Instance.stateData.enemyRobots.Data[RobotType.Infantry4].HP = isEnemyRedSide ? gameRobotHp.Red4 : gameRobotHp.Blue4;
            DataManager.Instance.stateData.enemyRobots.Data[RobotType.Infantry5].HP = isEnemyRedSide ? gameRobotHp.Red5 : gameRobotHp.Blue5;
            DataManager.Instance.stateData.enemyRobots.Data[RobotType.Sentry].HP = isEnemyRedSide ? gameRobotHp.Red7 : gameRobotHp.Blue7;
            DataManager.Instance.stateData.enemyFacilities.Data[RobotType.Outpost].HP = isEnemyRedSide ? gameRobotHp.RedOutpost : gameRobotHp.BlueOutpost;
            DataManager.Instance.stateData.enemyFacilities.Data[RobotType.Base].HP = isEnemyRedSide ? gameRobotHp.RedBase : gameRobotHp.BlueBase;
            DataManager.Instance.stateData.allieRobots.Data[RobotType.Hero].HP = isEnemyRedSide ? gameRobotHp.Blue1 : gameRobotHp.Red1;
            DataManager.Instance.stateData.allieRobots.Data[RobotType.Engineer].HP = isEnemyRedSide ? gameRobotHp.Blue2 : gameRobotHp.Red2;
            DataManager.Instance.stateData.allieRobots.Data[RobotType.Infantry3].HP = isEnemyRedSide ? gameRobotHp.Blue3 : gameRobotHp.Red3;
            DataManager.Instance.stateData.allieRobots.Data[RobotType.Infantry4].HP = isEnemyRedSide ? gameRobotHp.Blue4 : gameRobotHp.Red4;
            DataManager.Instance.stateData.allieRobots.Data[RobotType.Infantry5].HP = isEnemyRedSide ? gameRobotHp.Blue5 : gameRobotHp.Red5;
            DataManager.Instance.stateData.allieRobots.Data[RobotType.Sentry].HP = isEnemyRedSide ? gameRobotHp.Blue7 : gameRobotHp.Red7;
            DataManager.Instance.stateData.allieFacilities.Data[RobotType.Outpost].HP = isEnemyRedSide ? gameRobotHp.BlueOutpost : gameRobotHp.RedOutpost;
            DataManager.Instance.stateData.allieFacilities.Data[RobotType.Base].HP = isEnemyRedSide ? gameRobotHp.BlueBase : gameRobotHp.RedBase;
        }

        private void getRadarStatus(byte[] data)
        {
            IntPtr dataPtr = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            package.RadarInfo radarInfo = Marshal.PtrToStructure<package.RadarInfo>(dataPtr);
            DataManager.Instance.UploadData(radarInfo, (radarInfo) =>
            {
                DataManager.Instance.stateData.radarInfo.DoubleDebuffChances = radarInfo.DoubleDebuffChances;
                DataManager.Instance.stateData.radarInfo.IsDoubleDebuffAble = radarInfo.IsDoubleDebuffAble;
            });
            LogManager.Instance.log("[SerialHandler] <0x020E> RadarStatus: " +
                "DoubleDebuffChances: " + radarInfo.DoubleDebuffChances.ToString() +
                ",IsDoubleDebuffAble: " + radarInfo.IsDoubleDebuffAble.ToString());
        }

        private void getGameBuff(byte[] data)
        {
            IntPtr dataPtr = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            EventData gameBuff = Marshal.PtrToStructure<EventData>(dataPtr);
            LogManager.Instance.log("[SerialHandler] <0x0101> GameBuff: " +
                "IsSupplyAreaOccupied: " + gameBuff.IsSupplyAreaOccupied.ToString() +
                ", IsSupplyAreaOccupied2: " + gameBuff.IsSupplyAreaOccupied2.ToString() +
                ", IsSupplyAreaOccupied3: " + gameBuff.IsSupplyAreaOccupied3.ToString() +
                ", IsLittleEnergyOrganActivated: " + gameBuff.IsLittleEnergyOrganActivated.ToString() +
                ", IsBigEnergyOrganActivated: " + gameBuff.IsBigEnergyOrganActivated.ToString() +
                ", IsCentralHighlandOccupied: " + gameBuff.IsCentralHighlandOccupied.ToString() +
                ", IsTrapezoidalHighlandOccupied: " + gameBuff.IsTrapezoidalHighlandOccupied.ToString() +
                ", EnemyDartHitTime: " + gameBuff.EnemyDartHitTime.ToString() +
                ", EnemyDartHitTarget: " + gameBuff.EnemyDartHitTarget.ToString() +
                ", CenterGainPointStatus: " + gameBuff.CenterGainPointStatus.ToString());
        }

        private void getAlliedRobotInfo(byte[] data)
        {
            // TODO: Implement this function
        }


        private void OnDestroy()
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
        }

    }

}