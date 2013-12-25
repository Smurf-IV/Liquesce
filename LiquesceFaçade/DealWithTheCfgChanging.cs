#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="DealWithTheCfgChanging.cs" company="Smurf-IV">
// 
//  Copyright (C) 2013 Simon Coghlan (Aka Smurf-IV)
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using System.Linq;
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
               if (mtDetail != null)
               {
                  currentConfigDetails.MountDetails.Add(mtDetail);
               }
            }
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

      private MountDetail mtDetail;


      private void XOnUnknownElement(object sender, XmlElementEventArgs xeeArgs)
      {
         string innerText = xeeArgs.Element.InnerText;
         switch (xeeArgs.Element.Name)
         {
            case "DriveLetter":
               FillInMtDetail(xeeArgs);
               mtDetail.DriveLetter = innerText;
               break;
            case "VolumeLabel":
               FillInMtDetail(xeeArgs);
               mtDetail.VolumeLabel = innerText;
               break;
            case "AllocationMode":
               FillInMtDetail(xeeArgs);
               Enum.TryParse(char.ToUpper(innerText[0]) + innerText.Substring(1), out mtDetail.AllocationMode);
               break;
            case "HoldOffBufferBytes":
               FillInMtDetail(xeeArgs);
               UInt64.TryParse(innerText, out mtDetail.HoldOffBufferBytes);
               break;

            case "SourceLocations":
               FillInMtDetail(xeeArgs);
               string[] split = innerText.Split(new string[]{"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
               foreach (string s1 in split.Select(s => s.Trim()).Where(s1 => !string.IsNullOrEmpty(s1)))
               {
                  mtDetail.SourceLocations.Add(new SourceLocation(s1));
               }
               break;
            default:
            Log.Fatal("Unable to find convertor for {0} for {1}", xeeArgs.Element.Name, innerText);
               break;
         }
      }

      private void FillInMtDetail(XmlElementEventArgs xeeArgs)
      {
         if (mtDetail == null)
         {
            mtDetail = new MountDetail();
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
