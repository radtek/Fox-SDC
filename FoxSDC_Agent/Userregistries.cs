﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FoxSDC_Agent
{
    class Userregistries
    {
        public static List<string> GetLoadedUserRegistries()
        {
            List<string> UserSIDs = new List<string>();

            try
            {
                using (RegistryKey hivelist = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\hivelist", false))
                {
                    if (hivelist == null)
                        return (UserSIDs);

                    foreach (string keys in hivelist.GetValueNames())
                    {
                        if (keys.ToUpper().StartsWith("\\REGISTRY\\USER\\S-") == true &&
                            keys.ToLower().EndsWith("_classes") == false)
                        {
                            UserSIDs.Add(keys.Substring(15).ToUpper());
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                FoxEventLog.WriteEventLog("Cannot list running User-Hives: " + ee.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }
            return (UserSIDs);
        }

        public static Dictionary<string, string> GetUsers()
        {
            Dictionary<string, string> Users = new Dictionary<string, string>();

            try
            {
                ManagementScope Scope;
                Scope = new ManagementScope("\\\\.\\root\\CIMV2", null);
                ObjectQuery Query = new ObjectQuery("SELECT * FROM Win32_UserAccount");
                EnumerationOptions eo = new EnumerationOptions();
                eo.Timeout = new TimeSpan(0, 0, 3, 0);
                ManagementObjectSearcher Searcher = new ManagementObjectSearcher(Scope, Query, eo);

                foreach (ManagementObject WmiObject in Searcher.Get())
                {
                    Users.Add((string)WmiObject["SID"], (string)WmiObject["Domain"] + "\\" + (string)WmiObject["Name"]);
                }
            }
            catch (Exception ee)
            {
                FoxEventLog.WriteEventLog("Cannot list users (SID): " + ee.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }
            return (Users);
        }
    }
}
