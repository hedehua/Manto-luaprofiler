/*
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
        private static Queue<Cmd> m_cmdQueue = new Queue<Cmd>(32);

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

            NetUtil.Clear();

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

            SendCmd(Cmd.handshake);
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
        public static void SendCmd(int cmd, int arg = 0)
        {
            lock (m_cmdQueue)
            {
                m_cmdQueue.Enqueue(new Cmd(cmd, arg));
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
                                            SysInfo s = NetUtil.DeserializeSysInfo(br);
                                            if (m_onReceiveSample != null) 
                                            {
                                                m_onReceiveSample(s);
                                            }
                                        }
                                        break;
                                    case 1:  // sample 
                                        {
                                            long t1 = System.Diagnostics.Stopwatch.GetTimestamp(); 
                                            Sample s = NetUtil.Deserialize(br);
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
                                    case 3: // full memory info
                                        {
                                            LuaFullMemory luaFullMemory = NetUtil.DeserializeLuaFullMemory(br);
                                            if (m_onReceiveSample != null) 
                                            {
                                                m_onReceiveSample(luaFullMemory);
                                            }
                                        }
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
                                Cmd cmd = m_cmdQueue.Dequeue();
                                bw.Write(PACK_HEAD);
                                bw.Write(cmd.msgId);
                                bw.Write(cmd.arg);
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
            if (m_onClientDisconnected != null) m_onClientDisconnected();
        }

     
    }

}