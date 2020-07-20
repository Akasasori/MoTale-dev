/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System.Collections.Concurrent;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace OpenNos.Core
{
    public class Language
    {
        #region Members

        private static Language _instance;

        private readonly CultureInfo _resourceCulture;

        private readonly ResourceManager _manager;

        private readonly StreamWriter _streamWriter;

        private ConcurrentDictionary<string, string> _language = new ConcurrentDictionary<string, string>();

        #endregion

        #region Instantiation

        private Language()
        {
            try
            {
                _streamWriter = new StreamWriter("MissingLanguage.txt", true)
                {
                    AutoFlush = true
                };
            }
            catch
            {
            }

            _resourceCulture = new CultureInfo(ConfigurationManager.AppSettings[nameof(Language)]);

            if (Assembly.GetEntryAssembly() != null)
            {
                _manager = new ResourceManager(Assembly.GetEntryAssembly().GetName().Name + ".Resource.LocalizedResources", Assembly.GetEntryAssembly());
            }
        }

        #endregion

        #region Properties

        public static Language Instance => _instance ?? (_instance = new Language());

        #endregion

        #region Methods

        public string GetMessageFromKey(string key)
        {
            return _language.GetOrAdd(key, name =>
            {
                string value = _manager?.GetString(name, _resourceCulture);

                if (string.IsNullOrEmpty(value))
                {
                    _streamWriter?.WriteLine(name);
                   return "none";
                }

                return value;
            });
        }

#endregion
    }
}