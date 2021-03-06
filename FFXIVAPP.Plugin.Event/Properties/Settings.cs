﻿// FFXIVAPP.Plugin.Event ~ Settings.cs
// 
// Copyright © 2007 - 2016 Ryan Wilson - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Xml.Linq;
using FFXIVAPP.Common.Helpers;
using FFXIVAPP.Common.Models;
using FFXIVAPP.Common.Utilities;
using NLog;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Drawing.FontFamily;

namespace FFXIVAPP.Plugin.Event.Properties
{
    public class Settings : ApplicationSettingsBase, INotifyPropertyChanged
    {
        private static Settings _default;

        public static Settings Default
        {
            get { return _default ?? (_default = ((Settings) (Synchronized(new Settings())))); }
        }

        public override void Save()
        {
            // this call to default settings only ensures we keep the settings we want and delete the ones we don't (old)
            DefaultSettings();
            SaveSettingsNode();
            SaveEventsNode();
            Constants.XSettings.Save(Path.Combine(Common.Constants.PluginsSettingsPath, "FFXIVAPP.Plugin.Event.xml"));
        }

        private void DefaultSettings()
        {
            Constants.Settings.Clear();
            Constants.Settings.Add("GlobalVolume");
        }

        public new void Reset()
        {
            DefaultSettings();
            foreach (var key in Constants.Settings)
            {
                var settingsProperty = Default.Properties[key];
                if (settingsProperty == null)
                {
                    continue;
                }
                var value = settingsProperty.DefaultValue.ToString();
                SetValue(key, value, CultureInfo.InvariantCulture);
            }
        }

        public void SetValue(string key, string value, CultureInfo cultureInfo)
        {
            try
            {
                var type = Default[key].GetType()
                                       .Name;
                switch (type)
                {
                    case "Boolean":
                        Default[key] = Boolean.Parse(value);
                        break;
                    case "Color":
                        var cc = new ColorConverter();
                        var color = cc.ConvertFrom(value);
                        Default[key] = color ?? Colors.Black;
                        break;
                    case "Double":
                        Default[key] = Double.Parse(value, cultureInfo);
                        break;
                    case "Font":
                        var fc = new FontConverter();
                        var font = fc.ConvertFromString(value);
                        Default[key] = font ?? new Font(new FontFamily("Microsoft Sans Serif"), 12);
                        break;
                    case "Int32":
                        Default[key] = Int32.Parse(value, cultureInfo);
                        break;
                    default:
                        Default[key] = value;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.Log(LogManager.GetCurrentClassLogger(), "", ex);
            }
            RaisePropertyChanged(key);
        }

        #region Property Bindings (Settings)

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("#FF000000")]
        public Color ChatBackgroundColor
        {
            get { return ((Color) (this["ChatBackgroundColor"])); }
            set
            {
                this["ChatBackgroundColor"] = value;
                RaisePropertyChanged();
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("#FF800080")]
        public Color TimeStampColor
        {
            get { return ((Color) (this["TimeStampColor"])); }
            set
            {
                this["TimeStampColor"] = value;
                RaisePropertyChanged();
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("Microsoft Sans Serif, 12pt")]
        public Font ChatFont
        {
            get { return ((Font) (this["ChatFont"])); }
            set
            {
                this["ChatFont"] = value;
                RaisePropertyChanged();
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("100")]
        public Double Zoom
        {
            get { return ((Double) (this["Zoom"])); }
            set
            {
                this["Zoom"] = value;
                RaisePropertyChanged();
            }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("1")]
        public Double GlobalVolume
        {
            get { return ((Double) (this["GlobalVolume"])); }
            set
            {
                this["GlobalVolume"] = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Implementation of INotifyPropertyChanged

        public new event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion

        #region Iterative Settings Saving

        private void SaveSettingsNode()
        {
            if (Constants.XSettings == null)
            {
                return;
            }
            var xElements = Constants.XSettings.Descendants()
                                     .Elements("Setting");
            var enumerable = xElements as XElement[] ?? xElements.ToArray();
            foreach (var setting in Constants.Settings)
            {
                var element = enumerable.FirstOrDefault(e => e.Attribute("Key")
                                                              .Value == setting);
                var xKey = setting;
                if (Default[xKey] == null)
                {
                    continue;
                }
                if (element == null)
                {
                    var xValue = Default[xKey].ToString();
                    var keyPairList = new List<XValuePair>
                    {
                        new XValuePair
                        {
                            Key = "Value",
                            Value = xValue
                        }
                    };
                    XmlHelper.SaveXmlNode(Constants.XSettings, "Settings", "Setting", xKey, keyPairList);
                }
                else
                {
                    var xElement = element.Element("Value");
                    if (xElement != null)
                    {
                        xElement.Value = Default[setting].ToString();
                    }
                }
            }
        }

        private void SaveEventsNode()
        {
            if (Constants.XSettings == null)
            {
                return;
            }
            Constants.XSettings.Descendants("Event")
                     .Where(node => PluginViewModel.Instance.Events.All(e => e.Key.ToString() != node.Attribute("Key")
                                                                                                     .Value))
                     .Remove();
            var xElements = Constants.XSettings.Descendants()
                                     .Elements("Event");
            var enumerable = xElements as XElement[] ?? xElements.ToArray();

            foreach (var item in PluginViewModel.Instance.Events)
            {
                var xKey = (item.Key != Guid.Empty ? item.Key : Guid.NewGuid()).ToString();
                var xRegEx = item.RegEx;
                var xSound = item.Sound;
                var xTTS = item.TTS;
                var xRate = item.Rate;
                var xVolume = item.Volume;
                var xDelay = item.Delay;
                var xCategory = item.Category;
                var xEnabled = item.Enabled;
                var xExecutable = item.Executable;
                var xArguments = item.Arguments;
                var keyPairList = new List<XValuePair>
                {
                    new XValuePair
                    {
                        Key = "RegEx",
                        Value = xRegEx
                    },
                    new XValuePair
                    {
                        Key = "Sound",
                        Value = xSound
                    },
                    new XValuePair
                    {
                        Key = "TTS",
                        Value = xTTS
                    },
                    new XValuePair
                    {
                        Key = "Rate",
                        Value = xRate.ToString(CultureInfo.InvariantCulture)
                    },
                    new XValuePair
                    {
                        Key = "Volume",
                        Value = xVolume.ToString(CultureInfo.InvariantCulture)
                    },
                    new XValuePair
                    {
                        Key = "Delay",
                        Value = xDelay.ToString(CultureInfo.InvariantCulture)
                    },
                    new XValuePair
                    {
                        Key = "Category",
                        Value = xCategory
                    },
                    new XValuePair
                    {
                        Key = "Enabled",
                        Value = xEnabled.ToString()
                    },
                    new XValuePair
                    {
                        Key = "Executable",
                        Value = xExecutable
                    },
                    new XValuePair
                    {
                        Key = "Arguments",
                        Value = xArguments
                    }
                };
                var element = enumerable.FirstOrDefault(e => e.Attribute("Key")
                                                              .Value == xKey);
                if (element == null)
                {
                    XmlHelper.SaveXmlNode(Constants.XSettings, "Settings", "Event", xKey, keyPairList);
                }
                else
                {
                    element.SetAttributeValue("Key", xKey);

                    foreach (var kv in keyPairList)
                    {
                        var childElement = element.Element(kv.Key);
                        if (childElement == null)
                        {
                            childElement = new XElement(kv.Key);
                            element.Add(childElement);
                        }
                        childElement.SetValue(kv.Value);
                    }
                }
            }
        }

        #endregion
    }
}
