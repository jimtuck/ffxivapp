// FFXIVAPP.Client
// PluginContainer.cs
//  
// Created by Ryan Wilson.
// Copyright � 2007-2013 Ryan Wilson - All Rights Reserved

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FFXIVAPP.Client.Helpers;
using FFXIVAPP.Client.Models;
using FFXIVAPP.Client.Properties;
using FFXIVAPP.Common.Models;
using FFXIVAPP.Common.Utilities;
using FFXIVAPP.IPluginInterface;
using NLog;

#endregion

namespace FFXIVAPP.Client
{
    internal class PluginContainer : IPluginHost
    {
        #region Property Bindings

        private AggregateCatalog _catalog;
        private PluginCollectionHelper _loaded;

        [ImportMany(typeof (IPlugin), AllowRecomposition = true)]
        private List<IPlugin> _plugins;

        public PluginCollectionHelper Loaded
        {
            get { return _loaded ?? (_loaded = new PluginCollectionHelper()); }
        }

        [ImportMany(typeof (IPlugin), AllowRecomposition = true)]
        private List<IPlugin> Plugins
        {
            get { return _plugins ?? (_plugins = new List<IPlugin>()); }
            set
            {
                if (_plugins == null)
                {
                    _plugins = new List<IPlugin>();
                }
                _plugins = value;
            }
        }

        private AggregateCatalog Catalog
        {
            get { return _catalog ?? (_catalog = new AggregateCatalog()); }
            set
            {
                if (_catalog == null)
                {
                    _catalog = new AggregateCatalog();
                }
                _catalog = value;
            }
        }

        #endregion

        #region Declarations

        #endregion

        /// <summary>
        /// </summary>
        /// <param name="path"> </param>
        public void LoadPlugins(string path = "")
        {
            path = (path == "") ? AppDomain.CurrentDomain.BaseDirectory : path;
            UnloadPlugins();
            if (!Directory.Exists(path))
            {
                return;
            }
            var directories = Directory.GetDirectories(path);
            var fileNames = new List<string>();
            foreach (var d in directories)
            {
                var settings = String.Format(@"{0}\PluginInfo.xml", d);
                if (!File.Exists(settings))
                {
                    continue;
                }
                var xDoc = XDocument.Load(settings);
                foreach (var xElement in xDoc.Descendants()
                                             .Elements("Main"))
                {
                    var xKey = (string) xElement.Attribute("Key");
                    var xValue = (string) xElement.Element("Value");
                    if (String.IsNullOrWhiteSpace(xKey) || String.IsNullOrWhiteSpace(xValue))
                    {
                        return;
                    }
                    switch (xKey)
                    {
                        case "FileName":
                            var fileName = String.Format(@"{0}\{1}", d, xValue);
                            fileNames.Add(fileName);
                            Catalog.Catalogs.Add(new DirectoryCatalog(Path.GetDirectoryName(fileName), Path.GetFileName(fileName)));
                            break;
                    }
                }
            }
            var cc = new CompositionContainer(Catalog);
            cc.ComposeParts(this);
            VerifyPlugins(fileNames);
        }

        /// <summary>
        /// </summary>
        public void UnloadPlugins()
        {
            foreach (PluginInstance pInstance in Loaded)
            {
                if (pInstance.Instance != null)
                {
                    pInstance.Instance.Dispose();
                }
                pInstance.Instance = null;
            }
            Catalog.Catalogs.Clear();
            Plugins.Clear();
            Loaded.Clear();
        }

        /// <summary>
        /// </summary>
        private void VerifyPlugins(IReadOnlyList<string> fileNames)
        {
            try
            {
                for (var i = 0; i < Plugins.Count; i++)
                {
                    var pluginInstance = new PluginInstance();
                    pluginInstance.Instance = Plugins[i];
                    pluginInstance.AssemblyPath = fileNames[i];
                    pluginInstance.Instance.Host = this;
                    pluginInstance.Instance.Initialize();
                    Loaded.Add(pluginInstance);
                }
            }
            catch (Exception ex)
            {
                Logging.Log(LogManager.GetCurrentClassLogger(), "", ex);
            }
        }

        #region Implementaion of IPluginHost

        /// <summary>
        /// </summary>
        /// <param name="pluginName"> </param>
        /// <param name="commands"> </param>
        public void Commands(string pluginName, IEnumerable<string> commands)
        {
            var pluginInstance = Loaded.Find(pluginName);
            if (pluginInstance == null)
            {
                return;
            }
            if (!Settings.Default.AllowPluginCommands)
            {
                var enumerable = commands as List<string> ?? commands.ToList();
                var commandlist = enumerable.Aggregate("", (current, s) => current + (s + ","));
                Logging.Log(LogManager.GetCurrentClassLogger(), String.Format("PluginCommandAborted: {0}: \n{1}", pluginName, commandlist.Substring(0, commandlist.Length - 1)));
                return;
            }
            // return for now as all commands are disabled
            return;
            foreach (var command in commands)
            {
                var ascii = Encoding.GetEncoding("utf-16");
                KeyBoardHelper.SendNotify(ascii.GetBytes(command));
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="displayed"> </param>
        /// <param name="content"> </param>
        public void PopupMessage(out bool displayed, object content)
        {
            var popupContent = content as PopupContent;
            var pluginInstance = Loaded.Find(popupContent.PluginName);
            if (pluginInstance == null || ShellView.View.Notify.IsOpen)
            {
                displayed = false;
                return;
            }
            PopupHelper.Toggle(content);
            displayed = true;
            EventHandler onClosed = null;
            onClosed = delegate
            {
                pluginInstance.Instance.PopupResult = PopupHelper.Result;
                PopupHelper.MessagePopup.Closed -= onClosed;
            };
            PopupHelper.MessagePopup.Closed += onClosed;
        }

        #endregion
    }
}
