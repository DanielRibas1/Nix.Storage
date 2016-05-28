using Nix.Store.Client.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Nix.Store.Client.Contract
{    
    [ServiceContract(Namespace= "Nix.Store.Client")]
    [ServiceKnownType(typeof(List<string>))]
    [ServiceKnownType(typeof(Dictionary<string, List<string>>))]
    [ServiceKnownType(typeof(LazyDeserializeObject))]
    public interface IFeedService
    {
        [OperationContract]
        string GetRawAppSettings(string appKey);
        [OperationContract]
        IDictionary<string, IDictionary<string, object>> GetNetAppSettings(string appKey);
        [OperationContract]
        IDictionary<string, IDictionary<string, object>> GetNetGroupAppSettings(string appKey, string groupName);        
    }
}
