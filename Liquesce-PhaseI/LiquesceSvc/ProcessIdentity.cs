﻿#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="CacheHelper.cs" company="Smurf-IV">
// 
//  Copyright (C) 2011 Smurf-IV
// 
//  This program is free software: you can redistribute it and/or modify.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// 
//  </copyright>
//  <summary>
//  Url: http://liquesce.wordpress.com/2011/06/07/c-dictionary-cache-that-has-a-timeout-on-its-values/
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
         Process[] processesByName = Process.GetProcessesByName("system");
         if (processesByName.Length > 0)
            return (uint) processesByName[0].Id;
         throw new SystemException("Unable to identify System.exx process ID");
      }

      /// <summary>
      /// Pass in the processID from Dokan and the Anonymouse delegate Action
      /// </summary>
      /// <param name="processId"></param>
      /// <param name="act"></param>
      static public void Invoke(uint processId, Action act)
      {
         if (systemProcessId == processId)
            act();
         else
            InvokeHelper((int)processId, act);
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
         using (WindowsImpersonationContext impersonationContext = wi.Impersonate())
         {
            act();
         }
      }

   }
}
