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
* Filename: NetWorkClient
* Created:  2018/7/13 14:29:22
* Author:   エル・プサイ・コングリィ
* Purpose:  
* ==============================================================================
*/

using System;
using EasyHook;
using System.Threading;
using System.Windows.Forms;

namespace SparrowLuaProfiler
{
    [Serializable]
    public class HookParameter
    {
        public string Msg { get; set; }
        public int HostProcessId { get; set; }
    }

    public class Main : IEntryPoint
    {
        #region field
        public LocalHook MessageBoxWHook = null;
        public LocalHook MessageBoxAHook = null;
        public static int frameCount { private set; get; }
        #endregion

        public void Uninstall()
        {
            Utl.Log("uninstall");
            NativeAPI.LhUninstallAllHooks();
        }

        public Main(
            RemoteHooking.IContext context,
            string channelName
            , HookParameter parameter
            )
        {
        }

        public void Run( 
            RemoteHooking.IContext context,
            string channelName
            , HookParameter parameter
            )
        {
            try
            {
                Utl.OnApplicationLaunch();
                LuaDLL.Uninstall();
                LuaDLL.HookLoadLibrary();
                LuaDLL.BindEasyHook();
            }
            catch (Exception ex)
            {
                Utl.Log(ex.Message);
                return;
            }

            NetWorkClient.ConnectServer("127.0.0.1", 2333);
            LuaProfiler.BeginPlay();

        }

    }
}
