using Nix.Store.Client.Constants;
using Nix.Store.Client.Engine;
using Nix.Store.Server.Exceptions;
using Nix.Store.Server.Repositories.Factories;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace Nix.Store.Server
{
    public class DatabaseEngine
    {
        public EntitiesFactory EntitiesFactory = new EntitiesFactory();

        protected string ConnectionString { get; set; }

        public DatabaseEngine(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        #region Application Feeding Configuration Methods

        /// <summary>
        /// Devuelve una coleccion de configuraciones serializada en una cadena de texto XML.
        /// </summary>
        /// <param name="appKey">Llave de aplicacion</param>
        /// <returns>Coleccion de configuraciones en XML codificada UTF-8</returns>
        /// <exception cref="AppKeyNotFoundException"><see cref="Application"/> no encontrado en la base de datos con la llave proporcionada.</exception>
        public string GetRawAppSettings(string appKey)
        {
            
            using (var context = EntitiesFactory.Create(ConnectionString))
            {
                var app = context.Applications.FirstOrDefault(x => x.Name == appKey);
                if (app == null)
                    throw new AppKeyNotFoundException(appKey);
                using (var reader = EncodeSettings(app).CreateReader())
                {
                    reader.MoveToContent();
                    return reader.ReadOuterXml();
                }
            }                
            
        }

        /// <summary>
        /// Devuelve una coleccion de configuraciones deserializada en formato dicionario de objetos con todos sus grupos.
        /// </summary>
        /// <param name="appKey">Llave de aplicacion</param>
        /// <returns>Dicionario de objetos complejos con la coleccion de configuraciones</returns>
        /// <remarks>El dicionario usa los nombres de grupo como llave y los valores son las configuraciones en forma de <see cref="IDictionary{String, Object}">Dicionario de objetos</see></remarks>
        /// <exception cref="AppKeyNotFoundException"><see cref="Application"/> no encontrado en la base de datos con la llave proporcionada.</exception>
        /// <seealso cref="GetAppGroupSettings"/>
        public ConcurrentDictionary<string, IDictionary<string, object>> GetAppSettings(string appKey)
        {
            
            var settingStore = new ConcurrentDictionary<string, IDictionary<string, object>>();
            using (var context = EntitiesFactory.Create(ConnectionString))
            {
                var app = context.Applications.FirstOrDefault(x => x.Name == appKey);
                if (app == null)
                    throw new AppKeyNotFoundException(appKey);
                foreach (var group in app.Groups)
                {
                    var settings = BuildGroup(group);
                    settingStore.TryAdd(group.Key, settings);
                }
            }
            return settingStore;
            
        }

        /// <summary>
        /// Devuelve una coleccion de configuraciones deserializada en formato dicionario de objetos con solo el grupo definido.
        /// </summary>
        /// <param name="appKey">Llave de aplicacion</param>
        /// <param name="groupName">Llave del grupo de configuraciones</param>
        /// <returns>Dicionario de objetos complejos con la coleccion de configuraciones</returns>
        /// <remarks>Este metodo se usa con <see cref="SettingsManager.MergeSettings"/> para integrar varios grupos en un contendor.</remarks>
        /// <exception cref="AppKeyNotFoundException"><see cref="Application"/> no encontrado en la base de datos con la llave proporcionada.</exception>
        /// <exception cref="GroupKeyNotFoundException"><see cref="Group"/> no encontrado en la base de datos con la llave proporcionada.</exception>
        /// <seealso cref="GetAppSettings"/>
        public ConcurrentDictionary<string, IDictionary<string, object>> GetAppGroupSettings(string appKey, string groupName)
        {            
            using (var context = EntitiesFactory.Create(ConnectionString))
            {
                if (!context.Applications.Any(x => x.Name == appKey))
                    throw new AppKeyNotFoundException(appKey);
                var group = context.Groups.FirstOrDefault(x => x.Key == groupName && x.Application.Name == appKey);
                if (group == null)
                    throw new GroupKeyNotFoundException(appKey, groupName);
                var settings = new ConcurrentDictionary<string, IDictionary<string, object>>();
                settings.TryAdd(groupName, BuildGroup(group));
                return settings;
            }                    
        }

        /// <summary>
        /// Genera un conjunto de configuraciones para un grupo a partir de su entidad de base de datos.
        /// </summary>
        /// <param name="group"></param>
        /// <returns>Dicionario de objetos con las configuraciones del grupo</returns>
        /// <exception cref="SerializationException">No se ha conseguido deserilizar correctamente la entidad <see cref="Group"/></exception>
        private IDictionary<string, object> BuildGroup(Group group)
        {
            var xmlFileLoader = new XmlLoader();            
            var settingsGroup = new Dictionary<string, object>();
            foreach (var setting in group.Settings)
            {
                try
                {

                    var element = new XElement(XName.Get(XmlTags.SETTING_TAG));
                    var keyAttr = new XAttribute(XmlTags.SETTING_KEY, setting.Key);
                    element.Add(keyAttr);
                    var typeAttr = new XAttribute(XmlTags.SETTING_TYPE, setting.Type);
                    element.Add(typeAttr);
                    if (!String.IsNullOrEmpty(setting.ItemType))
                    {
                        var itemTypeAttr = new XAttribute(XmlTags.SETTING_ITEMTYPE, setting.ItemType);
                        element.Add(itemTypeAttr);
                    }
                    if (!String.IsNullOrEmpty(setting.Value))
                    {
                        var valueAttr = new XAttribute(XmlTags.SETTING_VALUE, setting.Value);
                        element.Add(valueAttr);
                    }
                    else
                        if (!String.IsNullOrEmpty(setting.Body))
                        {
                            var body = XElement.Parse(SurroundElements(setting.Body));
                            element.Add(body.Elements());
                        }

                    string key;
                    object configElement;
                    if (xmlFileLoader.TryCreateSettingObject(element, out key, out configElement))
                        settingsGroup.Add(key, configElement);                    
                }
                catch (Exception ex)
                {
                    throw new SerializationException(String.Format("Error Deserializing Group {0} into object store", group), ex);
                }
            }
            return settingsGroup;
        }

        #endregion
                
        #region Administrative Methods

        /// <summary>
        /// Devuelve una coleccion con todas las llaves de aplicacion presentes en la base de datos.
        /// </summary>
        /// <returns>Listado de llaves</returns>
        public List<string> GetAppKeys()
        {
            using (var context = EntitiesFactory.Create(ConnectionString))
            {
                return context.Applications.Select(x => x.Name).ToList();
            }
        }

        /// <summary>
        /// Devuelve la <see cref="Application"/> dada su llave
        /// </summary>
        /// <param name="appKey">Llave de aplicacion</param>
        /// <returns>Entidad con la configuracion de aplicacion</returns>
        /// <exception cref="AppKeyNotFoundException"><see cref="Application"/> no encontrado en la base de datos con la llave proporcionada.</exception>
        public Application GetAppFullSettings(string appKey)
        {
            using (var context = EntitiesFactory.Create(ConnectionString))
            {
                var appSettings = context.Applications.FirstOrDefault(x => x.Name == appKey);
                if (appSettings == null)
                    throw new AppKeyNotFoundException(appKey);
                return appSettings;
            }
        }

        /// <summary>
        /// Guarda las configuraciones de una aplicacion en la base de datos.
        /// </summary>
        /// <param name="appSettings">Entidad <see cref="Application"/> con las configuraciones.</param>
        /// <exception cref="AggregateException">Conjunto de <see cref="DbEntityValidationResult"/> antes del commit.</exception>
        /// <exception cref="Exception">Error generico, cuando no es posible lanzar una <see cref="AggregateException"/>.</exception>
        public void SetAppFullSettings(string requestAgent, Application appSettings)
        {
            using (var context = EntitiesFactory.Create(ConnectionString))
            {                
                var currentAppSettings = context.Applications.Find(appSettings.Id);
                Application workEntity = null;
                if (currentAppSettings == null)
                    workEntity = context.Applications.Add(appSettings);
                else
                {
                    var t_Entity = context.Entry(currentAppSettings);
                    t_Entity.CurrentValues.SetValues(appSettings);
                    workEntity = t_Entity.Entity;
                    foreach (Group group in appSettings.Groups)
                    {
                        if (group.Id == 0)
                            context.Groups.Add(group);
                        else
                        {
                            var dbGroup = context.Entry<Group>(context.Groups.Find(group.Id));
                            group.Application = workEntity;
                            group.ApplicationId = workEntity.Id;
                            dbGroup.CurrentValues.SetValues(group);
                        }
                        foreach (SettingMapping settingsMap in group.SettingMappings)
                        {
                            if (settingsMap.Id == 0)
                                context.SettingMappings.Add(settingsMap);
                            else
                            {
                                var dbSettingMap = context.Entry<SettingMapping>(context.SettingMappings.Find(settingsMap.Id));
                                settingsMap.Group = dbSettingMap.Entity.Group;
                                settingsMap.GroupId = dbSettingMap.Entity.GroupId;
                                dbSettingMap.CurrentValues.SetValues(settingsMap);
                            }
                        }
                        foreach (Setting setting in group.Settings)
                        {
                            if (setting.Id == 0)
                                context.Settings.Add(setting);
                            else
                            {
                                var dbSetting = context.Entry<Setting>(context.Settings.Find(setting.Id));
                                setting.Group = dbSetting.Entity.Group;
                                setting.GroupId = dbSetting.Entity.GroupId;
                                setting.SettingMapping = dbSetting.Entity.SettingMapping;
                                setting.MapId = dbSetting.Entity.MapId;
                                dbSetting.CurrentValues.SetValues(setting);
                            }
                        }
                    }
                }
                var errors = context.GetValidationErrors();                
                if (errors.Any())
                {
                    try
                    {
                        throw new AggregateException(errors
                            .Select(x =>
                            {
                                var enex = new AggregateException(x.ValidationErrors
                                .Select(e =>
                                    {
                                        var eex = new Exception(e.ErrorMessage);
                                        eex.Source = e.PropertyName;
                                        eex.Data.Add("PropertyName", e.PropertyName);
                                        eex.Data.Add("DbValidationError", e);
                                        return eex;
                                    }));
                                enex.Source = x.Entry.Entity.GetType().FullName;
                                enex.Data.Add("State", x.Entry.State.ToString());
                                enex.Data.Add("IsValid", x.IsValid);
                                enex.Data.Add("DbEntityValidationResult", x);
                                return enex;
                            }));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("ValidationErrors found before commit new values for applicaction {0}", appSettings.Name), ex);
                    }
                }
                List<ChangeHistory> changeSet;                
                if (TryGetChanges(context, out changeSet) && changeSet.Any())
                {
                    changeSet.ForEach(x =>
                        {
                            x.AgentName = requestAgent;
                            x.ChangeDate = DateTime.Now;                            
                        });
                    context.ChangeHistories.AddRange(changeSet);
                }
                if (context.ChangeTracker.HasChanges())
                    context.SaveChanges();
            }
        }

        private bool TryGetChanges(ConfigEntities context, out List<ChangeHistory> changeSet)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            List<ChangeHistory> innerChangeSet = null;
            if (!context.ChangeTracker.HasChanges())
            {
                changeSet = null;
                return false;
            }
            var entries = context.ChangeTracker.Entries();
            if (entries.Any())
                innerChangeSet = new List<ChangeHistory>();
            foreach (var entry in entries)            
                if (entry.State != System.Data.Entity.EntityState.Unchanged)
                    LookupChange(entry.Entity.GetType().BaseType.Name, entry.CurrentValues, entry.State != EntityState.Added ? entry.OriginalValues : null, entry.State, ref innerChangeSet);
            changeSet = innerChangeSet;
            return true;                
        }

        private void LookupChange(string parentPropertyName, DbPropertyValues propertyValues, DbPropertyValues originalValues, EntityState state, ref  List<ChangeHistory> changes)
        {
            foreach (var propertyName in propertyValues.PropertyNames)
            {
                var nestedValues = propertyValues[propertyName] as DbPropertyValues;                
                if (nestedValues != null)
                    LookupChange(String.Format("{0}{1}.", parentPropertyName, propertyName), nestedValues, originalValues ?? originalValues[propertyName] as DbPropertyValues, state, ref changes);
                else 
                {    
                    var change = new ChangeHistory();
                    change.Action = state.ToString();
                    change.EntityName = String.Format("{0}.{1}", parentPropertyName, propertyName);
                    change.ChangedValue = state == EntityState.Deleted ? null : ParseEntityValue(propertyValues[propertyName]);
                    change.OriginalValue = state == EntityState.Added ? null : ParseEntityValue(originalValues[propertyName]);                    
                    if (change.ChangedValue != change.OriginalValue)
                        changes.Add(change);
                }
            }
        }

        private string ParseEntityValue(object value)
        {
            if (value == null)
                return null;
            return value.ToString();
        }

        #endregion

        #region Util Methods

        /// <summary>
        /// Codifica <see cref="Application" a XML dado el formato esperado. />
        /// </summary>
        /// <param name="appSettings"><see cref="Application"/> a serializar.</param>
        /// <returns>Devuelve un objeto <see cref="XDocument"/> con la informacion serializada.</returns>
        /// <exception cref="SerializationException">No es posible serialiar el contenido segun el formato establecido.</exception>
        public XDocument EncodeSettings(Application appSettings)
        {
            try
            {
                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
                var configElement = new XElement(XName.Get(XmlTags.DOCUMENT_TAG), new XAttribute(XName.Get(XmlTags.DOCUMENT_APPKEY), appSettings.Id));
                var groupsElements = new XElement(XName.Get(XmlTags.GROUPS_TAG));

                foreach (var group in appSettings.Groups)
                {
                    var groupElement = new XElement(XName.Get(XmlTags.GROUP_TAG));
                    groupElement.Add(new XAttribute(XName.Get(XmlTags.GROUP_IDKEY), group.Id), new XAttribute(XName.Get(XmlTags.GROUP_KEY), group.Key));
                    foreach (var mapId in group.Settings.Select(x => x.MapId).Distinct())
                    {
                        var mapping = group.SettingMappings.Single(x => x.Id == mapId);
                        var settingsElement = new XElement(XName.Get(XmlTags.SETTINGS_TAG));
                        if (mapping.Key != String.Empty)
                            settingsElement.Add(new XAttribute(XName.Get(XmlTags.SETTINGS_ID), mapping.Key));
                        foreach (var setting in group.Settings.Where(x => x.MapId == mapId))
                        {
                            var settingElement = new XElement(
                                XName.Get(XmlTags.SETTING_TAG),
                                new XAttribute(XName.Get(XmlTags.SETTING_KEY), setting.Key),
                                new XAttribute(XName.Get(XmlTags.SETTING_ID), setting.Id));
                            if (Type.GetType(setting.Type) != typeof(string))                            
                                settingElement.Add(new XAttribute(XName.Get(XmlTags.SETTING_TYPE), setting.Type));
                            if (!String.IsNullOrEmpty(setting.ItemType))
                                settingElement.Add(new XAttribute(XName.Get(XmlTags.SETTING_ITEMTYPE), setting.ItemType));
                            if (!String.IsNullOrEmpty(setting.Value))
                                settingElement.Add(new XAttribute(XName.Get(XmlTags.SETTING_VALUE), setting.Value));
                            if (!String.IsNullOrEmpty(setting.Body))
                            {
                                var rootedBody = XElement.Parse(SurroundElements(setting.Body));
                                foreach (XElement element in rootedBody.Elements())
                                    settingElement.Add(element);
                            }                             
                            settingsElement.Add(settingElement);
                        }
                        groupElement.Add(settingsElement);
                    }
                    groupsElements.Add(groupElement);
                }
                configElement.Add(groupsElements);
                doc.Add(configElement);
                return doc;
            }
            catch (Exception ex)
            {
                throw new SerializationException(String.Format("Error serializing into XML {0} entity with AppKey {1}", typeof(Application).Name, appSettings.Name), ex);
            }
        }

        /// <summary>
        /// Decodifica a una entidad <see cref="Application"/> un <see cref="XDocument"/> con la configuracion en XML.
        /// </summary>
        /// <param name="xmlSettings"><see cref="XDocument"/> con las configuraciones.</param>
        /// <returns>Entidad <see cref="Application"/> con las configuraciones.</returns>
        /// <exception cref="SerializationException">No es posible decodificar los datos hacia una entidad <see cref="Application"/>.</exception>
        public Application DecodeSettings(XDocument xmlSettings)
        {
            Application appSettings = null;
            try
            {
                appSettings = new Application();
                var configElement = xmlSettings.Element(XName.Get(XmlTags.DOCUMENT_TAG));
                var idAttr1 = configElement.Attribute(XName.Get(XmlTags.DOCUMENT_ID));
                appSettings.Id = idAttr1 != null ? int.Parse(idAttr1.Value) : 0;
                appSettings.Name = configElement.Attribute(XName.Get(XmlTags.DOCUMENT_APPKEY)).Value;
                foreach (XElement groupElement in configElement.Element(XName.Get(XmlTags.GROUPS_TAG)).Elements(XName.Get(XmlTags.GROUP_TAG)))
                {
                    var group = new Group();
                    group.Application = appSettings;
                    var idAttr2 = groupElement.Attribute(XName.Get(XmlTags.GROUP_IDKEY));
                    group.Id = idAttr2 != null ? int.Parse(idAttr2.Value) : 0;                     
                    group.Key = groupElement.Attribute(XName.Get(XmlTags.GROUP_KEY)).Value;
                    foreach (var settingsElement in groupElement.Elements(XName.Get(XmlTags.SETTINGS_TAG)))
                    {
                        var mapping = new SettingMapping();
                        var idAttr3 = settingsElement.Attribute(XName.Get(XmlTags.SETTINGS_ID));
                        mapping.Id = idAttr3 != null ? int.Parse(idAttr3.Value) : 0;                        
                        mapping.Key = settingsElement.Attribute(XName.Get(XmlTags.SETTINGS_KEY)).Value;
                        mapping.Group = group;
                        group.SettingMappings.Add(mapping);
                        foreach (var settingElement in settingsElement.Elements(XName.Get(XmlTags.SETTING_TAG)))
                        {
                            var setting = new Setting();
                            setting.SettingMapping = mapping;
                            setting.MapId = mapping.Id;
                            setting.Group = group;
                            var idAttr4 = settingElement.Attribute(XName.Get(XmlTags.SETTING_ID));                            
                            setting.Id = idAttr4 != null ? int.Parse(idAttr4.Value) : 0;
                            setting.Key = settingElement.Attribute(XName.Get(XmlTags.SETTING_KEY)).Value;
                            var typeAttr = settingElement.Attribute(XName.Get(XmlTags.SETTING_TYPE));                       
                            setting.Type = typeAttr != null ? typeAttr.Value : "string";
                            var valueAttr = settingElement.Attribute(XName.Get(XmlTags.SETTING_VALUE));
                            if (valueAttr != null)
                                setting.Value = valueAttr.Value;
                            var itemTypeAttr = settingElement.Attribute(XName.Get(XmlTags.SETTING_ITEMTYPE));
                            if (itemTypeAttr != null)
                                setting.ItemType = itemTypeAttr.Value;
                            var bodyElements = settingElement.Elements();
                            if (bodyElements != null)                            
                                using (var reader = settingElement.CreateReader())
                                {
                                    reader.MoveToContent();
                                    setting.Body = reader.ReadInnerXml();
                                }
                            group.Settings.Add(setting);                            
                        }
                    }
                    appSettings.Groups.Add(group);
                }
                return appSettings;                    
            }
            catch (Exception ex)
            {
                if (appSettings != null)   
                    throw new SerializationException(String.Format("Error deserializing from XML {0} entity with AppKey {1}", typeof(Application).Name, appSettings.Name), ex);
                else
                    throw new SerializationException(String.Format("Error deserializing from XML {0} entity", typeof(Application).Name), ex);
            }
        }
                
        private string SurroundElements(string source)
        {
            return String.Format(@"{0}{1}{2}", XmlTags.ROOT_OPEN, source, XmlTags.ROOT_CLOSE);
        }

        #endregion




    }
}
