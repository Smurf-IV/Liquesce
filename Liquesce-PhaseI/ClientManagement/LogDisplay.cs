using System;
using System.IO;
using System.Windows.Forms;
using NLog;

namespace ClientManagement
{
   /// <summary>
   /// 
   /// </summary>
   public partial class LogDisplay : Form
   {
      private readonly string LogLocation;
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// 
      /// </summary>
      public LogDisplay(string logLocation)
      {
         InitializeComponent();
         LogLocation = logLocation;
      }


      private void OpenFile()
      {
         try
         {
            OpenFileDialog openFileDialog = new OpenFileDialog
                                               {
                                                  InitialDirectory =
                                                     Path.Combine(
                                                        Environment.GetFolderPath(
                                                           Environment.SpecialFolder.CommonApplicationData), LogLocation),
                                                  Filter = "Log files (*.log)|*.log|Archive logs (*.*)|*.*",
                                                  FileName = "*.log",
                                                  FilterIndex = 2,
                                                  Title = "Select name to view contents"
                                               };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
               UseWaitCursor = true;
               using (StreamReader reader = new StreamReader(File.Open(openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
               {
                  string line;
                  while (null != (line = reader.ReadLine()))
                  {
                     textBox1.Items.Add(line);
                  }
               }
               int count = textBox1.Items.Count - 1;
               if (count > 1)
                  textBox1.SetSelected(count, true);
            }
            else
            {
               Close();
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenFile has an exception: ", ex);
            textBox1.Items.Add(ex.Message);
         }
         finally
         {
            UseWaitCursor = false;
         }
      }

      private void done_Click(object sender, EventArgs e)
      {
         Close();
      }

      private void LogDisplay_Shown(object sender, EventArgs e)
      {
         OpenFile();
      }

   }
}