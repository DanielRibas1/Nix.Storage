using Nix.Store.Client.MessageInspectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Web;

namespace Nix.Store.Server.Behaviours
{
    public class EnvironmentEndpointBehaviour : IEndpointBehavior
    {
        #region IEndpointBehavior Implementation

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void Validate(ServiceEndpoint endpoint) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            // TODO Activar
//#if !DEBUG
//            var inspector = new EnvironmentMessageInspector(EnvironmentScope.Current);
//            clientRuntime.MessageInspectors.Add(inspector);
//#endif
        }       
        #endregion
    }

    public class EnvironmentEndpointBehaviourExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType { get { return typeof(EnvironmentEndpointBehaviour); } }
        protected override object CreateBehavior()
        {
            return new EnvironmentEndpointBehaviour();
        }       
    }
}