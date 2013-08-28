#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ProcessIdentity.cs" company="Smurf-IV">
// 
//  Copyright (C) 2011-2012 Simon Coghlan (Aka Smurf-IV)
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
using System.Diagnostics;
using System.Security.Principal;

namespace LiquesceSvc
{
   internal static class ProcessIdentity
   {
      private static readonly CacheHelper<int, WindowsIdentity> cacheProcessIdToWi = new CacheHelper<int, WindowsIdentity>(60);
      private static readonly uint systemProcessId = GetSystemProcessId();

      /// <summary>
      /// Find the System.exe ID to prevent impersonation of it.
      /// This is to workaround the impersonation of a user coming over the share access
      /// </summary>
      /// <exception cref="SystemException">thrown if system.exe is not found</exception>
      /// <returns>systemProcessId</returns>
      private static uint GetSystemProcessId()
      {
         Process[] processesByName = Process.GetProcessesByName("System");
         if (processesByName.Length > 0)
            return (uint) processesByName[0].Id;
         throw new SystemException("Unable to identify System.exe process ID");
      }

      /// <summary>
      /// Pass in the processID from Dokan and the Anonymouse delegate Action.
      /// </summary>
      /// <param name="processId"></param>
      /// <param name="act"></param>
      /// <remarks>
      /// If the process is the System.exe, then this has probably come over the SMB2 or other network protocol.
      /// Which means that it will have checked the access permissions for whatever the action is going to be.
      /// http://msdn.microsoft.com/en-us/library/gg465326%28v=PROT.10%29.aspx
      /// </remarks>
      static public void Invoke(uint processId, Action act)
      {
         if (CouldBeSMB(processId))
            act();
         else
            InvokeHelper((int)processId, act);
      }

      /// <summary>
      /// Does the processID match the system ID ?
      /// </summary>
      /// <param name="processId"></param>
      /// <returns></returns>
      private static bool CouldBeSMB(uint processId)
      {
         return (systemProcessId == processId);
      }

      private static void InvokeHelper(int processId, Action act)
      {
         // To minimise the cache footrint.. All that is needed is the WindowsIdentity from the process
         WindowsIdentity wi;
         if (!cacheProcessIdToWi.TryGetValue(processId, out wi))
         {
            using (Process ownerProcess = Process.GetProcessById(processId))
            {
               wi = ownerProcess.WindowsIdentity();
               cacheProcessIdToWi[processId] = wi;
            }
         }
         else
            cacheProcessIdToWi.Touch(processId);
         using (wi.Impersonate())
         {
            act();
         }
      }

   }
}
