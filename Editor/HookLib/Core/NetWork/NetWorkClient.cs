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
* Filename: NetWorkClient
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
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    public static class NetWorkClient
    {
        private static TcpClient m_client = null;
        private static Thread m_sendThread;
        private static Thread m_receiveThread;
        private static Queue<NetBase> m_sampleQueue = new Queue<NetBase>(256);
        private static Dictionary<long, Sample> m_samples = new Dictionary<long, Sample>();
        private const int PACK_HEAD = 0x23333333;
        private static NetworkStream ns;
        public static MBinaryWriter bw;
        private static BinaryReader br;


        /// <summary>
        /// 1 hook, 2 don't hook
        /// </summary>
        private static Action<int> m_onReceiveCmd;

        public static void RegisterOnReceiveCmd(Action<int> onReceive)
        {
            m_onReceiveCmd = onReceive;
        }

        public static void UnRegistReceive()
        {
            m_onReceiveCmd = null;
        }

        #region public
        public static void ConnectServer(string host, int port)
        {
            Utl.Log(string.Format("connect to {0}:{1}", host, port));
            if (m_client != null)
            {
                Utl.Log("previous connection is still exist, will be closed soon. " + m_client.Connected);
                Close();
            }
            m_client = new TcpClient();

            m_client.NoDelay = true;
            try
            {
                m_client.Connect(host, port);
                m_client.Client.SendTimeout = 1000;
                //m_sampleDict.Clear();
                m_strDict.Clear();
                m_key = 0;
                ns = m_client.GetStream();
                bw = new MBinaryWriter(ns);
                br = new BinaryReader(ns);

                m_sendThread = new Thread(new ThreadStart(DoSendMessage));
                m_sendThread.Start();

                m_receiveThread = new Thread(new ThreadStart(DoReceiveMessage));
                m_receiveThread.Start();
            }

            catch (Exception e)
            {
                Utl.Log("connect to server error: " + e.Message);
                Close();
            }
        }

        public static void Close()
        {
            try
            {
                if (m_client != null)
                {
                    if (m_client.Connected)
                    {
                        m_client.Close();
                    }
                    m_client = null;
                    Utl.Log("socket closed.");
                }
                m_sampleQueue.Clear();

                if (m_sendThread != null)
                {
                    var tmp = m_sendThread;
                    m_sendThread = null;
                    tmp.Abort();
                }
                if (m_receiveThread != null)
                {
                    var tmp = m_receiveThread;
                    m_receiveThread = null;
                    tmp.Abort();
                }
            }
            catch (Exception e) 
            {
                Utl.Log("abort thread: " + e.Message);
            }
            finally
            {
                m_strDict.Clear();
            }

        }

        //private static Dictionary<string, Sample> m_sampleDict = new Dictionary<string, Sample>(256);
        public static void AddSample(Sample sample) 
        {
            m_samples.Add(sample.seq, sample);
            Sample clone = sample.Clone();
            SendMessage(clone);
        }

        public static void SendMessage(NetBase sample)
        {
            if (m_client == null)
            {
                sample.Restore();
                return;
            }
            lock (m_sampleQueue)
            {
                m_sampleQueue.Enqueue(sample);
            }
        }
        #endregion

        #region private

        // 接受请求
        private static void DoReceiveMessage()
        {
            Console.WriteLine("<color=#00ff00>begin to listener</color>");

            //sign为true 循环接受数据
            while (true)
            {
                try
                {

                    if (m_client == null)
                    {
                        return;
                    }

                    if (ns.CanRead && ns.DataAvailable)
                    {
                        try
                        {
                            int head = br.ReadInt32();
                            if (head == PACK_HEAD)
                            {
                                int messageId = br.ReadInt32();
                                Utl.Log(string.Format("receive message id:{0}", messageId));
                                switch (messageId)
                                {
                                    case 0:
                                        {
                                            // hand shake 
                                        }
                                        break;
                                    case 1:
                                    case 2:
                                        {
                                            Utl.Log("queue count:" + m_sampleQueue.Count);
                                            if (m_onReceiveCmd != null)
                                            {
                                                m_onReceiveCmd(messageId);
                                            }
                                        }
                                        break;
                                    default:
                                        Sample sample = null;
                                        if (m_samples.TryGetValue(messageId, out sample))
                                        {
                                            SendMessage(sample);
                                        }
                                        else 
                                        {
                                            Utl.Log(string.Format("can't find sample:{0}", messageId));
                                        }
                                        break;
                                }
                            }
                        }
                        catch (Exception e) 
                        {
                            Utl.Log(e.ToString());
                        }
                        finally { }

                    }
                }
                catch (Exception e) { Utl.Log(e.ToString()); }
                finally { }
            }
        }
        private static void DoSendMessage()
        {
            while (true)
            {
                try
                {
                    if (m_client == null)
                    {
                        return;
                    }

                    while (true)
                    {

                        NetBase s = null;
                        lock (m_sampleQueue)
                        {
                            if (m_sampleQueue.Count == 0) break;
                            else if (m_sampleQueue.Count > 0)
                            {
                                s = m_sampleQueue.Dequeue();
                            }
                        }
                        bw.Write(PACK_HEAD);
                        if (s is SysInfo)
                        {
                            bw.Write((int)0);
                        }
                        if (s is Sample)
                        {
                            bw.Write((int)1);
                        }
                        else if (s is LuaRefInfo)
                        {
                            bw.Write((int)2);
                        }
                        else if (s is LuaDiffInfo)
                        {
                            bw.Write((int)3);
                        }

                        Serialize(s, bw);
                        s.Restore();

                    }
                    Thread.Sleep(10);
                }
#pragma warning disable 0168
                catch (IOException e) 
                {
                    Close();
                }
                catch (Exception e)
                {
                    Utl.Log("send msg erro:" + e.ToString());
                }
#pragma warning restore 0168
            }

        }

        private static int m_key = 0;
        public static int GetUniqueKey()
        {
            return m_key++;
        }
        private static Dictionary<string, KeyValuePair<int, byte[]>> m_strDict = new Dictionary<string, KeyValuePair<int, byte[]>>(8192);
        private static bool GetBytes(string s, out byte[] result, out int index)
        {
            bool ret = true;
            KeyValuePair<int, byte[]> keyValuePair;
            if (!m_strDict.TryGetValue(s, out keyValuePair))
            {
                result = Encoding.UTF8.GetBytes(s);
                index = GetUniqueKey();
                keyValuePair = new KeyValuePair<int, byte[]>(index, result);
                m_strDict.Add(s, keyValuePair);
                ret = false;
            }
            else
            {
                index = keyValuePair.Key;
                result = keyValuePair.Value;
            }

            return ret;
        }
        private static void Serialize(NetBase o, BinaryWriter bw)
        {
            if (o is Sample)
            {
                Sample s = (Sample)o;

                bw.Write(s.seq);
                bw.Write(s.currentTime);
                bw.Write(s.calls);
                bw.Write(s.costLuaGC);
                bw.Write(s.costMonoGC);
                WriteString(bw, s.name);

                bw.Write(s.costTime);

                bw.Write((ushort)s.childs.Count);

                var childs0 = s.childs;
                for (int i0 = 0, i0max = childs0.Count; i0 < i0max; i0++)
                {
                    Serialize(childs0[i0], bw);
                }
            }
            else if (o is SysInfo) 
            {
                SysInfo s = (SysInfo)o;
                bw.Write(s.running ? (byte)1 : (byte)0);
                bw.Write(s.hooked ? (byte)1 : (byte)0);
            }
            else if (o is LuaRefInfo)
            {
                LuaRefInfo r = (LuaRefInfo)o;

                bw.Write(r.cmd);
                WriteString(bw, r.name);
                WriteString(bw, r.addr);
                bw.Write(r.type);
            }
            else if (o is LuaDiffInfo)
            {
                LuaDiffInfo ld = (LuaDiffInfo)o;
                // add
                var addDict = ld.addRef;
                bw.Write(addDict.Count);
                foreach (var item in addDict)
                {
                    WriteString(bw, item.Key);
                    bw.Write((int)item.Value);
                }
                var addDetail = ld.addDetail;
                bw.Write(addDetail.Count);
                foreach (var item in addDetail)
                {
                    WriteString(bw, item.Key);
                    var list = item.Value;
                    bw.Write(list.Count);
                    foreach (var listItem in list)
                    {
                        WriteString(bw, listItem);
                    }
                }
                // rm
                var rmDict = ld.rmRef;
                bw.Write(rmDict.Count);
                foreach (var item in rmDict)
                {
                    WriteString(bw, item.Key);
                    bw.Write((int)item.Value);
                }
                var rmDetail = ld.rmDetail;
                bw.Write(rmDetail.Count);
                foreach (var item in rmDetail)
                {
                    WriteString(bw, item.Key);
                    var list = item.Value;
                    bw.Write(list.Count);
                    foreach (var listItem in list)
                    {
                        WriteString(bw, listItem);
                    }
                }

                // null
                var nullDict = ld.nullRef;
                bw.Write(nullDict.Count);
                foreach (var item in nullDict)
                {
                    WriteString(bw, item.Key);
                    bw.Write((int)item.Value);
                }
                var nullDetail = ld.nullDetail;
                bw.Write(nullDetail.Count);
                foreach (var item in nullDetail)
                {
                    WriteString(bw, item.Key);
                    var list = item.Value;
                    bw.Write(list.Count);
                    foreach (var listItem in list)
                    {
                        WriteString(bw, listItem);
                    }
                }
            }
        }

        public static void WriteString(BinaryWriter bw, string name)
        {
            byte[] datas;
            int index = 0;
            bool isRef = GetBytes(name, out datas, out index);
            bw.Write((byte)(isRef ? 1 : 0));
            bw.Write((short)index);
            if (!isRef)
            {
                bw.Write(datas.Length);
                bw.Write(datas);
            }
        }

        #endregion

    }

}