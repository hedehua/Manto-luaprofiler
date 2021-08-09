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
using System.Runtime.InteropServices;

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

        public static void OnLuaStateCreated(IntPtr luastate) 
        {
            if (luastate != IntPtr.Zero)
            {
                m_mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            }
            m_mainL = luastate;
            NetWorkClient.RegisterOnReceiveCmd(OnReceiveCmd);
            SendSysInfo();
        }

        private static void OnReceiveCmd(int cmd) 
        {
            if (cmd == 1) 
            {
                SetHook(m_mainL, true);
            }
            if (cmd == 2) 
            {
                SetHook(m_mainL, false);
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
            NetWorkClient.UnRegistReceive();
            SendSysInfo();
        }
        public static void SendSysInfo() 
        {
            Utl.Log(string.Format("send sysinfo: luastate:{0} {1}", IsMainLCreated, CheckHook()));
            NetWorkClient.SendMessage(new SysInfo(IsMainLCreated, CheckHook()));
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

                int ret = LuaDLL.lua_getinfo(luaState, "nS", ar);
                if (ret == 0)
                {
                    Utl.Log("lua_getinfo:" + ret);
                    return;
                }

                lua_Debug debug = (lua_Debug)Marshal.PtrToStructure(ar, typeof(lua_Debug));
                switch (debug.evt)
                {
                    case LuaDLL.LUA_HOOKCALL:
                        string message = string.Format("{0} {1} {2}", debug.name, debug.source, debug.linedefined/*, debug.namewhat*/ );
                        BeginSample(luaState, message, currentTime);
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
            if(stackExhausted)
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

