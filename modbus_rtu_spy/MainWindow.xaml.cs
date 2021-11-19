using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace modbus_rtu_spy
{
    public partial class MainWindow : Window, IDisposable
    {
        public SerialPort com_spy;
        public string buff_Log;
        public string orderbyte16 = "AB";
        public string orderbyte32 = "ABCD";
        public string LogCom;
        public string RawCom;
        public bool chek_Bin = false;
        public bool chek_UInt16 = false;
        public bool chek_Int16 = false;
        public bool chek_UInt32 = false;
        public bool chek_Int32 = false;
        public bool chek_Float = false;
        public string[] orderByteL16 = { "AB", "BA" };
        public string[] orderByteL32 = { "ABCD", "ABDC", "ACBD", "ACDB", "ADBC", "ADCB", "BACD", "BADC", "BCAD", "BCDA", "BDAC", "BDCA", "CABD", "CADB", "CBAD", "CBDA", "CDAB", "CDBA", "DABC", "DACB", "DBAC", "DBCA", "DCAB", "DCBA" };
        public StreamWriter sw;
        public System.Threading.Timer timer;
        public string new_line;

        public MainWindow()
        {
            InitializeComponent();
            SerialCom sp = new SerialCom();
            new_line = Environment.NewLine;
            cbx_Port.ItemsSource = sp.GetSerialPorts();
            cbx_Parity.ItemsSource = sp.GetParity();
            cbx_StopBits.ItemsSource = sp.GetStopBits();
            cbx_Data.ItemsSource = sp.GetDataBits();
            cbx_Speed.ItemsSource = sp.GetBaudRates();
            cbx_Handshake.ItemsSource = sp.GetHandshake();
            Cbx_BO16.ItemsSource = orderByteL16;
            Cbx_BO32.ItemsSource = orderByteL32;

        }

        private void OnTimedEvent(object state)
        {
            if (!com_spy.IsOpen) { return; }
            if (com_spy.BytesToRead == 0) { return; }

            int datareceive = 0;
            byte[] dataLog;

            try
            {
                datareceive = com_spy.Read(dataLog = new byte[com_spy.BytesToRead], 0, dataLog.Length);
            }
            catch { return; }

            int k = 0;
            int l = 0;
            int numberframe = 0;
            LogCom = "";
            RawCom = "";
            List<byte[]> farmelist = new List<byte[]>();
            string DataStr = DateTime.Now.ToString("HH:mm:ss:fff") + " ";

            for (l = 0; l < (datareceive - 4); l++)
            {
                for (k = l; k < (datareceive - 4); k++)
                {
                    if ((dataLog[l] > 0) && (dataLog[l] < 249) &&
                        (dataLog[l + 1] == 0x01 ||
                         dataLog[l + 1] == 0x02 ||
                         dataLog[l + 1] == 0x03 ||
                         dataLog[l + 1] == 0x04 ||
                         dataLog[l + 1] == 0x05 ||
                         dataLog[l + 1] == 0x06 ||
                         dataLog[l + 1] == 0x0F ||
                         dataLog[l + 1] == 0x10 ||
                         dataLog[l + 1] == 0x81 ||
                         dataLog[l + 1] == 0x82 ||
                         dataLog[l + 1] == 0x83 ||
                         dataLog[l + 1] == 0x84 ||
                         dataLog[l + 1] == 0x85 ||
                         dataLog[l + 1] == 0x86 ||
                         dataLog[l + 1] == 0x8F ||
                         dataLog[l + 1] == 0x90))
                    {
                        int LenghtFrame = k + 3 - l;
                        if (LenghtFrame > 256) { break; }
                        byte[] CalcCRC = new byte[2];

                        Crc16(dataLog, LenghtFrame, l, ref CalcCRC);

                        if (CalcCRC[0] == dataLog[k + 3] && CalcCRC[1] == dataLog[k + 4])
                        {
                            byte[] temp_frame = new byte[(k + 5 - l)];
                            Array.Copy(dataLog, l, temp_frame, 0, k + 5 - l);
                            farmelist.Add(temp_frame);
                            l = k + 4;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            RawCom += new_line;
            RawCom += "[start capture]" + "----------------------------";
            RawCom += new_line;

            foreach (byte hexstr in dataLog)
            {
                RawCom += string.Format("{0:X2}", hexstr) + " ";
            }

            RawCom += new_line;
            RawCom += "[end capture] " + DataStr + "[" + datareceive + "] bytes ----------------------------";
            RawCom += new_line;

            int countbyteidx = 0;
            foreach (byte[] countframe in farmelist)
            {
                foreach (byte countbyte in countframe)
                {
                    countbyteidx++;
                }
            }

            string _ToLog = "";
            _ToLog += new_line;
            _ToLog += "[start capture]===================================================================================================";
            _ToLog += new_line;

            buff_Log = "";
            for (int i = 0; i < farmelist.Count; i++)
            {
                numberframe++;
                if ((i + 1) < farmelist.Count)
                {
                    if (farmelist[i][0] == farmelist[i + 1][0] && farmelist[i][1] == farmelist[i + 1][1])
                    {
                        if (farmelist[i].Length == 8 && 
                            (farmelist[i][1] == 0x01 || 
                             farmelist[i][1] == 0x02 || 
                             farmelist[i][1] == 0x03 || 
                             farmelist[i][1] == 0x04 ))
                        {
                            LogFrame(farmelist[i], "master=>", numberframe);
                            continue;
                        }

                        if ((farmelist[i][1] == 0x05 || farmelist[i][1] == 0x06) && farmelist[i].Length > 6 && farmelist[i + 1].Length > 6) 
                        {
                            if ((farmelist[i][2] == farmelist[i + 1][2]) &&
                                (farmelist[i][3] == farmelist[i + 1][3]) &&
                                (farmelist[i][4] == farmelist[i + 1][4]) &&
                                (farmelist[i][5] == farmelist[i + 1][5]))
                                
                            {
                                LogFrame(farmelist[i], "master=>", numberframe);
                                continue;
                            }
                        }
                        if (farmelist[i].Length > 8 && (farmelist[i][1] == 0x0F || farmelist[i][1] == 0x10))
                        {
                            LogFrame(farmelist[i], "master=>", numberframe);
                            continue;
                        }
                    }
                }
                if (i > 0)
                {
                    if (farmelist[i][0] == farmelist[i - 1][0] && farmelist[i][1] == farmelist[i - 1][1] && farmelist[i].Length != 8 && (farmelist[i][1] == 0x03 || farmelist[i][1] == 0x04))
                    {
                        if (farmelist[i][2] == (((ushort)((ushort)(farmelist[i - 1][4] << 8) | farmelist[i - 1][5])) * 2))
                        {
                            LogFrame(farmelist[i], "<= slave", numberframe);
                            continue;
                        }
                    }
                    if (farmelist[i][0] == farmelist[i - 1][0] && farmelist[i][1] == farmelist[i - 1][1] && farmelist[i].Length != 8 && (farmelist[i][1] == 0x01 || farmelist[i][1] == 0x02))
                    {
                        LogFrame(farmelist[i], "<= slave", numberframe);
                        continue;
                    }

                    if (farmelist[i][0] == farmelist[i - 1][0] && farmelist[i][1] == farmelist[i - 1][1] && farmelist[i].Length == 8 && (farmelist[i][1] == 0x10 || farmelist[i][1] == 0x0F) && (farmelist[i][5] == farmelist[i - 1][5]))
                    {
                        LogFrame(farmelist[i], "<= slave", numberframe);
                        continue;
                    }
                    if (farmelist[i][0] == farmelist[i - 1][0] && farmelist[i][1] == farmelist[i - 1][1] && (farmelist[i][1] == 0x05 || farmelist[i][1] == 0x06) && farmelist[i].Length == 8)
                    {
                        LogFrame(farmelist[i], "<= slave", numberframe);
                        continue;
                    }
                    if ((
                        (farmelist[i][1] == 0x81 && farmelist[i - 1][1] == 0x01) ||
                        (farmelist[i][1] == 0x82 && farmelist[i - 1][1] == 0x02) ||
                        (farmelist[i][1] == 0x83 && farmelist[i - 1][1] == 0x03) ||
                        (farmelist[i][1] == 0x84 && farmelist[i - 1][1] == 0x04) ||
                        (farmelist[i][1] == 0x85 && farmelist[i - 1][1] == 0x05) ||
                        (farmelist[i][1] == 0x86 && farmelist[i - 1][1] == 0x06) ||
                        (farmelist[i][1] == 0x8F && farmelist[i - 1][1] == 0x0F) ||
                        (farmelist[i][1] == 0x90 && farmelist[i - 1][1] == 0x10)) &&
                        farmelist[i][0] == farmelist[i - 1][0] &&
                        farmelist[i].Length == 5)
                    {
                        LogFrame(farmelist[i], "<= slave", numberframe);
                        continue;
                    }
                }
                LogFrame(farmelist[i], "no answer", numberframe);
            }
            _ToLog += buff_Log;
            _ToLog += new_line;
            _ToLog += "ByteOrder16 = " + orderbyte16 + " ByteOrder32 = " + orderbyte32;
            _ToLog += new_line;
            _ToLog += "[end capture]=====================================================================================================";
            _ToLog += new_line;
            _ToLog += DataStr + "[" + countbyteidx + "] bytes";
            _ToLog += new_line;
            ToLog(_ToLog);

            Dispatcher.Invoke(new Action(() =>
                {
                    textboxRaw.AppendText(RawCom);
                    textboxRaw.ScrollToEnd();
                }
            ));
        }

        private void ToLog(string text)
        {
            if (sw != null)
            {
                sw.Write(text);
            }
            Dispatcher.Invoke(new Action(() =>
            {
                textbox.AppendText(text);
                textbox.ScrollToEnd();
            }
            ));
        }

        private void LogFrame(byte[] frame, string direction, int _numberframe)
        {
            int k = 0;
            int j = 0;
            buff_Log += new_line;
            string retstring = string.Format("{0:d4}", _numberframe) + " : " + direction + " ";
            buff_Log += retstring;

            if (direction.Contains(">"))//master
            {
                buff_Log += " DEV: [" + string.Format("{0:X2}", frame[0]) + "]";
                buff_Log += " FUN: [" + string.Format("{0:X2}", frame[1]) + "]";
                if (frame[1] == 0x01 || frame[1] == 0x02)
                {
                    if (frame[1] == 0x01)
                    {
                        buff_Log += " Read Coils";
                        buff_Log += new_line;
                    }
                    if (frame[1] == 0x02)
                    {
                        buff_Log += " Read Discrete Inputs";
                        buff_Log += new_line;
                    }
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i < 4)
                        {
                            if (i == 2)
                            {
                                buff_Log += "                 START BIN : ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 3)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i > 3)
                        {
                            if (i == 4)
                            {
                                buff_Log += "                 NUM OF BIN: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 5)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                    }
                }
                if (frame[1] == 0x03 || frame[1] == 0x04)
                {
                    if (frame[1] == 0x03)
                    {
                        buff_Log += " Read Holding Registers";
                        buff_Log += new_line;
                    }
                    if (frame[1] == 0x04)
                    {
                        buff_Log += " Read Input Registers";
                        buff_Log += new_line;
                    }
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i < 4)
                        {                            
                            if (i == 2)
                            {
                                buff_Log += "                 START REG : ";
                                buff_Log += "["+ string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 3)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i-1]<<8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i > 3)
                        {      
                            if (i == 4)
                            {
                                buff_Log += "                 NUM OF REG: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 5)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i-1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                    }
                }
                if (frame[1] == 0x05)
                {
                    buff_Log += " Write Single Coil";
                    buff_Log += new_line;
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i < 4)
                        {
                            if (i == 2)
                            {
                                buff_Log += "                 ADR Coil: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 3)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC) : " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i > 3)
                        {
                            if (i == 4)
                            {
                                buff_Log += "                  OUT VAL: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 5)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "] ";
                                buff_Log += new_line;
                            }
                        }
                    }
                }
                if (frame[1] == 0x06)
                {
                    buff_Log += " Write Single Register";
                    buff_Log += new_line;
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i < 4)
                        {
                            if (i == 2)
                            {
                                buff_Log += "                 START REG: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 3)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC) : " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i > 3)
                        {
                            if (i == 4)
                            {
                                buff_Log += "                   REG VAL: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 5)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "] ";
                                if (chek_Bin == true || chek_UInt16 == true || chek_Int16 == true)
                                {
                                    if (orderbyte16 == "") { orderbyte16 = "AB"; }
                                    char[] orderbyte = orderbyte16.ToCharArray();
                                    byte[] customviewValue16 = new byte[2];
                                    customviewValue16[orderbyte[0] - 65] = frame[i];
                                    customviewValue16[orderbyte[1] - 65] = frame[i - 1];

                                    if (chek_Bin == true)
                                    {
                                        ushort vvcUInt16;
                                        try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                        string bits_str;
                                        try { bits_str = Convert.ToString(vvcUInt16, 2).PadLeft(16, paddingChar: '0'); } catch { bits_str = "_"; }
                                        buff_Log += " (BIN): [" + bits_str + "] ";
                                    }
                                    if (chek_UInt16 == true)
                                    {
                                        ushort vvcUInt16;
                                        try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                        buff_Log += "(UInt16) : " + string.Format("{0:D5}", vvcUInt16);
                                    }
                                    if (chek_Int16 == true)
                                    {
                                        short vvc_Int16;
                                        try { vvc_Int16 = BitConverter.ToInt16(customviewValue16, 0); } catch { vvc_Int16 = 0; }
                                        buff_Log += " (Int16) : " + string.Format("{0:D5}", vvc_Int16);
                                    }
                                }
                                buff_Log += new_line;
                            }
                        }
                    }
                }
                if (frame[1] == 0x10)
                {
                    buff_Log += " Write Multiple registers";
                    buff_Log += new_line;
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i < 4)
                        {
                            if (i == 2)
                            {
                                buff_Log += "                 START REG : ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 3)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i > 3)
                        {
                            if (i == 4)
                            {
                                buff_Log += "                 NUM OF REG: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 5)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i == 6)
                        {
                                buff_Log += "                 Byte Count: [";
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", frame[i]);
                                buff_Log += new_line;
                        }
                        if (i > 6)
                        {
                            if (i % 2 != 0)
                            {
                            buff_Log += "                    REG VAL: ";
                            buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i % 2 == 0 )
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "] ";
                                if (chek_Bin == true || chek_UInt16 == true || chek_Int16 == true)
                                {
                                    if (orderbyte16 == "") { orderbyte16 = "AB"; }
                                    char[] orderbyte = orderbyte16.ToCharArray();
                                    byte[] customviewValue16 = new byte[2];
                                    customviewValue16[orderbyte[0] - 65] = frame[i];
                                    customviewValue16[orderbyte[1] - 65] = frame[i - 1];

                                    if (chek_Bin == true)
                                    {
                                        ushort vvcUInt16;
                                        try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                        string bits_str;
                                        try { bits_str = Convert.ToString(vvcUInt16, 2).PadLeft(16, paddingChar: '0'); } catch { bits_str = "_"; }
                                        buff_Log += " (BIN): [" + bits_str + "] ";
                                    }
                                    if (chek_UInt16 == true)
                                    {
                                        ushort vvcUInt16;
                                        try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                        buff_Log += "(UInt16): " + string.Format("{0:D5}", vvcUInt16);
                                    }
                                    if (chek_Int16 == true)
                                    {
                                        short vvc_Int16;
                                        try { vvc_Int16 = BitConverter.ToInt16(customviewValue16, 0); } catch { vvc_Int16 = 0; }
                                        buff_Log += " (Int16): " + string.Format("{0:D5}", vvc_Int16);
                                    }
                                }
                                if ((j > 0) && ((j + 1) % 2 == 0) && ((chek_UInt32 == true) || (chek_Int32 == true) || (chek_Float == true)))
                                {
                                    if (orderbyte32 == "") { orderbyte32 = "ABCD"; }
                                    char[] orderbyte = orderbyte32.ToCharArray();
                                    byte[] customviewValue32 = new byte[4];
                                    customviewValue32[orderbyte[0] - 65] = frame[i];    //A
                                    customviewValue32[orderbyte[1] - 65] = frame[i - 1];//B
                                    customviewValue32[orderbyte[2] - 65] = frame[i - 2];//C
                                    customviewValue32[orderbyte[3] - 65] = frame[i - 3];//D

                                    if (chek_UInt32 == true)
                                    {
                                        UInt32 vvcUInt32;
                                        try { vvcUInt32 = BitConverter.ToUInt32(customviewValue32, 0); } catch { vvcUInt32 = 0; }
                                        buff_Log += " (UInt32) : " + string.Format("{0:D5}", vvcUInt32);
                                    }
                                    if (chek_Int32 == true)
                                    {
                                        Int32 vvcInt32;
                                        try { vvcInt32 = BitConverter.ToInt32(customviewValue32, 0); } catch { vvcInt32 = 0; }
                                        buff_Log += " (Int32) : " + string.Format("{0:D5}", vvcInt32);
                                    }
                                    if (chek_Float == true)
                                    {
                                        float vvcFloat;
                                        try { vvcFloat = BitConverter.ToSingle(customviewValue32, 0); } catch { vvcFloat = 0.0F; }
                                        buff_Log += " (Float) : " + vvcFloat.ToString("0.0000");
                                    }
                                }
                                j++;
                                buff_Log += new_line;
                            }
                        }
                    }
                }
                if (frame[1] == 0x0F)
                {
                    buff_Log += " Write Multiple Coils";
                    buff_Log += new_line;
                    j = 0;
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i < 4)
                        {
                            if (i == 2)
                            {
                                buff_Log += "                 START Coil : ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 3)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i > 3 && i < 6)
                        {
                            if (i == 4)
                            {
                                buff_Log += "                 NUM OF Coil: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 5)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC): " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i == 6)
                        {
                            buff_Log += "                 Byte Count : [";
                            buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                            buff_Log += " (DEC): " + string.Format("{0:d5}", frame[i]);
                            buff_Log += new_line;
                        }
                        if (i > 6)
                        {
                            string bits_str;
                            try { bits_str = Convert.ToString(frame[i], 2).PadLeft(8, paddingChar: '0'); } catch { bits_str = "_"; }
                            buff_Log += "                 " + string.Format("+{0:d4}", j) + " (HEX): [" + string.Format("{0:X2}", frame[i]) + "] (BIN): [" + bits_str + "]";
                            buff_Log += new_line;
                            j += 8;
                        }
                    }
                }
                buff_Log += "                 CRC : [" + string.Format("{0:X2}", frame[frame.Length - 2]) + " " + string.Format("{0:X2}", frame[frame.Length - 1]) + "]";
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            else if (direction.Contains("<"))//slave
            {
                buff_Log += " DEV: [" + string.Format("{0:X2}", frame[0]) + "]";
                if (frame[1] != 0x81 &&
                    frame[1] != 0x82 &&
                    frame[1] != 0x83 &&
                    frame[1] != 0x84 &&
                    frame[1] != 0x85 &&
                    frame[1] != 0x86 &&
                    frame[1] != 0x8F &&
                    frame[1] != 0x90)
                { 
                    buff_Log += " FUN: [" + string.Format("{0:X2}", frame[1]) + "]"; 
                }
                if (frame[1] == 0x03 || frame[1] == 0x04)
                {
                    buff_Log += new_line;
                    buff_Log += "                 CBD (HEX): " + string.Format("{0:X2}", frame[2]);
                    buff_Log += " (DEC): " + string.Format("{0:d3}", frame[2]);
                    buff_Log += " (REG): " + string.Format("{0:d3}", frame[2] / 2);
                    buff_Log += new_line;
                    buff_Log += "                 ANS : ";
                    buff_Log += new_line;

                    for (int i = 3; i < frame.Length - 2; i++)
                    {
                        if (k == 0)
                        {
                            buff_Log += "                 " + string.Format("+{0:d4}", j) + ":(HEX) [" + string.Format("{0:X2}", frame[i]) + " ";
                            k = 1;
                        }
                        else
                        {
                            buff_Log += string.Format("{0:X2}", frame[i]);
                            buff_Log += "] ";
                            if (chek_Bin == true || chek_UInt16 == true || chek_Int16 == true)
                            {
                                if (orderbyte16 == "") { orderbyte16 = "AB"; }
                                char[] orderbyte = orderbyte16.ToCharArray();
                                byte[] customviewValue16 = new byte[2];
                                customviewValue16[orderbyte[0] - 65] = frame[i];
                                customviewValue16[orderbyte[1] - 65] = frame[i - 1];

                                if (chek_Bin == true)
                                {
                                    ushort vvcUInt16;
                                    try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                    string bits_str;
                                    try { bits_str = Convert.ToString(vvcUInt16, 2).PadLeft(16, paddingChar: '0'); } catch { bits_str = "_"; }
                                    buff_Log += " BIN : [" + bits_str + "] ";
                                }
                                if (chek_UInt16 == true)
                                {
                                    ushort vvcUInt16;
                                    try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                    buff_Log += "(UInt16) : " + string.Format("{0:D5}", vvcUInt16);
                                }
                                if (chek_Int16 == true)
                                {
                                    short vvc_Int16;
                                    try { vvc_Int16 = BitConverter.ToInt16(customviewValue16, 0); } catch { vvc_Int16 = 0; }
                                    buff_Log += " (Int16) : " + string.Format("{0:D5}", vvc_Int16);
                                }
                            }
                            if ((j > 0) && ((j + 1) % 2 == 0) && ((chek_UInt32 == true) || (chek_Int32 == true) || (chek_Float == true)))
                            {
                                if (orderbyte32 == "") { orderbyte32 = "ABCD"; }
                                char[] orderbyte = orderbyte32.ToCharArray();
                                byte[] customviewValue32 = new byte[4];
                                customviewValue32[orderbyte[0] - 65] = frame[i];    //A
                                customviewValue32[orderbyte[1] - 65] = frame[i - 1];//B
                                customviewValue32[orderbyte[2] - 65] = frame[i - 2];//C
                                customviewValue32[orderbyte[3] - 65] = frame[i - 3];//D

                                if (chek_UInt32 == true)
                                {
                                    UInt32 vvcUInt32;
                                    try { vvcUInt32 = BitConverter.ToUInt32(customviewValue32, 0); } catch { vvcUInt32 = 0; }
                                    buff_Log += " (UInt32) : " + string.Format("{0:D5}", vvcUInt32);
                                }
                                if (chek_Int32 == true)
                                {
                                    Int32 vvcInt32;
                                    try { vvcInt32 = BitConverter.ToInt32(customviewValue32, 0); } catch { vvcInt32 = 0; }
                                    buff_Log += " (Int32) : " + string.Format("{0:D5}", vvcInt32);
                                }
                                if (chek_Float == true)
                                {
                                    float vvcFloat;
                                    try { vvcFloat = BitConverter.ToSingle(customviewValue32, 0); } catch { vvcFloat = 0.0F; }
                                    buff_Log += " (Float) : " + vvcFloat.ToString("0.0000");
                                }
                            }
                            k = 0;
                            j++;
                            buff_Log += new_line;
                        }
                    }
                }
                else if (frame[1] == 0x01 || frame[1] == 0x02)
                {
                    buff_Log += new_line;
                    buff_Log += "                 CBD (HEX): " + string.Format("{0:X2}", frame[2]);
                    buff_Log += " (DEC) : " + string.Format("{0:d3}", frame[2]);
                    buff_Log += new_line;
                    buff_Log += "                 ANS : ";
                    buff_Log += new_line;
                    j = 0;
                    for (int i = 3; i < frame.Length - 2; i++)
                    {
                        string bits_str;
                        try { bits_str = Convert.ToString(frame[i], 2).PadLeft(8, paddingChar: '0'); } catch { bits_str = "_"; }
                        buff_Log += "                 " + string.Format("+{0:d4}", j) + " (HEX):[" + string.Format("{0:X2}", frame[i]) + "] BIN : [" + bits_str + "]";
                        buff_Log += new_line;
                        j += 8;
                    }
                }
                else if (frame[1] == 0x06)
                {
                    buff_Log += new_line;
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i < 4)
                        {
                            if (i == 2)
                            {
                                buff_Log += "                 ANS : ";
                                buff_Log += new_line;
                                buff_Log += "                 ADR REG: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 3)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "]";
                                buff_Log += " (DEC) : " + string.Format("{0:d5}", (ushort)((ushort)(frame[i - 1] << 8) + frame[i]));
                                buff_Log += new_line;
                            }
                        }
                        if (i > 3)
                        {
                            if (i == 4)
                            {
                                buff_Log += "                 REG VAL: ";
                                buff_Log += "[" + string.Format("{0:X2}", frame[i]) + " ";
                            }
                            if (i == 5)
                            {
                                buff_Log += string.Format("{0:X2}", frame[i]) + "] ";
                                if (chek_Bin == true || chek_UInt16 == true || chek_Int16 == true)
                                {
                                    if (orderbyte16 == "") { orderbyte16 = "AB"; }
                                    char[] orderbyte = orderbyte16.ToCharArray();
                                    byte[] customviewValue16 = new byte[2];
                                    customviewValue16[orderbyte[0] - 65] = frame[i];
                                    customviewValue16[orderbyte[1] - 65] = frame[i - 1];

                                    if (chek_Bin == true)
                                    {
                                        ushort vvcUInt16;
                                        try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                        string bits_str;
                                        try { bits_str = Convert.ToString(vvcUInt16, 2).PadLeft(16, paddingChar: '0'); } catch { bits_str = "_"; }
                                        buff_Log += " BIN : [" + bits_str + "] ";
                                    }
                                    if (chek_UInt16 == true)
                                    {
                                        ushort vvcUInt16;
                                        try { vvcUInt16 = BitConverter.ToUInt16(customviewValue16, 0); } catch { vvcUInt16 = 0; }
                                        buff_Log += "(UInt16) : " + string.Format("{0:D5}", vvcUInt16);
                                    }
                                    if (chek_Int16 == true)
                                    {
                                        short vvc_Int16;
                                        try { vvc_Int16 = BitConverter.ToInt16(customviewValue16, 0); } catch { vvc_Int16 = 0; }
                                        buff_Log += " (Int16) : " + string.Format("{0:D5}", vvc_Int16);
                                    }
                                }
                                buff_Log += new_line;
                            }
                        }
                    }
                }
                else if (
                    frame[1] == 0x81 ||
                    frame[1] == 0x82 ||
                    frame[1] == 0x83 ||
                    frame[1] == 0x84 ||
                    frame[1] == 0x85 ||
                    frame[1] == 0x86 ||
                    frame[1] == 0x8F ||
                    frame[1] == 0x90
                    )
                {
                    buff_Log += " Error code: [" + string.Format("{0:X2}", frame[1]) + "]";
                    buff_Log += " Exception code: [" + string.Format("{0:X2}", frame[2]) + "]";
                    if (frame[2] == 0x01) { buff_Log += " ILLEGAL FUNCTION"; }
                    if (frame[2] == 0x02) { buff_Log += " ILLEGAL DATA ADDRESS"; }
                    if (frame[2] == 0x03) { buff_Log += " ILLEGAL DATA VALUE"; }
                    if (frame[2] == 0x04) { buff_Log += " SERVER DEVICE FAILURE"; }
                    if (frame[2] == 0x05) { buff_Log += " ACKNOWLEDGE"; }
                    if (frame[2] == 0x06) { buff_Log += " SERVER DEVICE BUSY"; }
                    if (frame[2] == 0x08) { buff_Log += " MEMORY PARITY ERROR"; }
                    if (frame[2] == 0x0A) { buff_Log += " GATEWAY PATH UNAVAILABLE"; }
                    if (frame[2] == 0x0B) { buff_Log += " GATEWAY TARGET DEVICE FAILED TO RESPOND"; }
                    buff_Log += new_line;
                }
                else
                {
                    buff_Log += new_line;
                    buff_Log += "                 ANS : [";
                    for (int i = 2; i < frame.Length - 2; i++)
                    {
                        if (i == frame.Length - 3)
                        {
                            buff_Log += string.Format("{0:X2}", frame[i]);
                        }
                        else
                        {
                            buff_Log += string.Format("{0:X2}", frame[i]) + " ";
                        }
                    }
                    buff_Log += "]";
                    buff_Log += new_line;
                }
                buff_Log += "                 CRC : [" + string.Format("{0:X2}", frame[frame.Length - 2]) + " " + string.Format("{0:X2}", frame[frame.Length - 1]) + "]";
            }
            else//no answer
            {
                for (int i = 0; i < frame.Length; i++)
                {
                    buff_Log += string.Format("{0:X2}", frame[i]) + " ";
                }
            }
            buff_Log += new_line;
        }

        private void OpenComPort(object sender, RoutedEventArgs e)
        {
            string comname = cbx_Port.Text;
            string baudrate = cbx_Speed.Text;
            string paritycb = cbx_Parity.Text;
            string stopbcb = cbx_StopBits.Text;
            string databits = cbx_Data.Text;

            Parity tParity;
            StopBits stopBits;
            int combd = 19200;
            try { combd = int.Parse(baudrate); }
            catch { combd = 19200; cbx_Speed.Text = "19200"; };

            int idatabits = 8;
            try { idatabits = int.Parse(databits); }
            catch { idatabits = 8; cbx_Speed.Text = "8"; };

            try
            {
                tParity = (Parity)Enum.Parse(typeof(Parity), paritycb);
            }
            catch
            {
                paritycb = "None";
                tParity = (Parity)Enum.Parse(typeof(Parity), paritycb);
                cbx_Parity.Text = paritycb;
            }

            try
            {
                stopBits = (StopBits)Enum.Parse(typeof(StopBits), stopbcb);
            }
            catch
            {
                stopbcb = "One";
                stopBits = (StopBits)Enum.Parse(typeof(StopBits), stopbcb);
                cbx_StopBits.Text = stopbcb;
            }

            if (cbx_Port.Text.Contains("COM"))
            {
                comname = cbx_Port.Text.Substring(cbx_Port.Text.IndexOf("COM"), cbx_Port.Text.IndexOf(":") - 1);
            }
            else
            {
                MessageBox.Show("Please select serial port");
                return;
            }

            com_spy = new SerialPort(comname);

            if (!com_spy.IsOpen)
            {
                com_spy.BaudRate = combd;
                com_spy.Parity = tParity;
                com_spy.DataBits = idatabits;
                com_spy.StopBits = stopBits;
                com_spy.ReadBufferSize = 8192;
                Open_port.IsEnabled = false;
                cbx_Data.IsEnabled = false;
                cbx_Parity.IsEnabled = false;
                cbx_Port.IsEnabled = false;
                cbx_Speed.IsEnabled = false;
                cbx_StopBits.IsEnabled = false;
                Capptextb.IsEnabled = false;
                Close_port.IsEnabled = true;
                com_spy.Open();

                timer = new System.Threading.Timer(OnTimedEvent);

                try
                {
                    timer.Change(0, ushort.Parse(Capptextb.Text));
                }
                catch
                {
                    Capptextb.Text = "3000";
                    timer.Change(0, ushort.Parse(Capptextb.Text));
                }
            }
        }

        private void CloseComPort(object sender, RoutedEventArgs e)
        {
            if (com_spy != null)
            {
                if (com_spy.IsOpen)
                {
                    timer.Dispose();
                    com_spy.Close();
                    com_spy.Dispose();
                    Open_port.IsEnabled = true;
                    Open_port.IsEnabled = true;
                    cbx_Data.IsEnabled = true;
                    cbx_Parity.IsEnabled = true;
                    cbx_Port.IsEnabled = true;
                    cbx_Speed.IsEnabled = true;
                    cbx_StopBits.IsEnabled = true;
                    Capptextb.IsEnabled = true;
                    Close_port.IsEnabled = false;
                }
            }
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            textbox.Clear();
            textboxRaw.Clear();
            textbox.AppendText("Clear=======================================================================================================================");
            textboxRaw.AppendText("Clear=======================================================================================================================");
        }

        private void Capptextb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ushort.TryParse(Capptextb.Text, out ushort num))
            {
                if (num > 10000) { Capptextb.Text = "10000"; }
                else { Capptextb.Text = Capptextb.Text; }
            }
            else
            {
                Capptextb.Text = "3000";
            }
        }

        public static void Crc16(byte[] data, int length, int offset, ref byte[] crc)
        {
            byte[] aucCRCHi = {
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40
            };

            byte[] aucCRCLo = {
            0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7,
            0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E,
            0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09, 0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9,
            0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC,
            0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
            0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32,
            0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D,
            0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A, 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38,
            0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF,
            0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
            0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1,
            0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4,
            0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F, 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB,
            0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA,
            0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
            0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0,
            0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97,
            0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C, 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E,
            0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89,
            0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
            0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83,
            0x41, 0x81, 0x80, 0x40
            };

            //if (offset > length) throw new ArgumentOutOfRangeException();

            byte ucCRCHi = 0xFF, ucCRCLo = 0xFF;
            int idx;

            int i = offset;
            int len = length;
            while (len-- > 0)
            {
                idx = ucCRCLo ^ data[i++];
                ucCRCLo = (byte)(ucCRCHi ^ aucCRCHi[idx]);
                ucCRCHi = aucCRCLo[idx];
            }

            crc[0] = ucCRCLo;
            crc[1] = ucCRCHi;
        }

        public void Dispose()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                return;

            if (disposing)
            {
                timer.Dispose();
                com_spy.Dispose();
            }
        }

        private void chbUInt16_Checked(object sender, RoutedEventArgs e)
        {
            chek_UInt16 = true;
        }

        private void chbUInt16_Unchecked(object sender, RoutedEventArgs e)
        {
            chek_UInt16 = false;
        }

        private void chb_Int16_Checked(object sender, RoutedEventArgs e)
        {
            chek_Int16 = true;
        }

        private void chb_Int16_Unchecked(object sender, RoutedEventArgs e)
        {
            chek_Int16 = false;
        }

        private void chbUInt32_Checked(object sender, RoutedEventArgs e)
        {
            chek_UInt32 = true;
        }

        private void chbUInt32_Unchecked(object sender, RoutedEventArgs e)
        {
            chek_UInt32 = false;
        }

        private void chb_Int32_Checked(object sender, RoutedEventArgs e)
        {
            chek_Int32 = true;
        }

        private void chb_Int32_Unchecked(object sender, RoutedEventArgs e)
        {
            chek_Int32 = false;
        }

        private void chb_Float_Checked(object sender, RoutedEventArgs e)
        {
            chek_Float = true;
        }

        private void chb_Float_Unchecked(object sender, RoutedEventArgs e)
        {
            chek_Float = false;
        }

        private void chb_Bin_Checked(object sender, RoutedEventArgs e)
        {
            chek_Bin = true;
        }

        private void chb_Bin_Unchecked(object sender, RoutedEventArgs e)
        {
            chek_Bin = false;
        }

        private void Cbx_BO16_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            orderbyte16 = Cbx_BO16.SelectedItem.ToString();
        }

        private void Cbx_BO32_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            orderbyte32 = Cbx_BO32.SelectedItem.ToString();
        }

        private void savetofile_Checked(object sender, RoutedEventArgs e)
        {
            string DataStrfile = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff") + ".txt";
            sw = new StreamWriter(DataStrfile);
        }

        private void savetofile_Unchecked(object sender, RoutedEventArgs e)
        {
            sw.Close();
        }
    }
}
