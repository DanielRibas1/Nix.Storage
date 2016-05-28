using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Nix.Store.Client.Contract
{
    [ServiceContract(Namespace = "Nix.Store.Client")]
    public interface IAdminService
    {
        [OperationContract]
        void SetXmlSettings(string requestAgent, string rawXmlSettings);
        [OperationContract]
        string GetXmlSettings(string appKey);
        [OperationContract]
        List<string> GetAvailableAppKeys();
    }
}
