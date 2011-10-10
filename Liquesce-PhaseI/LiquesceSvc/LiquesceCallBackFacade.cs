#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceCallBackFacade.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
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
using System.ServiceModel;
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
   class LiquesceCallBackFacade : ILiquesceCallBack
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceCallBackFacade()
      {
         Log.Debug("Object Created");
      }
      public void Subscribe(Guid id)
      {
         Log.Debug("Calling Subscribe");
         ManagementLayer.Instance.Subscribe(id);
      }

      public void Unsubscribe(Guid id)
      {
         Log.Debug("Calling Unsubscribe");
         ManagementLayer.Instance.Unsubscribe(id);
      }
   }
}
