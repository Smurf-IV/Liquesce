namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// THMB (Thumbnail)
      /// Starts a file download where the server sends a thumbnail image of a remote file in the specified format.
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=THMB
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void THMB_Command(string cmdArguments)
      {
         // TODO: Need more info on the THMB formatting !
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void THMB_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" THMB");
      }
   }
   /*

//http://activedeveloperdk.codeplex.com/
//The MIT License (MIT)
using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Configuration;

namespace ActiveDeveloper.Core.Utilities
{
  public sealed class GDI
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="imagePathAndName"></param>
    /// <param name="newHeight"></param>
    /// <param name="newWidth"></param>
    /// <returns>Name of the created thumbnail. E.g: small_thumb.jpg</returns>
    public static string CreateThumbnail(string imagePathAndName, int newHeight, int newWidth )
    {
      using( Bitmap bitmap = new Bitmap( imagePathAndName ) ) {
        Image thumbnail = bitmap.GetThumbnailImage( newWidth, newHeight, null, new IntPtr() );

        FileInfo fileInfo = new FileInfo( imagePathAndName );
        string thumbnailName = ConfigurationManager.AppSettings[ "ThumbnailAbr" ] + fileInfo.Name;
        thumbnail.Save( fileInfo.Directory.ToString() + Path.DirectorySeparatorChar + thumbnailName );

        return thumbnailName;
      }
    }
  }
}
    * */
}