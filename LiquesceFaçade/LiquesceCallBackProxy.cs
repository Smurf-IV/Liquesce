#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceCallBackProxy.cs" company="Smurf-IV">
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

using System;
using System.ServiceModel;

namespace LiquesceFacade
{
   public class LiquesceCallBackProxy : DuplexClientBase<ILiquesceCallBack>, ILiquesceCallBack
   {

      public LiquesceCallBackProxy(InstanceContext callbackInstance)
         : base(callbackInstance)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, string endpointConfigurationName)
         : base(callbackInstance, endpointConfigurationName)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress)
         : base(callbackInstance, endpointConfigurationName, remoteAddress)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress)
         : base(callbackInstance, endpointConfigurationName, remoteAddress)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress)
         : base(callbackInstance, binding, remoteAddress)
      {
      }

      #region Implementation of ILiquesceCallBack

      public void Subscribe(Client id)
      {
         try
         {
            base.Channel.Subscribe(id);
         }
         catch (EndpointNotFoundException ex)
         {

            throw new ApplicationException("Liquesce service is off. Please start the Liquesce server first then try to connect",ex);
         }
         catch (CommunicationException ex)
         {

            throw new ApplicationException("Liquesce Service is off. Please start the Liquesce server first then try to connect", ex);
         }
      }

      public void Unsubscribe(Client id)
      {
         base.Channel.Unsubscribe(id);
      }

      #endregion
   }
}
