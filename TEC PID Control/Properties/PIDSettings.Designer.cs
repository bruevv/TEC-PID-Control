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
    internal sealed partial class PIDSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static PIDSettings defaultInstance = ((PIDSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new PIDSettings())));
        
        public static PIDSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public double CtrlP {
            get {
                return ((double)(this["CtrlP"]));
            }
            set {
                this["CtrlP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public double CtrlI {
            get {
                return ((double)(this["CtrlI"]));
            }
            set {
                this["CtrlI"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public double CtrlD {
            get {
                return ((double)(this["CtrlD"]));
            }
            set {
                this["CtrlD"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.5")]
        public double MaxCtrlPar {
            get {
                return ((double)(this["MaxCtrlPar"]));
            }
            set {
                this["MaxCtrlPar"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public double MinCtrlPar {
            get {
                return ((double)(this["MinCtrlPar"]));
            }
            set {
                this["MinCtrlPar"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2.8")]
        public double GlobalGain {
            get {
                return ((double)(this["GlobalGain"]));
            }
            set {
                this["GlobalGain"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1.1")]
        public double TimeConstant {
            get {
                return ((double)(this["TimeConstant"]));
            }
            set {
                this["TimeConstant"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public double MaxIntegralError {
            get {
                return ((double)(this["MaxIntegralError"]));
            }
            set {
                this["MaxIntegralError"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.1")]
        public double CRate {
            get {
                return ((double)(this["CRate"]));
            }
            set {
                this["CRate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool RampEnable {
            get {
                return ((bool)(this["RampEnable"]));
            }
            set {
                this["RampEnable"] = value;
            }
        }
    }
}