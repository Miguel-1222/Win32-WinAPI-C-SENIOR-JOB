﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using System.Management;

namespace LicenseLib
{
    public class DeviceInfo
    {
        private string hardDiskModelStr = null;
        private string hardDiskTypeStr = null;
        private string hardDiskSerialNoStr = null;
        private string windowsProductIdStr = null;
        private string cpuIdStr = null;
        private string machineNameStr = null;
        private string macAddrStr = null;
        private string deviceHashStr = null;
        public string hardDiskModel
        {
            get { return hardDiskModelStr; }
            set { hardDiskModelStr = value; }
        }
        public string hardDiskType
        {
            get { return hardDiskTypeStr; }
            set { hardDiskTypeStr = value; }
        }
        public string hardDiskSerialNo
        {
            get { return hardDiskSerialNoStr; }
            set { hardDiskSerialNoStr = value; }
        }
        public string windowProductId
        {
            get { return windowsProductIdStr; }
            set { windowsProductIdStr = value; }
        }
        public string cpuId
        {
            get { return cpuIdStr; }
            set { cpuIdStr = value; }
        }
        public string machineName
        {
            get { return machineNameStr; }
            set { machineNameStr = value; }
        }
        public string macAddr
        {
            get { return macAddrStr; }
            set { macAddrStr = value; }
        }
        public string deviceHash
        {
            get { return deviceHashStr; }
            set { deviceHashStr = value; }
        }

        public DeviceInfo()
        {
            collectHashString();
        }

        public string collectHardDiskInfo()
        {
            // Getting Hard disk serial number...
            ManagementObjectSearcher moSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            foreach (ManagementObject wmi_HD in moSearcher.Get())
            {
                hardDiskModel = wmi_HD["Model"].ToString();  //Model Number
                hardDiskType = wmi_HD["InterfaceType"].ToString();  //Interface Type
                hardDiskSerialNo = wmi_HD["SerialNumber"].ToString(); //Serial Number
                break;
            }

            return hardDiskSerialNo;
        }

        public string collectWindowProdId()
        {
            // Getting Windows Product Key...
            byte[] id = null;
            RegistryKey localKey;
            if (Environment.Is64BitOperatingSystem)
                localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            else
                localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            var regKey = localKey.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", false);

            String strProdId = null, strWinProdKey = null;
            if (regKey != null)
            {
                id = regKey.GetValue("DigitalProductId") as byte[];

                strProdId = regKey.GetValue("ProductId") as String;
            }

            if (id != null)
            {
                WinProdKey pKey = new WinProdKey();
                strWinProdKey = pKey.DecodeKeyByteArray(id);
            }

            windowProductId = (strWinProdKey == null) ? "" : strWinProdKey;
            return windowProductId;
        }

        public string collectCPUId()
        {
            // Get CPU id..

            ManagementClass managClass = new ManagementClass("win32_processor");
            ManagementObjectCollection managCollec = managClass.GetInstances();

            string cpuInfo = null;
            foreach (ManagementObject managObj in managCollec)
            {
                cpuInfo = managObj.Properties["processorID"].Value.ToString();
                break;
            }
            cpuId = (cpuInfo == null) ? "" : cpuInfo;
            return cpuId;
        }

        public string collectComputerName()
        {
            machineName = Environment.MachineName;
            return machineName;
        }

        public string collectMACAddr()
        {
            var macAddrInfo =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();

            macAddr = macAddrInfo;
            return macAddr;
        }

        public string collectHashString()
        {
            deviceHash = getHashStringFromInfo(collectHardDiskInfo(), collectWindowProdId(), collectCPUId(), collectComputerName(), collectMACAddr());
            return deviceHash;
        }
        public string getHashStringFromInfo(string strHdSn, string strWinProdId, string strCPUId, string strName, string strMacAddr)
        {
            string text = "[" + strHdSn + "], [" + strWinProdId + "], [" +
                strCPUId + "], [" +
                /*collectComputerName()*/"" + "], [" + /*collectMACAddr()*/"" + "]";

            byte[] bytes = Encoding.Unicode.GetBytes(text);

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(bytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }

            using (SHA256Managed hashstring = new SHA256Managed())
            {
                byte[] hash = hashstring.ComputeHash(bytes);
                string hashString = string.Empty;
                foreach (byte x in hash)
                {
                    hashString += String.Format("{0:x2}", x);
                }

                return hashString;
            }
        }
    }
}
