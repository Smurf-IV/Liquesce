using System.IO;
using System.Windows.Forms;

namespace Liquesce
{
   public partial class MainForm : Form
   {
      public MainForm()
      {
         InitializeComponent();
      }

      private void MainForm_Shown( object sender, System.EventArgs e )
      {

      }


      /*
       * A recursive method to populate a TreeView
       * Author: Danny Battison
       * Contact: gabehabe@googlemail.com
       */

      /// <summary>
      /// A method to populate a TreeView with directories, subdirectories, etc
      /// </summary>
      /// <param name="dir">The path of the directory</param>
      /// <param name="node">The "master" node, to populate</param>
      public void PopulateTree( string dir, TreeNode node )
      {
         // get the information of the directory
         DirectoryInfo directory = new DirectoryInfo( dir );
         // loop through each subdirectory
         foreach (DirectoryInfo d in directory.GetDirectories())
         {
            // create a new node
            TreeNode t = new TreeNode( d.Name );
            // populate the new node recursively
            PopulateTree( d.FullName, t );
            node.Nodes.Add( t ); // add the node to the "master" node
         }
         // lastly, loop through each file in the directory, and add these as nodes
         foreach (FileInfo f in directory.GetFiles())
         {
            // create a new node
            TreeNode t = new TreeNode( f.Name );
            // add it to the "master"
            node.Nodes.Add( t );
         }
      }
   }
}
