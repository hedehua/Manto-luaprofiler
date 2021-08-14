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
* Filename: LuaProfiler
* Created:  2018/7/13 14:29:22
* Author:   エル・プサイ・コングリィ
* Purpose:  
* ==============================================================================
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace SparrowLuaProfiler
{
    public static class LuaProfiler
    {
        #region member

        private static readonly Stack<Sample> beginSampleMemoryStack = new Stack<Sample>();
        public static int m_mainThreadId = -100;
        const long MaxB = 1024;
        const long MaxK = MaxB * 1024;
        const long MaxM = MaxK * 1024;
        const long MaxG = MaxM * 1024;

        #endregion

        #region property
        private static IntPtr m_mainL = IntPtr.Zero;

        public static bool memory_hooked = false;
        private static Dictionary<long, LuaRefInfo> luaRefInfoMap = new Dictionary<long, LuaRefInfo>();

        public static void BeginPlay()
        {
            NetWorkClient.RegisterOnReceiveCmd(OnReceiveCmd);
            SendSysInfo();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer">若当前为alloc操作，buffer为新分配的地址，若当前为free,则为空</param>
        /// <param name="ptr">为旧的地址</param>
        /// <param name="osize"></param>
        /// <param name="nsize"></param>
        /// 

        public static void PreLuaMemoryAlloc(IntPtr ptr, long osize, long nsize) 
        {
        }

        public static void PostLuaMemoryAlloc( IntPtr ptr, IntPtr buffer, long osize, long nsize)
        {
       //     Utl.Log(string.Format("ptr:{0} buffer:{1} osize:{2} nsize:{3}", ptr, buffer, osize, nsize));
            if (!memory_hooked) return;
            if (m_mainL == IntPtr.Zero) return;
            if (osize == 0 && nsize == 0) return;
            lock (luaRefInfoMap)
            {
                try
                {
                    // free object,remove ptr from cache
                    LuaRefInfo refInfo;
                    bool contains = luaRefInfoMap.TryGetValue(ptr.ToInt64(), out refInfo);
                    if (contains)
                    {
                        luaRefInfoMap.Remove(ptr.ToInt64());
                        refInfo.Restore();
                    }
                    if (nsize == 0)
                    {
                        return;
                    }

                    // get call stack
                    refInfo = LuaRefInfo.Create();
                    refInfo.size = (int)nsize;
                    refInfo.addr = buffer.ToInt64();
                    lua_Debug debug = new lua_Debug();
                    IntPtr ar = Marshal.AllocHGlobal(Marshal.SizeOf(debug));
                    Marshal.StructureToPtr(debug, ar, false);
                    if (LuaDLL.lua_getstack(m_mainL, 0, ar) > 0 && LuaDLL.lua_getinfo(m_mainL, "nS", ar) > 0)
                    {
                        refInfo.name = debug.ToString();
                    }
                    else
                    {
                        refInfo.name = "unknown";
                    }
                    Marshal.FreeHGlobal(ar);
                    luaRefInfoMap.Add(refInfo.addr, refInfo);

                }
                catch (Exception e)
                {
                    Utl.Log(string.Format("Alloc Exception: {0}", e.ToString()));
                }
            }
        }

        public static void OnLuaStateCreated(IntPtr luastate)
        {
            if (luastate != IntPtr.Zero)
            {
                m_mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            }
            m_mainL = luastate;
            SendSysInfo();
        }

        private static void OnReceiveCmd(int arg)
        {
            switch (arg)
            {
                case 0:

                    {
                        if (m_mainL != IntPtr.Zero)
                        {
                            SetHook(m_mainL, false);
                        }

                    }
                    break;
                case 1:
                    {
                        if (m_mainL != IntPtr.Zero)
                        {
                            SetHook(m_mainL, true);
                        }
                    }
                    break;
                case 2:
                    {
                        memory_hooked = false;
                    }
                    break;
                case 3:
                    {
                        memory_hooked = true;
                    }
                    break;
                case 4:
                    {
                        if (m_mainL == IntPtr.Zero) 
                        {
                            Utl.Log("capture memory failure, lua state has not beed created.");
                            return;
                        }
                        SendMemoryReport();
                    }
                    break;
                default: break;
            }

            SendSysInfo();
        }
        public static void OnLuaStateClosed(IntPtr luaState)
        {
            if (luaState != m_mainL)
            {
                Utl.Log("luastate Closed ,but it's not main state");
                return;
            }
            hook_func = null;
            m_mainL = IntPtr.Zero;
            m_mainThreadId = 0;
            SendSysInfo();
        }

        public static void SendMemoryReport() 
        {

            // 1. write to local file
            string fullPath = string.Format("{0}/memory-{1}.luam", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss"));
            FileStream fs = new FileStream(fullPath, FileMode.Create);
            MBinaryWriter bw = new MBinaryWriter(fs);
            lock (luaRefInfoMap)
            {
                bw.Write(luaRefInfoMap.Count);      // header <int> count;
                foreach (var item in luaRefInfoMap)
                {
                    NetUtil.Serialize(item.Value, bw);
                }
            }
            fs.Close();

            // 2. send to remote
            LuaFullMemory luaFullMemory = new LuaFullMemory(fullPath);
            NetWorkClient.SendMessage(luaFullMemory);
            Utl.Log(string.Format("SendMemoryReport size: {0} {1}", luaRefInfoMap.Count, fullPath));
        }

        public static void SendSysInfo()
        {
            Utl.Log(string.Format("SendSysInfo: luastate:{0} hook:{1} memory:{2}", IsMainLCreated, CheckHook(), memory_hooked));
            NetWorkClient.SendMessage(new SysInfo(IsMainLCreated, CheckHook(), memory_hooked));
        }

        private static LuaDLL.lua_Hook_fun hook_func = null;
        public static bool CheckHook()
        {
            if (m_mainL == IntPtr.Zero) return false;
            if (hook_func == null) return false;
            return LuaDLL.lua_gethook(m_mainL) == hook_func;
        }
        public static void SetHook(IntPtr luaState, bool enable)
        {
            if (enable)
            {
                hook_func = new LuaDLL.lua_Hook_fun(LuaHook);
                int LuaDebugMask = LuaDLL.LUA_MASKCALL | LuaDLL.LUA_MASKRET;
                LuaDLL.lua_sethook(luaState, hook_func, LuaDebugMask, 0);
            }
            else
            {
                hook_func = null;
                LuaDLL.lua_sethook(luaState, null, 0, 0);
                beginSampleMemoryStack.Clear();
            }
        }
        private static void LuaHook(IntPtr luaState, IntPtr ar)
        {
            try
            {
                long currentTime = LuaProfiler.getcurrentTime;

                Debug.Assert(LuaDLL.lua_getinfo(luaState, "nS", ar) > 0);

                lua_Debug debug = (lua_Debug)Marshal.PtrToStructure(ar, typeof(lua_Debug));
                switch (debug.evt)
                {
                    case LuaDLL.LUA_HOOKCALL:
                        BeginSample(luaState, debug.ToString(), currentTime);
                        break;
                    case LuaDLL.LUA_HOOKRET:
                        EndSample(luaState, currentTime);
                        break;
                    default: break;
                }

            }
            catch (Exception e)
            {
                Utl.Log(e.ToString());
            }

        }

        public static bool IsMainLCreated
        {
            get
            {
                return m_mainL != IntPtr.Zero;
            }
        }
        public static bool IsMainThread
        {
            get
            {
                return System.Threading.Thread.CurrentThread.ManagedThreadId == m_mainThreadId;
            }
        }
        #endregion

        #region sample

        public static long getcurrentTime
        {
            get
            {
                return System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }

        public static void BeginSample(IntPtr luaState, string name, long currentTime)
        {
            if (!IsMainThread)
            {
                return;
            }
            try
            {
                long memoryCount = LuaDLL.GetLuaMemory(luaState);
                Sample sample = Sample.Create(0, (int)memoryCount, name);
                beginSampleMemoryStack.Push(sample);
                sample.currentTime = currentTime;
                sample.internalCostTime = (int)(getcurrentTime - currentTime);
            }
            catch
            {
            }
        }

        public static void EndSample(IntPtr luaState, long currentTime)
        {
            if (!IsMainThread)
            {
                return;
            }

            if (beginSampleMemoryStack.Count <= 0)
            {
                return;
            }
            long nowMemoryCount = LuaDLL.GetLuaMemory(luaState);
            long nowMonoCount = GC.GetTotalMemory(false);
            Sample sample = beginSampleMemoryStack.Pop();

            var monoGC = nowMonoCount - sample.currentMonoMemory;
            var luaGC = nowMemoryCount - sample.currentLuaMemory;
            sample.currentLuaMemory = (int)nowMemoryCount;
            sample.currentMonoMemory = (int)nowMonoCount;
            sample.costLuaGC = (int)luaGC;
            sample.costMonoGC = (int)monoGC;

            if (sample.childs.Count > 0)
            {
                long mono_gc = 0;
                long lua_gc = 0;
                for (int i = 0, imax = sample.childs.Count; i < imax; i++)
                {
                    Sample c = sample.childs[i];
                    lua_gc += c.costLuaGC;
                    mono_gc += c.costMonoGC;
                }
                sample.costLuaGC = (int)Math.Max(lua_gc, luaGC);
                sample.costMonoGC = (int)Math.Max(mono_gc, monoGC);
            }

            if (!sample.CheckSampleValid())
            {
                sample.Restore();
                return;
            }
            sample.fahter = beginSampleMemoryStack.Count > 0 ? beginSampleMemoryStack.Peek() : null;

            bool stackExhausted = beginSampleMemoryStack.Count == 0;
            if (stackExhausted)
            {
                sample.Refix();
            }
            // 该尽可能靠后，以统计更多的内部消耗
            sample.costTime = (int)(getcurrentTime - sample.currentTime);
            sample.internalCostTime += (int)(getcurrentTime - currentTime);

            if (stackExhausted)
            {
                NetWorkClient.AddSample(sample);
            }
            //释放掉被累加的Sample
            if (beginSampleMemoryStack.Count != 0 && sample.fahter == null)
            {
                Utl.Log(string.Format("sample[{0}] restore.", sample.name));
                sample.Restore();
            }
        }

        #endregion

    }
}

