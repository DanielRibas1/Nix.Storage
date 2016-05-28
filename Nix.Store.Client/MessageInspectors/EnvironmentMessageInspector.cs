using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Web;

namespace Nix.Store.Client.MessageInspectors
{
    public class EnvironmentMessageInspector : IClientMessageInspector
    {
        private const string ENVIRONMENT_HEADER = "environment";
        private string _currentEnvironment;

        public EnvironmentMessageInspector(string currentEnvironment)
        {
            this._currentEnvironment = currentEnvironment;
        }

        #region IClientMessageInspector Implementation

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            object httpRequestMessageObject;
            if (reply.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
            {
                var httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                if (httpRequestMessage.Headers[ENVIRONMENT_HEADER] != this._currentEnvironment)
                    throw new Exception();
            }
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            HttpRequestMessageProperty httpRequestMessage;
            object httpRequestMessageObject;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                if (string.IsNullOrEmpty(httpRequestMessage.Headers[ENVIRONMENT_HEADER]))
                {
                    httpRequestMessage.Headers[ENVIRONMENT_HEADER] = this._currentEnvironment;
                }
            }
            else
            {
                httpRequestMessage = new HttpRequestMessageProperty();
                httpRequestMessage.Headers.Add(ENVIRONMENT_HEADER, this._currentEnvironment);
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);
            }            
            return null;
        }

        #endregion
    }
}