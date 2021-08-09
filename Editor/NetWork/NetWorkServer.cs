﻿/*
               #########                       
              ############                     
              #############                    
             ##  ###########                   
            ###  ###### #####                  
            ### #######   ####                 
           ###  ########## ####                
          ####  ########### ####               
         ####   ###########  #####             
        #####   ### ########   #####           
       #####   ###   ########   ######         
      ######   ###  ###########   ######       
     ######   #### ##############  ######      
    #######  #####################  ######     
    #######  ######################  ######    
   #######  ###### #################  ######   
   #######  ###### ###### #########   ######   
   #######    ##  ######   ######     ######   
   #######        ######    #####     #####    
    ######        #####     #####     ####     
     #####        ####      #####     ###      
      #####       ###        ###      #        
        ###       ###        ###               
         ##       ###        ###               
__________#_______####_______####______________
                我们的未来没有BUG              
* ==============================================================================
* Filename: NetWorkServer
* Created:  2018/7/13 14:29:22
* Author:   エル・プサイ・コングリィ
* Purpose:  
* ==============================================================================
*/

namespace SparrowLuaProfiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    public static class NetWorkServer
    {
        private static TcpListener tcpLister;
        private static TcpClient tcpClient = null;
        private static Thread receiveThread;
        private static Thread sendThread;
        public static Thread acceptThread;
        private static NetworkStream ns;
        private static BinaryReader br;
        private static BinaryWriter bw;

        private const int PACK_HEAD = 0x23333333;
        private static Action<NetBase> m_onReceiveSample;
        private static Action<string> m_onClientConnected;
        private static Action m_onClientDisconnected;
        private static Queue<int> m_cmdQueue = new Queue<int>(32);

        public static bool CheckIsReceiving()
        {
            return tcpClient != null;
        }

        public static void RegisterOnReceiveSample(Action<NetBase> onReceive)
        {
            m_onReceiveSample = onReceive;
        }

        public static void UnRegisterReceive()
        {
            m_onReceiveSample = null;
        }
        public static void RegisterOnClientConnected(Action<string> onConnected) 
        {
            m_onClientConnected = onConnected;
        }
        public static void RegisterOnClientDisconnected(Action onDisconnected) 
        {
            m_onClientDisconnected = onDisconnected;
        }

        public static void BeginListen(string ip, int port)
        {
            if (tcpLister != null) return;

            m_strCacheDict.Clear();

            IPAddress myIP = IPAddress.Parse(ip);
            tcpLister = new TcpListener(myIP, port);
            tcpLister.Start();
            acceptThread = new Thread(AcceptThread);
            acceptThread.Start();

        }

        private static void AcceptThread()
        {
            Console.WriteLine("<color=#00ff00>begin listerner</color>");

            while (true)
            {
                AcceptAClient();
                Thread.Sleep(100);
            }
        }

        private static void AcceptAClient()
        {
            if (tcpClient != null) return;

            try
            {
                if (tcpClient == null)
                {
                    tcpClient = tcpLister.AcceptTcpClient();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("<color=#ff0000>start fail</color>");
                Close();
                return;
            }

            Console.WriteLine("<color=#00ff00>link start</color>");
            
            tcpClient.ReceiveTimeout = 1000000;
            ns = tcpClient.GetStream();
            br = new BinaryReader(ns);
            bw = new BinaryWriter(ns);
            ns.ReadTimeout = 600000;

            // 启动一个线程来接受请求
            receiveThread = new Thread(DoReceiveMessage);
            receiveThread.Start();

            SendCmd(0);
            // 启动一个线程来发送请求
            sendThread = new Thread(DoSendMessage);
            sendThread.Start();
            if (m_onClientConnected != null)
            {
                string addressInfo =((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                m_onClientConnected(addressInfo);
            }
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="cmd"></param>
        // 0 handshake,
        // 1 开启hook
        // 2 关闭hook
        // 3 断开连接

        // 100+ sample detail
        public static void SendCmd(int cmd)
        {
            lock (m_cmdQueue)
            {
                m_cmdQueue.Enqueue(cmd);
            }
        }

        // 接受请求
        private static void DoReceiveMessage()
        {
            Console.WriteLine("<color=#00ff00>begin to listener</color>");

            //sign为true 循环接受数据
            while (true)
            {
                try
                {
                    if (tcpClient == null)
                    {
                        return;
                    }

                    while (ns.CanRead && ns.DataAvailable)
                    {
                        try
                        {
                            int head = br.ReadInt32();
                            if (head == PACK_HEAD)
                            {
                                int messageId = br.ReadInt32();
                                switch (messageId)
                                {
                                    case 0: // sys info
                                        {
                                            SysInfo s = DeserializeSysInfo(br);
                                            if (m_onReceiveSample != null) 
                                            {
                                                m_onReceiveSample(s);
                                            }
                                        }
                                        break;
                                    case 1:  // sample 
                                        {
                                            long t1 = System.Diagnostics.Stopwatch.GetTimestamp(); 
                                            Sample s = Deserialize(br);
                                            long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
                                            if (m_onReceiveSample != null)
                                            {
                                                m_onReceiveSample(s);
                                            }
                                            long t3 = System.Diagnostics.Stopwatch.GetTimestamp();
                                            Console.WriteLine(string.Format("{0} {1}", t3 - t2, t2 - t1));
                                        }
                                        break;
                                    case 2: // ref info
                                            break;
                                    case 3: // diff info
                                        break;
                                }

                            }
                        }
#pragma warning disable 0168
                        catch (EndOfStreamException ex)
                        {
                            Close();
                            return;
                        }
#pragma warning restore 0168
                    }

                }
#pragma warning disable 0168
                catch (ThreadAbortException e) {
                }
                catch (Exception e)
                {
                    Close();
                }
#pragma warning restore 0168
                Thread.Sleep(5);
            }
            Console.WriteLine("<color=#00ff00>stop to listener</color>");
        }

        private static void DoSendMessage()
        {
            while (true)
            {
                try
                {
                    if (ns.CanWrite)
                    {
                        lock (m_cmdQueue)
                        {
                            while (m_cmdQueue.Count > 0)
                            {
                                int msgId = -1;
                                msgId = m_cmdQueue.Dequeue();
                                bw.Write(PACK_HEAD);
                                bw.Write(msgId);
                            }
                        }
                    }
                }
#pragma warning disable 0168
                catch (ThreadAbortException e) { }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Close();
                }
#pragma warning restore 0168
                Thread.Sleep(10);
            }
        }

        public static void RealClose()
        {
            try
            {
                if (tcpLister != null)
                {
                    tcpLister.Stop();
                    tcpLister = null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            if (acceptThread != null)
            {
                try
                {
                    acceptThread.Abort();
                }
                catch { }
                acceptThread = null;
            }
            Close();
        }

        public static void CloseClient() 
        {
            if (tcpClient != null) 
            {
                tcpClient.Close();
            }
            Close();
        }

        public static void Close()
        {
            tcpClient = null;
            if (receiveThread != null)
            {
                try
                {
                    receiveThread.Abort();
                }
                catch { }
                receiveThread = null;
            }
            if (sendThread != null)
            {
                try
                {
                    sendThread.Abort();
                }
                catch { }
                sendThread = null;
            }
            
        }

        private static Dictionary<int, string> m_strCacheDict = new Dictionary<int, string>(4096);

        public static Sample Deserialize(BinaryReader br)
        {
            Sample s = null;

            s = new Sample();
            s.seq = br.ReadInt64();
            s.currentTime = br.ReadInt64();
            s.calls = br.ReadInt32();
            s.costLuaGC = br.ReadInt32();
            s.costMonoGC = br.ReadInt32();
            s.name = ReadString(br);

            s.costTime = br.ReadInt32();

            int count = br.ReadUInt16();
            for (int i = 0, imax = count; i < imax; i++)
            {
                Deserialize(br).fahter = s;
            }

            int lua_gc = 0;
            int mono_gc = 0;
            for (int i = 0, imax = s.childs.Count; i < imax; i++)
            {
                var item = s.childs[i];
                lua_gc += item.costLuaGC;
                mono_gc += item.costMonoGC;
            }
            s.costLuaGC = Math.Max(lua_gc, s.costLuaGC);
            s.costMonoGC = Math.Max(mono_gc, s.costMonoGC);

            return s;
        }
        public static SysInfo DeserializeSysInfo(BinaryReader br) 
        {
            byte b = br.ReadByte();
            byte h = br.ReadByte();
            return new SysInfo(b > 0, h > 0);
        }

        public static LuaRefInfo DeserializeRef(BinaryReader br)
        {
            LuaRefInfo refInfo = LuaRefInfo.Create();
            refInfo.cmd = br.ReadByte();
            refInfo.frameCount = br.ReadInt32();
            refInfo.name = ReadString(br);
            refInfo.addr = ReadString(br);
            refInfo.type = br.ReadByte();

            return refInfo;
        }
        public static LuaDiffInfo DeserializeDiff(BinaryReader br)
        {
            LuaDiffInfo diffInfo = LuaDiffInfo.Create();
            int addCount = br.ReadInt32();
            for (int i = 0; i < addCount; i++)
            {
                diffInfo.PushAddRef(ReadString(br), br.ReadInt32());
            }
            int addDetailCount = br.ReadInt32();
            for (int i = 0; i < addDetailCount; i++)
            {
                string key = ReadString(br);
                int count = br.ReadInt32();
                for (int ii = 0; ii < count; ii++)
                {
                    diffInfo.PushAddDetail(key, ReadString(br));
                }
            }

            int rmCount = br.ReadInt32();
            for (int i = 0; i < rmCount; i++)
            {
                diffInfo.PushRmRef(ReadString(br), br.ReadInt32());
            }
            int rmDetailCount = br.ReadInt32();
            for (int i = 0; i < rmDetailCount; i++)
            {
                string key = ReadString(br);
                int count = br.ReadInt32();
                for (int ii = 0; ii < count; ii++)
                {
                    diffInfo.PushRmDetail(key, ReadString(br));
                }
            }

            int nullCount = br.ReadInt32();
            for (int i = 0; i < nullCount; i++)
            {
                diffInfo.PushNullRef(ReadString(br), br.ReadInt32());
            }
            int nullDetailCount = br.ReadInt32();
            for (int i = 0; i < nullDetailCount; i++)
            {
                string key = ReadString(br);
                int count = br.ReadInt32();
                for (int ii = 0; ii < count; ii++)
                {
                    diffInfo.PushNullDetail(key, ReadString(br));
                }
            }

            return diffInfo;
        }
        public static string ReadString(BinaryReader br)
        {
            string result = null;

            bool isRef = br.ReadByte() == 1 ? true : false;
            int index = br.ReadInt16();
            if (!isRef)
            {
                int len = br.ReadInt32();
                byte[] datas = br.ReadBytes(len);
                result = string.Intern(Encoding.UTF8.GetString(datas));
                m_strCacheDict[index] = result;
            }
            else
            {
                result = m_strCacheDict[index];
            }

            return result;
        }
    }

}