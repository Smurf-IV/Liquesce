using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NLog;

namespace LiquesceFacade
{
   public class DealWithTheCfgChanging
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      private void InitialiseToDefault(ref ConfigDetails currentConfigDetails)
      {
         try
         {
            if (currentConfigDetails == null)
            {
               currentConfigDetails = new ConfigDetails();
               currentConfigDetails.InitConfigDetails();
               currentConfigDetails.SourceLocations.Add( new SourceLocation( @"C:\") );
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot create the default configDetails: ", ex);
            currentConfigDetails = null;
         }
      }

      public void ReadConfigDetails(ref ConfigDetails currentConfigDetails)
      {
         try
         {
            InitialiseToDefault(ref currentConfigDetails);
            XmlSerializer x = new XmlSerializer(typeof(ConfigDetails));
            x.UnknownElement += XOnUnknownElement;
            //x.UnknownNode += XOnUnknownNode;
            //x.UnreferencedObject += XUnreferencedObject;
            Log.Info("Attempting to read Drive details from: [{0}]", ConfigDetails.configFile);
            using (XmlTextReader textReader = new XmlTextReader(ConfigDetails.configFile))
            {
               currentConfigDetails = (ConfigDetails)x.Deserialize(textReader);
            }
            RequiresSourceLocationsFixup(currentConfigDetails);
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot read the configDetails: ", ex);
            currentConfigDetails = null;
         }
         finally
         {
            if (currentConfigDetails == null)
            {
               InitialiseToDefault(ref currentConfigDetails);
               if (!File.Exists(ConfigDetails.configFile))
                  ConfigDetails.WriteOutConfigDetails(currentConfigDetails);
            }
         }
      }

      private void XUnreferencedObject(object sender, UnreferencedObjectEventArgs e)
      {
      }

      private void XOnUnknownNode(object sender, XmlNodeEventArgs xmlNodeEventArgs)
      {
      }

      List<string> OldSourceLocation = new List<string>();

      private void RequiresSourceLocationsFixup(ConfigDetails newCfg)
      {
         foreach (string s in OldSourceLocation)
         {
            newCfg.SourceLocations.Add(new SourceLocation(s));
         }
      }

      private void XOnUnknownElement(object sender, XmlElementEventArgs xeeArgs)
      {
         if (xeeArgs.ExpectedElements == ":SourceLocation")
         {
            ConfigDetails target = xeeArgs.ObjectBeingDeserialized as ConfigDetails;
            if (target == null)
            {
               // for some reason the target is not always set !
               OldSourceLocation.Add(xeeArgs.Element.InnerText);
            }
            else
               target.SourceLocations.Add( new SourceLocation(xeeArgs.Element.InnerText) );
         }
      }

      private static bool OkToAddThisDriveType(string dr)
      {
         bool seemsOK;
         try
         {
            Log.Debug(dr);
            DriveInfo di = new DriveInfo(dr);
            DriveType driveType = di.DriveType;
            switch (driveType)
            {
               case DriveType.Removable:
               case DriveType.Fixed:
                  {
                     string di_DriveFormat = di.DriveFormat;
                     switch (di_DriveFormat.ToUpper())
                     {
                        case "CBFS":
                           Log.Warn("Removing the existing CBFS drive as this would cause confusion ! [{0}]",
                                    di.Name);
                           seemsOK = false;
                           break;
                        case "FAT":
                           Log.Warn("Removing FAT formated drive type, as this causes ACL Failures [{0}]", di.Name);
                           seemsOK = false;
                           break;
                        default:
                           seemsOK = true;
                           break;
                     }
                  }
                  break;
               case DriveType.Unknown:
               case DriveType.NoRootDirectory:
               case DriveType.Network:
               case DriveType.CDRom:
               case DriveType.Ram:
                  seemsOK = true;
                  break;
               default:
                  throw new ArgumentOutOfRangeException("driveType", "Unknown type detected");
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Check Drive Format Type threw:", ex);
            seemsOK = false;

         }
         return seemsOK;

      }
   }
}
