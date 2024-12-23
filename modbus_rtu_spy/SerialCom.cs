using System;
using System.Collections.Generic;
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
        try { return int.Parse(this.Name.Substring(3)).CompareTo(int.Parse(item.Name.Substring(3))); }
        catch { return -1; }
    }

    public static List<COMPortInfo> GetCOMPortsInfo()
    {
        List<COMPortInfo> comPortInfoList = new List<COMPortInfo>();
        ManagementScope connectionScope = new ManagementScope(@"\\.\root\CIMV2");
        connectionScope.Connect();

        ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");
        using (ManagementObjectSearcher comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery))
        {
            foreach (ManagementObject obj in comPortSearcher.Get())
            {
                string caption = obj["Caption"]?.ToString();
                if (!string.IsNullOrEmpty(caption) && (caption.Contains("(COM") || caption.Contains("(com")))
                {
                    int startIdx = caption.LastIndexOf("(COM") + 1;
                    int endIdx = caption.LastIndexOf(")");
                    string portName = caption.Substring(startIdx, endIdx - startIdx);

                    COMPortInfo comPortInfo = new COMPortInfo
                    {
                        Name = portName,
                        Description = caption
                    };
                    comPortInfoList.Add(comPortInfo);
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
        public SerialCom()
        {
        }

        public List<string> GetSerialPorts()
        {
            List<string> available_ports = new List<string>();
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
            return new List<string>() { "300", "600", "1200", "2400", "4800", "9600", "14400", "19200", "28800", "38400", "56000", "57600", "115200", "128000", "256000" };
        }

        public List<string> GetHandshake()
        {
            return new List<string>(Enum.GetNames(typeof(Handshake)));
        }
    }
}
