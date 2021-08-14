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
* Filename: Sample
* Created:  2018/7/13 14:29:22
* Author:   エル・プサイ・コングリィ
* Purpose:  
* ==============================================================================
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SparrowLuaProfiler
{
    public class NetUtil 
    {
        public static void Clear() 
        {
            m_strCacheDict.Clear();
            m_strDict.Clear();
            m_key = 0;
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
        public static void Serialize(NetBase o, BinaryWriter bw)
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
                bw.Write(s.state_created ? (byte)1 : (byte)0);
                bw.Write(s.hooked ? (byte)1 : (byte)0);
                bw.Write(s.memory_hooked ? (byte)1 : (byte)0);
            }
            else if (o is LuaRefInfo)
            {
                LuaRefInfo r = (LuaRefInfo)o;

                WriteString(bw, r.name);
                bw.Write(r.addr);
                bw.Write(r.size);
            }
            else if (o is LuaFullMemory)
            {
                LuaFullMemory f = (LuaFullMemory)o;
                WriteString(bw, f.fullPath);
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

        public static LuaRefInfo DeserializeLuaRefInfo(BinaryReader br)
        {
            string name = ReadString(br);
            long addr = br.ReadInt64();
            int size = br.ReadInt32();
            return LuaRefInfo.Create(name, addr, size);
        }

        public static SysInfo DeserializeSysInfo(BinaryReader br)
        {
            byte b = br.ReadByte();
            byte h = br.ReadByte();
            byte m = br.ReadByte();
            return new SysInfo(b > 0, h > 0, m > 0);
        }

        public static LuaFullMemory DeserializeLuaFullMemory(BinaryReader br)
        {
            string path = ReadString(br);
            return new LuaFullMemory(path);
        }

        public static LuaRefInfo DeserializeRef(BinaryReader br)
        {
            LuaRefInfo refInfo = LuaRefInfo.Create();

            refInfo.name = ReadString(br);
            refInfo.addr = br.ReadInt64();
            refInfo.addr = br.ReadInt32();

            return refInfo;
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
                result = m_strCacheDict[index];  // TODO:: 1. 这里有报错 2. 显示的问题
            }

            return result;
        }
    }
    public abstract class NetBase
    {
        public abstract void Restore();
    }
    public class Cmd : NetBase
    {

        public const int handshake = 0;
        public const int disconnect = 1;
        public const int input = 2; // 0 unhook, 1 hook, 2 memory hook, 3, memory unhook, 4, capture
        public const int seq = 4;

        public int msgId;
        public int arg;
        public override void Restore()
        {
            
        }
        public Cmd(int t, int a) 
        {
            this.msgId = t;
            this.arg = a;
        }
    }
    public class SysInfo : NetBase
    {
        public bool state_created; // 0 state closed, 1 state created
        public bool hooked;
        public bool memory_hooked;
        public override void Restore() 
        {
           
        }
        public SysInfo(bool c, bool h, bool m) 
        {
            hooked = h;
            state_created = c;
            memory_hooked = m;
        }
    }

    public class LuaRefInfo : NetBase
    {
        #region field
        public string name;
        public long addr;
        public int size;
        #endregion

        #region pool
        private static ObjectPool<LuaRefInfo> m_pool = new ObjectPool<LuaRefInfo>(32);
        public static LuaRefInfo Create()
        {
            LuaRefInfo r = m_pool.GetObject();
            return r;
        }

        public static LuaRefInfo Create(/*byte cmd, */string name, long addr/*, byte type*/, int size)
        {
            LuaRefInfo r = m_pool.GetObject();
            r.name = name;
            r.addr = addr;
            r.size = size;
            return r;
        }

        public override void Restore()
        {
            m_pool.Store(this);
        }

        public LuaRefInfo Clone()
        {
            LuaRefInfo result = new LuaRefInfo();
            result.name = this.name;
            result.addr = this.addr;
            result.size = this.size;

            return result;
        }

        #endregion
    }

    public class LuaFullMemory : NetBase
    {
        public string fullPath;
        
        public override void Restore()
        {
            
        }
        public LuaFullMemory(string path) 
        {
            fullPath = path;
        }
    }

    public class Sample : NetBase
    {
        public long seq;
        public int currentLuaMemory;
        public int currentMonoMemory;
        public long currentTime;

        public int calls;

        public int costLuaGC;
        public int costMonoGC;
        public string name;
        public int costTime;
        public int internalCostTime;
        public Sample _father;
        public MList<Sample> childs = new MList<Sample>(16);
        public string captureUrl = null;
        private string _fullName;

      //  public int luaGC;
        public bool isCopy = false;
        public long copySelfLuaGC = -1;
        public long selfLuaGC
        {
            get
            {
                if (isCopy) return copySelfLuaGC;
                long result = costLuaGC;
                for (int i = 0, imax = childs.Count; i < imax; i++)
                {
                    var item = childs[i];
                    result -= item.costLuaGC;
                }
                return Math.Max(result, 0);
            }
        }

        private bool m_childrenFilled = false;
        public bool childrenFilled
        {
            get { return m_childrenFilled; }
            set
            {
                if (m_childrenFilled == value) return;
                m_childrenFilled = value;
                for (int i = 0; i < childs.Count; i++)
                {
                    childs[i].childrenFilled = value;
                }
            }
        }

        public long copySelfMonoGC = -1;

        public long selfMonoGC
        {
            get
            {
                if (copySelfMonoGC != -1) return copySelfMonoGC;
                long result = costMonoGC;
                for (int i = 0, imax = childs.Count; i < imax; i++)
                {
                    var item = childs[i];
                    result -= item.costMonoGC;
                }

                return Math.Max(result, 0);
            }
        }

        public bool CheckSampleValid()
        {
            bool result = false;
            do
            {
                if (costLuaGC != 0)
                {
                    result = true;
                    break;
                }

                if (costMonoGC != 0)
                {
                    result = true;
                    break;
                }

                if (costTime > 100000)
                {
                    result = true;
                    break;
                }

            } while (false);


            return result;
        }

        #region property
        public string fullName
        {
            get
            {
                if (_father == null) return name;

                if (_fullName == null)
                {
                    Dictionary<object, string> childDict;
                    if (!m_fullNamePool.TryGetValue(_father.fullName, out childDict))
                    {
                        childDict = new Dictionary<object, string>();
                        m_fullNamePool.Add(_father.fullName, childDict);
                    }

                    if (!childDict.TryGetValue(name, out _fullName))
                    {
                        string value = name;
                        var f = _father;
                        while (f != null)
                        {
                            value = f.name + value;
                            f = f.fahter;
                        }
                        _fullName = value;
                        childDict[name] = string.Intern(_fullName);
                    }

                    return _fullName;
                }
                else
                {
                    return _fullName;
                }
            }
        }

        public Sample fahter
        {
            set
            {
                if (value != null)
                {
                    bool needAdd = true;
                    var childList = value.childs;
                    for (int i = 0, imax = childList.Count; i < imax; i++)
                    {
                        var item = childList[i];
                        if ((object)(item.name) == (object)(name))
                        {
                            needAdd = false;
                            item.AddSample(this);
                            break;
                        }
                    }
                    if (needAdd)
                    {
                        childList.Add(this);
                        _father = value;
                    }
                }
                else
                {
                    _father = null;
                }
            }
            get
            {
                return _father;
            }
        }
        #endregion

        #region pool
        private static Dictionary<object, Dictionary<object, string>> m_fullNamePool = new Dictionary<object, Dictionary<object, string>>();
        private static ObjectPool<Sample> samplePool = new ObjectPool<Sample>(4096);

        public static long ID_SEED = 100;
        private static long id_crease = ID_SEED;
        public static Sample Create()
        {
            Sample s = samplePool.GetObject();
            s.seq = ++id_crease;
            return s;
        }

        public static Sample Create(long time, int memory, string name)
        {
            Sample s = samplePool.GetObject();
            lock (s)
            {
                s.seq = ++id_crease;
                s.calls = 1;
                s.currentTime = time;
                s.currentLuaMemory = memory;
                s.currentMonoMemory = (int)GC.GetTotalMemory(false);
                s.costLuaGC = 0;
                s.costMonoGC = 0;
                s.name = name;
                s.costTime = 0;
                s.internalCostTime = 0;
                s._father = null;
                s.childs.Clear();
                s.captureUrl = null;
                s._fullName = null;
            }

            return s;
        }

        public override void Restore()
        {
            lock (this)
            {
                for (int i = 0, imax = childs.Count; i < imax; i++)
                {
                    childs[i].Restore();
                }
                _fullName = null;
                seq = 0;
                childs.Clear();
                samplePool.Store(this);
            }
        }
        #endregion

        #region method
        public void AddSample(Sample s)
        {
            calls += s.calls;
            costLuaGC += s.costLuaGC;
         //   luaGC += s.costLuaGC;   // obsolete
            costMonoGC += s.costMonoGC;
            costTime += s.costTime;
            internalCostTime += s.internalCostTime;
            for (int i = s.childs.Count - 1; i >= 0; i--)
            {
                var item = s.childs[i];
                item.fahter = this;
                if (item.fahter != s)
                {
                    s.childs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 浅拷贝，不拷贝children
        /// </summary>
        /// <returns></returns>
        public Sample Clone()
        {
            Sample s = new Sample();
            s.seq = seq;
            s.calls = calls;
            s.costMonoGC = costMonoGC;
            s.costLuaGC = costLuaGC;
            s.name = name;
            s.costTime = costTime;
            s.internalCostTime = internalCostTime;

            s.currentLuaMemory = currentLuaMemory;
            s.currentMonoMemory = currentMonoMemory;
            s.currentTime = currentTime;
            s.captureUrl = captureUrl;
            return s;
        }
        /// <summary>
        /// 排除hook对性能的干扰
        /// </summary>
        /// <returns></returns>
        public int Refix()
        {
            int cost = internalCostTime;
            for (int i = 0; i < childs.Count; i++)
            {
                Sample child = childs[i];
                cost += child.Refix();
            }
            costTime -= cost;
            return cost;
        }

        #endregion


        public static void DeleteFiles(string str)
        {
            DirectoryInfo fatherFolder = new DirectoryInfo(str);
            //删除当前文件夹内文件
            FileInfo[] files = fatherFolder.GetFiles();
            foreach (FileInfo file in files)
            {
                string fileName = file.Name;
                try
                {
                    File.Delete(file.FullName);
                }
                catch //(Exception ex)
                {
                    //Debug.LogError(ex);
                }
            }
            //递归删除子文件夹内文件
            foreach (DirectoryInfo childFolder in fatherFolder.GetDirectories())
            {
                DeleteFiles(childFolder.FullName);
            }
            Directory.Delete(str, true);
        }

    }

}

