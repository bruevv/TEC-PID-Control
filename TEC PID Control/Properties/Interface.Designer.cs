﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TEC_PID_Control.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.10.0.0")]
    internal sealed partial class Interface : global::System.Configuration.ApplicationSettingsBase {
        
        private static Interface defaultInstance = ((Interface)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Interface())));
        
        public static Interface Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("TemperatureCPerS:C/s")]
        public string PIDCRUnit {
            get {
                return ((string)(this["PIDCRUnit"]));
            }
            set {
                this["PIDCRUnit"] = value;
            }
        }
    }
}
