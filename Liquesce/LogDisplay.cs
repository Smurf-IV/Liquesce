using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using NLog;

namespace Liquesce
{
   /// <summary>
   /// 
   /// </summary>
   static public class DisplayLog
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// 
      /// </summary>
      static public void LogDisplay(string logLocation)
      {
         try
         {
            OpenFileDialog openFileDialog = new OpenFileDialog
                                               {
                                                  InitialDirectory =
                                                     Path.Combine(
                                                        Environment.GetFolderPath(
                                                           Environment.SpecialFolder.CommonApplicationData), logLocation),
                                                  Filter = "Log files (*.log)|*.log|Archive logs (*.*)|*.*",
                                                  FileName = "*.log",
                                                  FilterIndex = 2,
                                                  Title = "Select name to view contents"
                                               };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
               Process.Start("Wordpad.exe", '"' + openFileDialog.FileName + '"');
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenFile has an exception: ", ex);
         }
      }

   }
}