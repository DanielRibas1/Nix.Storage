//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Nix.Store.Server
{
    using System;
    using System.Collections.Generic;
    
    public partial class Group
    {
        public Group()
        {
            this.Settings = new HashSet<Setting>();
            this.SettingMappings = new HashSet<SettingMapping>();
        }
    
        public int Id { get; set; }
        public string Key { get; set; }
        public int ApplicationId { get; set; }
    
        public virtual Application Application { get; set; }
        public virtual ICollection<Setting> Settings { get; set; }
        public virtual ICollection<SettingMapping> SettingMappings { get; set; }
    }
}
