﻿using FoxSDC_Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FoxSDC_Agent.Push
{
    class PushMain3
    {
        class PushDataForThreadRunner
        {
            public PushData p;
            public Network net;
        }

        static bool StopThread = false;
        static Thread pm;
        const int WaitNoConnection = 120;
        const int WaitPDisNULL = 60;
        const int WaitPDisNULL2 = 10;
        const int WaitTamperIssue = 30;
        const int WaitCrash = 60;
        const int WaitQuit = 30;
        const int WaitNoClone = 30;
        static public void StartPushThread()
        {
            FoxEventLog.VerboseWriteEventLog("Push3: Starting Push Thread", System.Diagnostics.EventLogEntryType.Information);
            pm = new Thread(new ThreadStart(PushThread));
            pm.Start();
        }

        static public void StopPushThread()
        {
            FoxEventLog.VerboseWriteEventLog("Push3: Stopping Push Thread", System.Diagnostics.EventLogEntryType.Information);
            StopThread = true;
            if (pm != null)
                pm.Join();
        }

        static void PushThreadActionRunner(object o)
        {
            if (!(o is PushDataForThreadRunner))
                return;
            PushDataForThreadRunner t = (PushDataForThreadRunner)o;

            try
            {
                switch (t.p.Action)
                {
                    case "ping":
                        t.net.ResponsePushData3("ok", t.p.Action, 3, t.p.ReplyID);
                        break;
                    case "setmousedata":
                        t.net.ResponsePushData3(Redirs.MainScreenSystem.SetMousePosition(t.p.AdditionalData1), t.p.Action, 3, t.p.ReplyID);
                        break;
                    case "setkeyboarddata":
                        t.net.ResponsePushData3(Redirs.MainScreenSystem.SetKeyboard(t.p.AdditionalData1), t.p.Action, 3, t.p.ReplyID);
                        break;
                }
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.ToString());
                FoxEventLog.VerboseWriteEventLog("Push3: PushThreadActionRunner thread crashed", System.Diagnostics.EventLogEntryType.Information);
            }
        }

        static void PushThread()
        {
            Network net = null;
            PushDataRoot pd;
            int Crashes = 0;
            do
            {
                try
                {
                    if (net == null)
                        net = Utilities.ConnectNetwork(-1);

                    if (net == null)
                    {
                        FoxEventLog.VerboseWriteEventLog("Push3: no connection", System.Diagnostics.EventLogEntryType.Information);
                        for (int i = 0; i < WaitNoConnection; i++)
                        {
                            Thread.Sleep(1000);
                            if (StopThread == true)
                                return;
                        }
                        continue;
                    }
                    pd = net.GetPushData3();
                    if (pd == null)
                    {
                        FoxEventLog.VerboseWriteEventLog("Push3: pd==null", System.Diagnostics.EventLogEntryType.Information);
                        for (int i = 0; i < WaitPDisNULL; i++)
                        {
                            Thread.Sleep(1000);
                            if (StopThread == true)
                                return;
                        }
                        pd = net.GetPushData3();
                        if (pd == null)
                        {
                            net = null;
                            FoxEventLog.VerboseWriteEventLog("Push3: pd==null - 2nd time - resetting connection", System.Diagnostics.EventLogEntryType.Information);
                            for (int i = 0; i < WaitPDisNULL2; i++)
                            {
                                Thread.Sleep(1000);
                                if (StopThread == true)
                                    return;
                            }
                            continue;
                        }
                    }
                    if (ApplicationCertificate.Verify(pd) == false)
                    {
                        FoxEventLog.WriteEventLog("Push3: One or more PushData were tampered - no PushData will be processed.", System.Diagnostics.EventLogEntryType.Error);
                        for (int i = 0; i < WaitTamperIssue; i++)
                        {
                            Thread.Sleep(1000);
                            if (StopThread == true)
                                return;
                        }
                        continue;
                    }
                    Crashes = 0;

                    if (pd.Data.Action == "repeat")
                    {
                        if (StopThread == true)
                            return;
                        FoxEventLog.VerboseWriteEventLog("Push3: repeat", System.Diagnostics.EventLogEntryType.Information);
                        continue;
                    }
                    if (pd.Data.Action == "quit")
                    {
                        FoxEventLog.VerboseWriteEventLog("Push3: quit", System.Diagnostics.EventLogEntryType.Information);
                        net = null;
                        for (int i = 0; i < WaitQuit; i++)
                        {
                            Thread.Sleep(1000);
                            if (StopThread == true)
                                return;
                        }
                        continue;
                    }

                    Thread a = new Thread(new ParameterizedThreadStart(PushThreadActionRunner));
                    PushDataForThreadRunner t = new PushDataForThreadRunner();
                    t.net = net.CloneElement2();
                    t.p = pd.Data;
                    a.Start(t);
                }
                catch (Exception ee)
                {
                    Debug.WriteLine(ee.ToString());
                    FoxEventLog.VerboseWriteEventLog("Push3: SEH internally", System.Diagnostics.EventLogEntryType.Information);
                    Crashes++;
                    if (Crashes > 3)
                    {
                        FoxEventLog.VerboseWriteEventLog("Push3: Resetting connection due too many crashes", System.Diagnostics.EventLogEntryType.Information);
                        net = null;
                        Crashes = 0;
                    }
                    for (int i = 0; i < WaitCrash; i++)
                    {
                        Thread.Sleep(1000);
                        if (StopThread == true)
                            return;
                    }
                }
            } while (StopThread == false);
        }
    }
}
