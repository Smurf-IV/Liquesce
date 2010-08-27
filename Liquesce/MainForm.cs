using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using LiquesceFaçade;
using NLog;

namespace Liquesce
{
   public sealed partial class MainForm : Form
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private ConfigDetails cd = new ConfigDetails();

      public MainForm()
      {
         // Force use of Segou UI Font in Vista and above
         if (SystemFonts.MessageBoxFont.Size >= 9)
            Font = SystemFonts.MessageBoxFont;
         InitializeComponent();
         Icon icon = ExtractIcon.GetIconForFilename(Environment.GetFolderPath(Environment.SpecialFolder.MyComputer), true);
         imageListUnits.Images.Add("MyComputer", icon.ToBitmap());
      }

      private void MainForm_Shown(object sender, EventArgs e)
      {
         StartTree();
         PopulatePoolSettings();
         if (serviceController1.Status != ServiceControllerStatus.Running)
         {
            commitToolStripMenuItem.Text = "Service is not running";
            commitToolStripMenuItem.Enabled = false;
         }
         else
         {
            try
            {
               ChannelFactory<ILiquesce> factory = new ChannelFactory<ILiquesce>("LiquesceFaçade");
               ILiquesce remoteIF = factory.CreateChannel();
               cd = remoteIF.ConfigDetails;
            }
            catch (Exception ex)
            {
               Log.ErrorException("Unable to attach to the service, even tho it is running", ex);
               MessageBox.Show(this, ex.Message, "Has the firewall blocked the communications ?", MessageBoxButtons.OK,
                               MessageBoxIcon.Stop);
            }
         }
         InitialiseWith();
      }

      private void InitialiseWith()
      {
         if (!String.IsNullOrWhiteSpace(cd.DriveLetter))
         {
            // Add the drive letter back in as this would already have been removed
            MountPoint.Items.Add(cd.DriveLetter);
            MountPoint.Text = cd.DriveLetter;
            if (cd.SourceLocations != null)
               foreach (TreeNode tn in cd.SourceLocations.Select(sourceLocation => new TreeNode
                                                                                      {
                                                                                         Text = sourceLocation,
                                                                                         ImageKey = sourceLocation,
                                                                                         SelectedImageKey =
                                                                                            sourceLocation
                                                                                      }))
               {
                  mergeList.Nodes.Add(tn);
               }
            DelayCreation.Text = cd.DelayStartMilliSec.ToString();
            VolumeLabel.Text = cd.VolumeLabel;
            RestartExpectedOutput();
         }
      }

      private void PopulatePoolSettings()
      {
         string[] drives = Environment.GetLogicalDrives();
         foreach (string dr in drives)
         {
            MountPoint.Items.Remove(dr.Remove(1));
         }
         MountPoint.Text = "N";
      }

      #region Methods to populate and drill down in the tree
      // Code stolen from the http://frwingui.codeplex.com project
      private void StartTree()
      {
         TreeNode tvwRoot;
         // Code taken and adapted from http://msdn.microsoft.com/en-us/library/bb513869.aspx
         try
         {
            Enabled = false;
            UseWaitCursor = true;
            driveAndDirTreeView.Nodes.Clear();

            Log.Debug("Create the root node.");
            tvwRoot = new TreeNode
                         {
                            Text = Environment.MachineName,
                            ImageKey = "MyComputer",
                            SelectedImageKey = "MyComputer"
                         };
            driveAndDirTreeView.Nodes.Add(tvwRoot);
            Log.Debug("Now we need to add any children to the root node.");

            Log.Debug("Start with drives if you have to search the entire computer.");
            string[] drives = Environment.GetLogicalDrives();
            foreach (string dr in drives)
            {
               Log.Debug(dr);
               DriveInfo di = new DriveInfo(dr);

               FillInDirectoryType(tvwRoot, di);
            }

            tvwRoot.Expand();
         }
         catch (Exception ex)
         {
            Log.ErrorException("StartTree Threw: ", ex);
         }
         finally
         {
            Enabled = true;
            UseWaitCursor = false;
         }
      }

      private void FillInDirectoryType(TreeNode parentNode, DriveInfo di)
      {
         if ( di != null)
         {
            if( ( (di.DriveType == DriveType.Fixed)
                  || (di.DriveType == DriveType.Network)
                  )
               && (di.DriveFormat == "DOKAN")
               )
            {
               Log.Info("Removing the existing DOKAN drive as this would cause confusion ! [{0}]", di.Name);
               return;
            }
            SafelyAddIcon(di.Name);
            string label;
            try
            {
               label = !String.IsNullOrWhiteSpace(di.VolumeLabel) ? di.VolumeLabel : di.DriveType.ToString();
            }
            catch
            {
               // The above throws a wobble e.g. if the CD_Rom does not have a disk in it
               label = di.DriveType.ToString();
            } 
            label += " (" + di.Name + ")";
            TreeNode thisNode = new TreeNode
                                   {
                                      Text = label,
                                      ImageKey = di.Name,
                                      SelectedImageKey = di.Name,
                                      Tag = di.RootDirectory
                                   };
            if (di.IsReady)
               thisNode.Nodes.Add("DummyNode");
            parentNode.Nodes.Add(thisNode);
         }
      }

      private void WalkNextTreeLevel(TreeNode parentNode)
      {
         try
         {
            DirectoryInfo root = parentNode.Tag as DirectoryInfo;
            if (root != null)
            {
               Log.Debug("// Find all the subdirectories under this directory.");
               DirectoryInfo[] subDirs = root.GetDirectories();
               if (subDirs != null)
               {
                  foreach (DirectoryInfo dirInfo in subDirs)
                  {
                     // Recursive call for each subdirectory.
                     SafelyAddIcon(dirInfo.FullName);
                     TreeNode tvwChild = new TreeNode
                                            {
                                               Text = dirInfo.Name,
                                               ImageKey = dirInfo.FullName,
                                               SelectedImageKey = dirInfo.FullName,
                                               Tag = dirInfo
                                            };

                     Log.Debug("If this is a folder item and has children then add a place holder node.");
                     try
                     {
                        DirectoryInfo[] subChildDirs = dirInfo.GetDirectories();
                        if ((subChildDirs != null)
                            && (subChildDirs.Length > 0)
                           )
                           tvwChild.Nodes.Add("DummyNode");
                     }
                     catch (UnauthorizedAccessException uaex)
                     {
                        Log.InfoException("No Access to subdirs in " + tvwChild.Text, uaex);
                     }
                     parentNode.Nodes.Add(tvwChild);
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("RecurseAddChildren has thrown:", ex);
         }
      }

      private void driveAndDirTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
      {
         Enabled = false;
         UseWaitCursor = true;
         try
         {
            Log.Debug("Remove the placeholder node.");
            DirectoryInfo root = e.Node.Tag as DirectoryInfo;
            if (root != null)
            {
               e.Node.Nodes.Clear();
               WalkNextTreeLevel(e.Node);
            }
            e.Cancel = false;
         }
         catch (Exception ex)
         {
            Log.ErrorException("BeforeExpand has thrown: ", ex);
         }
         finally
         {
            Enabled = true;
            UseWaitCursor = false;
         }
      }
      #endregion

      #region DragAndDrop over to the merge list
      private void driveAndDirTreeView_MouseDown(object sender, MouseEventArgs e)
      {
         if (e.Button == MouseButtons.Left)
         {
            // Get the node underneath the mouse.
            TreeNode selected = driveAndDirTreeView.GetNodeAt(e.X, e.Y);
            driveAndDirTreeView.SelectedNode = selected;

            // Start the drag-and-drop operation with a cloned copy of the node.
            if (selected != null)
            {
               DragDropItem ud = new DragDropItem(GetSelectedNodesPath(selected));
               if (!String.IsNullOrEmpty(ud.Name))
                  driveAndDirTreeView.DoDragDrop(ud, DragDropEffects.All);
            }
         }
      }

      private void mergeList_DragOver(object sender, DragEventArgs e)
      {
         // Drag and drop denied by default.
         e.Effect = DragDropEffects.None;

         // Is it a valid format?
         DragDropItem ud = e.Data.GetData(typeof(DragDropItem)) as DragDropItem;
         if (ud != null)
         {
            e.Effect = DragDropEffects.Copy;
         }
      }

      private void mergeList_DragDrop(object sender, DragEventArgs e)
      {
         // Is it a valid format?
         DragDropItem ud = e.Data.GetData(typeof(DragDropItem)) as DragDropItem;
         if (ud != null)
         {
            CheckAndAddNewPath(mergeList, ud);
         }
      }
      private string GetSelectedNodesPath(TreeNode selected)
      {
         DirectoryInfo shNode = selected.Tag as DirectoryInfo;
         Log.Debug("Now we need to add any children to the root node.");
         string newPath = shNode != null ? shNode.FullName : selected.FullPath;
         return newPath;
      }

      private void CheckAndAddNewPath(TreeView targetTree, DragDropItem newPath)
      {
         // TODO: On Add check to make sure that the root (Or this) node have not already been covered.
         if (!String.IsNullOrEmpty(newPath.Name))
         {
            TreeNode tn = new TreeNode
                             {
                                Text = newPath.Name,
                                ImageKey = newPath.Name,
                                SelectedImageKey = newPath.Name
                             };
            targetTree.Nodes.Add(tn);
            RestartExpectedOutput();
         }
      }

      #endregion

      private void mergeList_KeyUp(object sender, KeyEventArgs e)
      {
         if (e.KeyCode != Keys.Delete)
            return;
         // Get the node underneath the mouse.
         TreeNode selected = mergeList.SelectedNode;

         if (selected != null)
         {
            mergeList.SelectedNode = null;
            mergeList.Nodes.Remove(selected);
            e.Handled = true;
            RestartExpectedOutput();
         }
      }

      private void RestartExpectedOutput()
      {
         FillExpectedLayoutWorker.CancelAsync();
         while (FillExpectedLayoutWorker.IsBusy)
         {
            Thread.Sleep(500);
            Application.DoEvents();
         }
         ConfigDetails cd = new ConfigDetails
                               {
                                  DelayStartMilliSec = (uint)DelayCreation.Value,
                                  DriveLetter = MountPoint.Text,
                                  VolumeLabel = VolumeLabel.Text,
                                  SourceLocations = new List<string>(mergeList.Nodes.Count)
                               };
         if (mergeList.Nodes != null)
            foreach (TreeNode node in mergeList.Nodes)
            {
               cd.SourceLocations.Add(node.Text);
            }
         FillExpectedLayoutWorker.RunWorkerAsync(cd);
      }

      private void FillExpectedLayoutWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
      {
         SetProgressBarStyle(ProgressBarStyle.Continuous);
      }

      private void FillExpectedLayoutWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
      {
         SetProgressBarStyle(ProgressBarStyle.Marquee);
         ClearExpectedList();
         ConfigDetails cd = e.Argument as ConfigDetails;
         BackgroundWorker worker = sender as BackgroundWorker;
         if ((cd == null)
            || (worker == null)
            )
         {
            Log.Error("Worker, or auguments are null, exiting.");
            return;
         }
         TreeNode root = new TreeNode(cd.VolumeLabel + " (" + cd.DriveLetter + ")");
         AddExpectedNode(null, root);
         if (worker.CancellationPending)
            return;
         WalkExpectedNextTreeLevel(root, cd.SourceLocations);
         if (worker.CancellationPending)
            return;
      }


      private void AddFiles(List<string> sourceLocations, string directoryPath, List<ExpectedDetailResult> allFiles)
      {
         try
         {
            DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
            if (dirInfo.Exists)
            {
               FileSystemInfo[] fileSystemInfos = dirInfo.GetFileSystemInfos();
               allFiles.AddRange(fileSystemInfos.Select(info2 => new ExpectedDetailResult
                                                                    {
                                                                       DisplayName = TrimAndAdd(sourceLocations, info2.FullName),
                                                                       ActualFileLocation = info2.FullName
                                                                    }));
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("AddFiles threw: ", ex);
         }
      }

      private string TrimAndAdd(List<string> sourceLocations, string fullFilePath)
      {
         int index = sourceLocations.FindIndex(fullFilePath.StartsWith);
         if (index >= 0)
         {
            string key = fullFilePath.Remove(0, sourceLocations[index].Length);
            return key;
         }
         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
      }


      private delegate void AddExpecteddNodeCallBack(TreeNode parent, TreeNode child);
      private void AddExpectedNode(TreeNode parent, TreeNode child)
      {
         if (expectedTreeView.InvokeRequired)
         {
            AddExpecteddNodeCallBack d = AddExpectedNode;
            Invoke(d, new object[] { parent, child });
         }
         else
         {
            if (parent == null)
               expectedTreeView.Nodes.Add(child);
            else
            {
               parent.Nodes.Add(child);
            }
         }
      }

      delegate void SetProgressBarStyleCallback(ProgressBarStyle style);
      private void SetProgressBarStyle(ProgressBarStyle style)
      {
         // InvokeRequired required compares the thread ID of the
         // calling thread to the thread ID of the creating thread.
         // If these threads are different, it returns true.
         if (progressBar1.InvokeRequired)
         {
            SetProgressBarStyleCallback d = SetProgressBarStyle;
            Invoke(d, new object[] { style });
         }
         else
         {
            progressBar1.Style = style;
            expectedTreeView.Enabled = (style != ProgressBarStyle.Marquee);
            UseWaitCursor = !Enabled;
         }
      }

      private delegate void ClearExpectedListCallBack();
      private void ClearExpectedList()
      {
         if (expectedTreeView.InvokeRequired)
         {
            ClearExpectedListCallBack d = ClearExpectedList;
            Invoke(d);
         }
         else
         {
            expectedTreeView.Nodes.Clear();
         }
      }

      private void expectedTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
      {
         try
         {
            SetProgressBarStyle(ProgressBarStyle.Marquee);
            Log.Debug("Remove the placeholder node.");
            string root = e.Node.Tag as string;
            if (!String.IsNullOrEmpty(root))
            {
               e.Node.Nodes.Clear();
               List<string> sourceLocations = new List<string>(mergeList.Nodes.Count);
               sourceLocations.AddRange(from TreeNode node in mergeList.Nodes select node.Text);
               WalkExpectedNextTreeLevel(e.Node, sourceLocations, root);
            }
            e.Cancel = false;
         }
         catch (Exception ex)
         {
            Log.ErrorException("BeforeExpand has thrown: ", ex);
         }
         finally
         {
            SetProgressBarStyle(ProgressBarStyle.Continuous);
         }
      }

      private void WalkExpectedNextTreeLevel(TreeNode parent, List<string> sourceLocations)
      {
         WalkExpectedNextTreeLevel(parent, sourceLocations, String.Empty);
      }

      private void WalkExpectedNextTreeLevel(TreeNode parent, List<string> sourceLocations, string expectedStartLocation)
      {
         List<ExpectedDetailResult> allFiles = new List<ExpectedDetailResult>();
         if (sourceLocations != null)
            sourceLocations.ForEach(str2 => AddFiles(sourceLocations, str2 + expectedStartLocation, allFiles));
         allFiles.Sort();
         Log.Debug("Should now have a huge list of filePaths");
         AddNextExpectedLevel(allFiles, parent);
      }

      private void AddNextExpectedLevel(List<ExpectedDetailResult> allFiles, TreeNode parent)
      {
         if (allFiles != null)
            foreach (ExpectedDetailResult kvp in allFiles)
            {
               if (Directory.Exists(kvp.ActualFileLocation))
               {
                  // This is a Dir, so make a new child
                  string label = kvp.DisplayName;
                  int index = kvp.DisplayName.LastIndexOf(Path.DirectorySeparatorChar);
                  if (index > 0)
                     label = kvp.DisplayName.Substring(index + 1);
                  TreeNode child = new TreeNode
                                      {
                                         Text = label,
                                         Tag = kvp.DisplayName,
                                         ToolTipText = kvp.ActualFileLocation
                                      };
                  child.Nodes.Add("DummyNode");
                  AddExpectedNode(parent, child);
               }
               else
               {
                  AddFileNodeCallBack d = AddFileNode;
                  Invoke(d, new object[] { kvp, parent });
               }
            }
      }

      private delegate void AddFileNodeCallBack(ExpectedDetailResult kvp, TreeNode parent);

      private void AddFileNode(ExpectedDetailResult kvp, TreeNode parent)
      {
         SafelyAddIcon(kvp.ActualFileLocation);
         TreeNode child = new TreeNode
                             {
                                Text = Path.GetFileName(kvp.DisplayName),
                                ImageKey = kvp.ActualFileLocation,
                                ToolTipText = kvp.ActualFileLocation
                             };
         AddExpectedNode(parent, child);
      }

      private void SafelyAddIcon(string fullFileName)
      {
         try
         {
            if (!imageListUnits.Images.ContainsKey(fullFileName))
            {
               imageListUnits.Images.Add(fullFileName, ExtractIcon.GetIconForFilename(fullFileName, true).ToBitmap());
            }
         }
         catch { }
      }

      private void commitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         try
         {
            SetProgressBarStyle(ProgressBarStyle.Marquee);
            cd.DelayStartMilliSec = (uint)DelayCreation.Value;
            cd.DriveLetter = MountPoint.Text;
            cd.VolumeLabel = VolumeLabel.Text;
            cd.SourceLocations = new List<string>(mergeList.Nodes.Count);
            if (mergeList.Nodes != null)
               foreach (TreeNode node in mergeList.Nodes)
               {
                  cd.SourceLocations.Add(node.Text);
               }

            ChannelFactory<ILiquesce> factory = new ChannelFactory<ILiquesce>("LiquesceFaçade");
            ILiquesce remoteIF = factory.CreateChannel();
            remoteIF.ConfigDetails = cd;
            Log.Info("Didn't go bang so stop");
            remoteIF.Stop();
            Log.Info("Now start");
            remoteIF.Start();
         }
         catch (Exception ex)
         {
            Log.ErrorException("Unable to attach to the service, even tho it is running", ex);
            MessageBox.Show(this, ex.Message, "Has the firewall blocked the communications ?", MessageBoxButtons.OK,
                            MessageBoxIcon.Stop);
         }
         finally
         {
            SetProgressBarStyle(ProgressBarStyle.Continuous);
         }
      }

      private void MountPoint_TextChanged(object sender, EventArgs e)
      {
         RestartExpectedOutput();
      }

      private void VolumeLabel_Validated(object sender, EventArgs e)
      {
         RestartExpectedOutput();
      }
   }
}
