#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceProxy.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2013 Simon Coghlan (Aka Smurf-IV)
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

using System.ServiceModel;

namespace LiquesceFacade
{
   public class LiquesceProxy : ClientBase<ILiquesce>, ILiquesce
    {
        public LiquesceProxy()
        {
        }

        public LiquesceProxy(string endpointConfigurationName)
            : base(endpointConfigurationName)
        {
        }

        public LiquesceProxy(string endpointConfigurationName, string remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        public LiquesceProxy(string endpointConfigurationName, EndpointAddress remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        public LiquesceProxy(System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {
        }

      #region Implementation of ILiquesce

      public void Stop()
      {
         base.Channel.Stop();
      }

      public void Start()
      {
         base.Channel.Start();
      }

      public LiquesceSvcState LiquesceState
      {
         get { return base.Channel.LiquesceState; }
      }

      public ConfigDetails ConfigDetails
      {
         get { return base.Channel.ConfigDetails; }
      }

      #endregion
    }
}
