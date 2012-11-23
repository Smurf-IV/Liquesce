#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceFacade.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Simon Coghlan (Aka Smurf-IV)
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


using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using LiquesceFacade;
using NLog;

namespace LiquesceSvc
{
   [ServiceBehavior(
      InstanceContextMode = InstanceContextMode.Single,
           ConcurrencyMode = ConcurrencyMode.Multiple,
           IncludeExceptionDetailInFaults = true
           )
   ]
   public class LiquesceFacade : ILiquesce
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceFacade()
      {
         Log.Debug("Object Created");
      }

      public void Stop()
      {
         Log.Debug("Calling Stop");
         ManagementLayer.Instance.Stop();
      }

      public void Start()
      {
         Log.Debug("Calling Start");
         // Queue the main work as a thread pool task as we want this method to finish promptly.
         ThreadPool.QueueUserWorkItem(ManagementLayer.Instance.Start);
      }

      public LiquesceSvcState LiquesceState
      {
         get
         {
            Log.Debug("Calling State");
            return ManagementLayer.Instance.State;
         }
      }

      public ConfigDetails ConfigDetails
      {
         get
         {
            Log.Debug("Calling get_ConfigDetails");
            return ManagementLayer.Instance.CurrentConfigDetails;
         }
         set
         {
            Log.Debug("Calling set_ConfigDetails");
            ManagementLayer.Instance.CurrentConfigDetails = value;
         }
      }

   }
}
