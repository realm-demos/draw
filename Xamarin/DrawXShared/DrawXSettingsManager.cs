﻿////////////////////////////////////////////////////////////////////////////
//
// Copyright 2016 Realm Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Linq;
using Realms;
using Realms.Exceptions;
using Realms.Sync;

namespace DrawXShared
{
    internal static class DrawXSettingsManager
    {
        private static Realm _localSettingsRealm;
        private static DrawXSettings _savedSettings;
        internal static User LoggedInUser
        {
            get
            {
                // hides the exception handling of User.Current
                try
                {
                    return User.Current;
                }
                catch (RealmException)
                {
                    // do nothing - assume multiple users logged in so will have to use login dialog to choose
                    // TODO - present a Currently Logged in picker in this case?
                    return null;
                }
            }
        }

        public static DrawXSettings Settings
        {
            get
            {
                if (_savedSettings == null)
                {
                    _savedSettings = _localSettingsRealm.All<DrawXSettings>().SingleOrDefault();
                }

                if (_savedSettings == null)
                {
                    Write(() =>
                    {
                        _savedSettings = _localSettingsRealm.Add(new DrawXSettings());
                    });
                }

                return _savedSettings;
            }
        }

        internal static void InitLocalSettings()
        {
            var settingsConf = new RealmConfiguration("DrawXsettings.realm");
            settingsConf.ObjectClasses = new[] { typeof(DrawXSettings) };
            settingsConf.SchemaVersion = 2;  // set explicitly and bump as we add setting properties
            _localSettingsRealm = Realm.GetInstance(settingsConf);
        }

        // bit of a hack which only works when the caller has objects already on the _realmLocalSettings Realm
        internal static void Write(Action writer)
        {
            _localSettingsRealm.Write(writer);
        }

        internal static bool UpdateCredentials(string serverIP, string username, string password)
        {
            if (!serverIP.Contains(":"))
            {
                // assume they forgot port so add standard port
                serverIP += ":9080";
            }

            var changedServer = _savedSettings.ServerIP == null ||
              !_savedSettings.ServerIP.Equals(serverIP) ||
              !_savedSettings.Username.Equals(username) ||
              !_savedSettings.Password.Equals(password);
            _localSettingsRealm.Write(() =>
            {
                _savedSettings.ServerIP = serverIP;
                _savedSettings.Username = username;
                _savedSettings.Password = password;
            });
            return changedServer;
        }

        internal static bool HasCredentials()
        {
            return _savedSettings != null &&
                !string.IsNullOrEmpty(_savedSettings.Username) &&
                !string.IsNullOrEmpty(_savedSettings.Password) &&
                !string.IsNullOrEmpty(_savedSettings.ServerIP);
        }
    }
}
