using System;
using System.Collections.Generic;
using System.Data.EntityClient;
using System.Linq;
using System.Text;

namespace Nix.Store.Server.Repositories.Factories
{
    public class EntitiesFactory
    {
        public ConfigEntities Create(string connectionString)
        {
            var entityBuilder = new EntityConnectionStringBuilder();
            entityBuilder.ProviderConnectionString = connectionString;            
            entityBuilder.Metadata = @"res://*/ConfigurationModel.csdl|res://*/ConfigurationModel.ssdl|res://*/ConfigurationModel.msl";
            entityBuilder.Provider = "System.Data.SqlClient";       
            return new ConfigEntities(entityBuilder.ConnectionString);
        }
    }
}
