using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Messaging;
using System.Runtime.InteropServices;


namespace LiquesceMirrorTray
{
    class MirrorWorker
    {
        private static Thread thread = new Thread(new ThreadStart(endlesswork));

        private static void endlesswork()
        {
            
            //while (true)
            {
                SendNotifyMessage((IntPtr)0xffff, RegisterWindowMessage("LMirrorFile"),
                    Marshal.StringToHGlobalAuto("worker did something"), Marshal.StringToHGlobalAuto("another message"));

            }
        }

        public static void Start()
        {
            thread.Start();
        }

        public static void Stop()
        {
            thread.Abort();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendNotifyMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int RegisterWindowMessage(string lpString); 

    }
}
