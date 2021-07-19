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
namespace SparrowLuaProfiler
{
    public static class LuaProfiler
    {
        #region member
        private static IntPtr _mainL = IntPtr.Zero;
        private static readonly Stack<Sample> beginSampleMemoryStack = new Stack<Sample>();
        public static int mainThreadId = -100;
        const long MaxB = 1024;
        const long MaxK = MaxB * 1024;
        const long MaxM = MaxK * 1024;
        const long MaxG = MaxM * 1024;

        private static Action<Sample> m_onReceiveSample;
        private static Action<LuaRefInfo> m_onReceiveRef;
        private static Action<LuaDiffInfo> m_onReceiveDiff;
        public static void RegisterOnReceiveSample(Action<Sample> onReceive)
        {
            m_onReceiveSample = onReceive;
        }
        public static void RegisterOnReceiveRefInfo(Action<LuaRefInfo> onReceive)
        {
            m_onReceiveRef = onReceive;
        }
        public static void RegisterOnReceiveDiffInfo(Action<LuaDiffInfo> onReceive)
        {
            m_onReceiveDiff = onReceive;
        }

        public static void UnRegistReceive()
        {
            m_onReceiveSample = null;
            m_onReceiveRef = null;
            m_onReceiveDiff = null;
        }
        #endregion

        #region property
        public static bool m_hasL = false;
        public static IntPtr mainL
        {
            get
            {
                return _mainL;
            }
            set
            {
                if (value != IntPtr.Zero)
                {
                    m_hasL = true;
                    // unlua has call this function.,so we don't need to do this.
                    //LuaDLL.luaL_initlibs(value);
                    mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                }
                else
                {
                    m_hasL = false;
                }
                _mainL = value;
            }
        }
        public static bool IsMainThread
        {
            get
            {
                return System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;
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
                Utl.Log(string.Format("sample[%s] restore.", sample.name));
                sample.Restore();
            }
        }

        #endregion

    }
}

