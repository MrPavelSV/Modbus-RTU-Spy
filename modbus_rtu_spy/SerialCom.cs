using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Management;

internal class ProcessConnection
{
    public static ConnectionOptions ProcessConnectionOptions()
    {
        ConnectionOptions options = new ConnectionOptions
        {
            Impersonation = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.Default,
            EnablePrivileges = true
        };
        return options;
    }

    public static ManagementScope ConnectionScope(string machineName, ConnectionOptions options, string path)

    {
        ManagementScope connectScope = new ManagementScope
        {
            Path = new ManagementPath(@"\\" + machineName + path),
            Options = options
        };
        connectScope.Connect();
        return connectScope;
    }
}

public class COMPortInfo : IComparable<COMPortInfo>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public COMPortInfo() { }

    public int CompareTo(COMPortInfo item)
    {
        try { return Int32.Parse(this.Name.Substring(3)).CompareTo(Int32.Parse(item.Name.Substring(3))); } catch { return -1; }
    }

    public static List<COMPortInfo> GetCOMPortsInfo()
    {
        List<COMPortInfo> comPortInfoList = new List<COMPortInfo>();
        ConnectionOptions options = ProcessConnection.ProcessConnectionOptions();
        ManagementScope connectionScope = ProcessConnection.ConnectionScope(Environment.MachineName, options, @"\root\CIMV2");
        ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");
        ManagementObjectSearcher comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery);

        using (comPortSearcher)
        {
            string caption = null;
            foreach (ManagementObject obj in comPortSearcher.Get())
            {
                if (obj != null)
                {
                    object captionObj = obj["Caption"];
                    if (captionObj != null)
                    {
                        caption = captionObj.ToString();
                        if (caption.Contains("(COM"))
                        {
                            COMPortInfo comPortInfo = new COMPortInfo
                            {
                                Name = caption.Substring(caption.LastIndexOf("(COM")).Replace("(", string.Empty).Replace(")", string.Empty),
                                Description = caption
                            };
                            comPortInfoList.Add(comPortInfo);
                        }
                    }
                }
            }
        }
        return comPortInfoList;
    }
}

namespace modbus_rtu_spy
{
    class SerialCom
    {
        private SerialPort serialPort;

        public SerialCom()
        {
        }

        public bool Open()
        {
            serialPort = new SerialPort();
            if (!serialPort.IsOpen)
            {
                serialPort.Open();
                return true;
            }
            return false;
        }

        public List <string> GetSerialPorts()
        {
            List<int> ports = new List<int>();
            List<string> portnames = new List<string>(SerialPort.GetPortNames());
            List<string> available_ports = new List<string>();

            foreach (string portname in portnames)
            {
                ports.Add(int.Parse(portname.Substring(3)));
            }
            ports.Sort();

            List<COMPortInfo> SerialInfo = new List<COMPortInfo>(COMPortInfo.GetCOMPortsInfo());

            SerialInfo.Sort();

            foreach (COMPortInfo comPort in SerialInfo)
            {
                if (int.TryParse(comPort.Name.Substring(3), out int test))
                {
                    available_ports.Add(string.Format("{0} : \"{1}\"", comPort.Name, comPort.Description));
                }
            }
            return available_ports;
        }

        public List<string> GetParity()
        {
            return new List<string>(Enum.GetNames(typeof(Parity)));
        }

        public List<string> GetStopBits()
        {
            return new List<string>(Enum.GetNames(typeof(StopBits)));
        }

        public List<string> GetDataBits()
        {
            List<string> DataBits = new List<string>();
            for (int i = 5; i < 9; i++) { DataBits.Add(i.ToString()); }        
            return DataBits;
        }

        public List<string> GetBaudRates()
        {
            return new List<string> (){"300","600","1200","2400","4800","9600","14400","19200","28800","38400","56000","57600","115200","128000","256000"};
        }

    }
}
