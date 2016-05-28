using log4net;
using Nix.Store.Client.Constants;
using Nix.Store.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Nix.Store.Client.Engine
{
    public class XmlLoader
    {
        protected static ILog Logger = LogManager.GetLogger(typeof(XmlLoader).Name);
        private const string ADD_LIST_METHOD = "Add";
        private const string UNKNOWN = "Unknown";
        private const string LIST_TYPE_MASK = "System.Collections.Generic.List`1[[{0}, {1}]]";
        private const string DICTIONARY_TYPE_MASK = "System.Collections.Generic.Dictionary`2[[System.String],[{0}, {1}]]";

        protected void LoadSection(ref ConcurrentDictionary<string, IDictionary<string, object>> settings, string bagType, XElement section)
        {
            var settingsSection = ParseSection(section);
            if (settings.ContainsKey(bagType))
            {                
                IDictionary<string, object> current;
                settings.TryGetValue(bagType, out current);
                foreach (var newSetting in settingsSection)
                {
                    if (!current.ContainsKey(newSetting.Key))
                    {
                        current.Add(newSetting);
                    }
                    else
                    {
                        Logger.Warn(String.Format("Config key duplication detected {0} with properties bag {1}, overwriting value", newSetting.Key, bagType.ToString()));
                        current[newSetting.Key] = newSetting.Value;
                    }
                }
            }
            else
            {
                settings.TryAdd(bagType, settingsSection);
            }
        }

        protected IDictionary<string, object> ParseSection(XElement section)
        {
            var sectionDictionary = new Dictionary<string, object>();
            if (section != null)
            {
                foreach (var settings in section.Elements(XName.Get(XmlTags.SETTINGS_TAG)))
                {
                    foreach (XElement setting in settings.Elements(XName.Get(XmlTags.SETTING_TAG)))
                    {
                        string key;
                        object configElement;
                        if (this.TryCreateSettingObject(setting, out key, out configElement))
                            sectionDictionary.Add(key, configElement);
                    }
                }
            }
            else
                return null;
            return sectionDictionary;
        }

        public bool TryCreateSettingObject(XElement setting, out string elementKey, out object element)
        {
            XAttribute key = null;
            XAttribute type = null;
            try
            {
                key = setting.Attribute(XName.Get(XmlTags.SETTING_KEY));
                type = setting.Attribute(XName.Get(XmlTags.SETTING_TYPE));
                element = this.CreateConfigElement(setting, type);
                elementKey = key.Value;
                return true;
            }
            catch (LazyDeserializationNeededException lde)
            {
                var lazyDeserializeObject = new LazyDeserializeObject(key.Value, lde.TypeRequested, setting);
                element = lazyDeserializeObject;
                elementKey = key.Value;
                return true;
            }
            catch (Exception ex)
            {
                var mask = "Unable to load {0} key with type {1}";
                var keyValue = key != null ? key.Value : UNKNOWN;
                var typeValue = type != null ? type.Value : "String";
                Logger.WarnFormat(String.Format(mask, keyValue, typeValue), ex);
                element = null;
                elementKey = null;
                return false;
            }
        }

        protected object CreateConfigElement(XElement setting, XAttribute elementType = null)
        {
            string type = null;
            XAttribute itemTypeAttr = setting.Attribute(XName.Get(XmlTags.SETTING_ITEMTYPE));
            if (setting.Attribute(XName.Get(XmlTags.SETTING_TYPE)) != null)
            {
                elementType = setting.Attribute(XName.Get(XmlTags.SETTING_TYPE));
            }
            type = elementType != null ? elementType.Value : String.Empty;

            Type castType = typeof(System.String);
            MethodInfo addMethod = null;

            ValidTypes typeEnum = ParseType(type);  
            switch (typeEnum)
            {
                case ValidTypes.Int:
                    return int.Parse(setting.Attribute(XName.Get(XmlTags.SETTING_VALUE)).Value);
                case ValidTypes.Decimal:
                    return decimal.Parse(setting.Attribute(XName.Get(XmlTags.SETTING_VALUE)).Value, CultureInfo.InvariantCulture);
                case ValidTypes.Bit:
                case ValidTypes.Bool:
                    return bool.Parse(setting.Attribute(XName.Get(XmlTags.SETTING_VALUE)).Value);
                case ValidTypes.List:
                    castType = this.GetCastType(setting, elementType);
                    addMethod = castType.GetMethod(ADD_LIST_METHOD);
                    var list = Activator.CreateInstance(castType);
                    foreach (var element in setting.Elements())
                        addMethod.Invoke(list, new object[] { this.CreateConfigElement(element, itemTypeAttr) });
                    return list;
                case ValidTypes.Dictionary:
                    castType = this.GetCastType(setting, elementType);
                    addMethod = castType.GetMethod(ADD_LIST_METHOD);
                    var dic = Activator.CreateInstance(castType);
                    foreach (var element in setting.Elements(XName.Get(XmlTags.INNER_KEYVALUE)))
                    {
                        var key = element.Attribute(XName.Get(XmlTags.SETTING_KEY));
                        addMethod.Invoke(dic, new object[] { key.Value, this.CreateConfigElement(element, itemTypeAttr) });
                    }
                    return dic;
                case ValidTypes.String:             
                    return setting.Attribute(XName.Get(XmlTags.SETTING_VALUE)) != null ? setting.Attribute(XName.Get(XmlTags.SETTING_VALUE)).Value : setting.Value;
                case ValidTypes.Custom:
                default:                    
                    var serializer = new System.Xml.Serialization.XmlSerializer(this.GetCastType(setting, elementType));
                    var name = setting.Name.ToString().ToLowerInvariant();
                    if (name == XmlTags.INNER_ITEM || name == XmlTags.SETTING_TAG)
                        return serializer.Deserialize(new StringReader(setting.FirstNode.ToString()));
                    else
                        return serializer.Deserialize(new StringReader(setting.ToString()));                
            }
        }

        protected Type GetCastType(XElement setting, XAttribute elementType = null)
        {
            string type = null;            
            string itemType = null;
            string elementTypeValue = null;

            var typeAttr = setting.Attribute(XName.Get(XmlTags.SETTING_TYPE));
            var itemTypeAttr = setting.Attribute(XName.Get(XmlTags.SETTING_ITEMTYPE));

            if (elementType != null)
                elementTypeValue = elementType.Value;
            else if (typeAttr != null)
                elementTypeValue = typeAttr.Value;
            if (itemTypeAttr != null)
                itemType = itemTypeAttr.Value;

            ValidTypes typeEnum = ParseType(elementTypeValue);  
            switch (typeEnum)
            {
                case ValidTypes.Int:
                    return typeof(System.Int32);
                case ValidTypes.Decimal:
                    return typeof(System.Decimal);
                case ValidTypes.Bit:
                case ValidTypes.Bool:
                    return typeof(System.Boolean);
                case ValidTypes.List:                                        
                    if (!setting.HasElements)
                        throw new Exception(String.Format("Type List found with empty elements, unable to get child type."));
                    var qualifiedItemType = this.GetCastType(setting.Elements().First(), itemTypeAttr);
                    type = String.Format(LIST_TYPE_MASK, qualifiedItemType.FullName, qualifiedItemType.Assembly.FullName);                    
                    return Type.GetType(type);
                case ValidTypes.Dictionary:
                    if (!setting.HasElements)
                        throw new Exception(String.Format("Type Dictionary found with empty elements, unable to get child type."));
                    var itemCastedType = this.GetCastType(setting.Elements().First(), itemTypeAttr);
                    type = String.Format(DICTIONARY_TYPE_MASK, itemCastedType.FullName, itemCastedType.Assembly.FullName);
                    return Type.GetType(type);
                case ValidTypes.String:                
                    return typeof(System.String);
                case ValidTypes.Custom:
                default:
                    var t = Type.GetType(elementTypeValue);
                    if (t == null)
                        throw new LazyDeserializationNeededException(elementTypeValue);     // Preparamos el XML para que se deserialze en destino, dado que no tenemos tipo en origen.
                    return t;
            }
        }

        private ValidTypes ParseType(string type)
        {
            ValidTypes typeEnum;
            if (String.IsNullOrEmpty(type))
                typeEnum = ValidTypes.String;
            else if (!Enum.TryParse(type, true, out typeEnum))
                typeEnum = ValidTypes.Custom;
            return typeEnum;
        }
    }
}
