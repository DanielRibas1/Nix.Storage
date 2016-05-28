using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nix.Store.Client.Constants
{
    public class XmlTags
    {
        public const string ROOT_OPEN = "<root>";
        public const string ROOT_CLOSE = "</root>";

        public const string DOCUMENT_TAG = "configuration";
        public const string DOCUMENT_ID = "id";
        public const string DOCUMENT_APPKEY = "appkey";
        public const string GROUPS_TAG = "groups";
        public const string GROUP_TAG = "group";
        public const string GROUP_IDKEY = "id";
        public const string GROUP_KEY = "key";
        public const string SETTINGS_TAG = "settings";
        public const string SETTINGS_ID = "id";
        public const string SETTINGS_KEY = "key";
        public const string SETTING_TAG = "setting";
        public const string SETTING_ID = "id";
        public const string SETTING_KEY = "key";
        public const string SETTING_TYPE = "type";
        public const string SETTING_VALUE = "value";
        public const string SETTING_ITEMTYPE = "itemtype";
        public const string INNER_ITEM = "item";
        public const string INNER_KEYVALUE = "keyvalue";
    }
}
