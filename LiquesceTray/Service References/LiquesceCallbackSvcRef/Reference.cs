#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Reference.cs" company="Smurf-IV">
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
namespace LiquesceTray.LiquesceCallbackSvcRef {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="LiquesceCallbackSvcRef.ILiquesceCallBack", CallbackContract=typeof(LiquesceTray.LiquesceCallbackSvcRef.ILiquesceCallBackCallback), SessionMode=System.ServiceModel.SessionMode.Required)]
    public interface ILiquesceCallBack {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/ILiquesceCallBack/Subscribe")]
        void Subscribe(System.Guid id);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/ILiquesceCallBack/Unsubscribe")]
        void Unsubscribe(System.Guid id);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ILiquesceCallBackCallback {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://tempuri.org/ILiquesceCallBack/Update")]
        void Update(LiquesceFacade.LiquesceSvcState state, string message);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ILiquesceCallBackChannel : LiquesceTray.LiquesceCallbackSvcRef.ILiquesceCallBack, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class LiquesceCallBackClient : System.ServiceModel.DuplexClientBase<LiquesceTray.LiquesceCallbackSvcRef.ILiquesceCallBack>, LiquesceTray.LiquesceCallbackSvcRef.ILiquesceCallBack {
        
        public LiquesceCallBackClient(System.ServiceModel.InstanceContext callbackInstance) : 
                base(callbackInstance) {
        }
        
        public LiquesceCallBackClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
                base(callbackInstance, endpointConfigurationName) {
        }
        
        public LiquesceCallBackClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public LiquesceCallBackClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public LiquesceCallBackClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, binding, remoteAddress) {
        }
        
        public void Subscribe(System.Guid id) {
            base.Channel.Subscribe(id);
        }
        
        public void Unsubscribe(System.Guid id) {
            base.Channel.Unsubscribe(id);
        }
    }
}
