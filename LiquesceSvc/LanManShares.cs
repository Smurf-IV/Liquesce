using System.Collections.Generic;
using Microsoft.Win32;

namespace LiquesceSvc
{
   class RegistryLanManShare
   {
      public string Name;
      public string CSCFlags;
      public uint MaxUses;
      public string Path;
      public uint Permissions;
      public string Remark;
      public int Type;
   }

   class LanManShares
   {
      static public List<RegistryLanManShare> GetLanManShares()
      {
         List<RegistryLanManShare> shares = new List<RegistryLanManShare>();
         RegistryKey RegShares = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\lanmanserver\Shares", false);
         if (RegShares != null)
         {
            foreach (string name in RegShares.GetValueNames())
            {
               RegistryLanManShare share = new RegistryLanManShare {Name = name};
               string[] values = (string[]) RegShares.GetValue(name);
               if (values != null)
               {
                  foreach (string value in values)
                  {
                     try
                     {
                        string[] splits = value.Split('=');
                        switch (splits[0])
                        {
                           case "CSCFlags":
                              share.CSCFlags = splits[1];
                              break;
                           case "MaxUses":
                              share.MaxUses = uint.Parse(splits[1]);
                              break;
                           case "Path":
                              share.Path = splits[1];
                              break;
                           case "Permissions":
                              share.Permissions = uint.Parse(splits[1]);
                              break;
                           case "Remark":
                              share.Remark = splits[1];
                              break;
                           case "Type":
                              share.Type = int.Parse(splits[1]);
                              break;
                        }
                     }
                     catch { }
                  }
               }
               shares.Add(share);
            }
         }
         return shares;
      }

      static public List<RegistryLanManShare> MatchDriveLanManShares(string DriveLetter)
      {
         return GetLanManShares().FindAll(share => share.Path.StartsWith(DriveLetter));
      }

   }
}
