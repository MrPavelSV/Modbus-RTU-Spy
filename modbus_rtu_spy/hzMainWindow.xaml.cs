using System;
using System.Windows;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

namespace modbus_rtu_spy
{
    public partial class MainWindow : Window , IDisposable
    {
        public SerialPort com_spy;
        public string LogCom;
        public string RawCom;
        //public byte[] dataLog;
        public int numberframe;
        public System.Threading.Timer timer;

        public MainWindow()
        {
            InitializeComponent();
            SerialCom sp = new SerialCom();
            numberframe = 0;
            cbx_Port.ItemsSource = sp.GetSerialPorts();
            cbx_Parity.ItemsSource = sp.GetParity();
            cbx_StopBits.ItemsSource = sp.GetStopBits();
            cbx_Data.ItemsSource = sp.GetDataBits();
            cbx_Speed.ItemsSource = sp.GetBaudRates();
        }

        private void OnTimedEvent(object state)
        {
            if (!com_spy.IsOpen) return;
            byte[] dataLog;
            int conuntByteread = com_spy.BytesToRead;
            dataLog = new byte[conuntByteread];
            com_spy.Read(dataLog, 0, conuntByteread);
            if (dataLog.Length == 0) return;

            int k = 0;
            int l = 0;
            numberframe = 0;
            LogCom = "";
            RawCom = "";
            List<byte[]> farmelist = new List<byte[]>();
            byte[] CalcCRC = new byte[2];

            for (l = 0; l < (conuntByteread - 5); )
            {
                for (k = l; k < (conuntByteread - 5); k++)
                {
                    if (((dataLog[l] > 0) && (dataLog[l] < 249)) &&
                        (dataLog[(l + 1)] == 0x01 ||
                         dataLog[(l + 1)] == 0x02 ||
                         dataLog[(l + 1)] == 0x03 ||
                         dataLog[(l + 1)] == 0x04 ||
                         dataLog[(l + 1)] == 0x05 ||
                         dataLog[(l + 1)] == 0x06 ||
                         dataLog[(l + 1)] == 0x0F ||
                         dataLog[(l + 1)] == 0x10 ||
                         dataLog[(l + 1)] == 0x81 ||
                         dataLog[(l + 1)] == 0x82 ||
                         dataLog[(l + 1)] == 0x83 ||
                         dataLog[(l + 1)] == 0x84 ||
                         dataLog[(l + 1)] == 0x85 ||
                         dataLog[(l + 1)] == 0x8F ||
                         dataLog[(l + 1)] == 0x90))
                    {
                        int LenghtFrame = (k + 3 - l);
                        if (LenghtFrame > 256) { break; }

                        Crc16(dataLog, LenghtFrame, l, ref CalcCRC);

                        if (CalcCRC[0] == dataLog[k + 3] && CalcCRC[1] == dataLog[k + 4])
                        {
                            byte[] temp_frame = new byte[((k + 5) - l)];
                            Array.Copy(dataLog, l, temp_frame, 0, ((k + 5) - l));
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
            foreach (var hexstr in dataLog)
            {
                RawCom += string.Format("{0:X2}", hexstr) + " ";
            }

            RawCom += Environment.NewLine;
            RawCom += "----------------------------";
            RawCom += Environment.NewLine;

            int countbyteidx = 0;
            foreach (var countframe in farmelist)
            {
                foreach (var countbyte in countframe)
                {
                    countbyteidx++;
                }
            }

            for (int i = 0; i < farmelist.Count; i++)
            {
                numberframe++;
                if ((i + 1) < farmelist.Count) {
                    if (farmelist[i][0] == farmelist[i + 1][0] && farmelist[i][1] == farmelist[i + 1][1] && farmelist[i].Length == 8)
                    {
                        LogCom += DateTime.Now.ToString("HH:mm:ss:fff") + " " + string.Format("{0:d4}", numberframe) + " : > ";
                        foreach (var strhex in farmelist[i])
                        {
                            LogCom += string.Format("{0:X2}", strhex) + " ";
                        }
                        LogCom += Environment.NewLine;
                        numberframe++;
                        LogCom += DateTime.Now.ToString("HH:mm:ss:fff") + " "+ string.Format("{0:d4}", numberframe) + " : < ";
                        foreach (var strhex in farmelist[i + 1])
                        {
                            LogCom += string.Format("{0:X2}", strhex) + " ";
                        }
                        LogCom += Environment.NewLine;
                        i++;
                    }
                }
                else
                {
                    LogCom += DateTime.Now.ToString("HH:mm:ss:fff") + " " + string.Format("{0:d4}", numberframe) + " : ? ";
                    foreach (var strhex in farmelist[i])
                    {
                        LogCom += string.Format("{0:X2}", strhex) + " ";
                    }
                    LogCom += Environment.NewLine;
                }
            }

            LogCom += Environment.NewLine;
            LogCom += "----------------------------";
            LogCom += Environment.NewLine;

            Dispatcher.Invoke(new Action(() =>
                {
                    countidxlabel.Content = countbyteidx;
                    lastframeidxlabel.Content = dataLog.Length;
                    rtextboxRaw.AppendText(RawCom);
                    rtextbox.AppendText(LogCom);
                    rtextbox.ScrollToEnd();
                    rtextboxRaw.ScrollToEnd();
                }
            ));
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
                comname = cbx_Port.Text.Substring(cbx_Port.Text.IndexOf("COM"), cbx_Port.Text.IndexOf(":")-1);
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
            rtextbox.Document.Blocks.Clear();
            rtextboxRaw.Document.Blocks.Clear();
            numberframe = 0;
        }

        private void Capptextb_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
           if (ushort.TryParse(Capptextb.Text, out ushort num))
            {
                if (num > 10000) { num = 10000; Capptextb.Text = "10000"; }
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
            disposing = true;
        }
    }
}
