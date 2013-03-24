// /*===========================================================
//    Copyright 2013 Robbert Hock
//  
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//  
// ============================================================*/

using System;
using Sitecore.License.Expiration.Module.FixedPaths.System.Modules.SitecoreLicenseExpirationModule;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Sitecore.Reflection;
using Sitecore.Web;

namespace Sitecore.License.Expiration.Module
{
    public class SitecoreLicenseExpirationGetContentEditorWarningProcessor
    {
        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        public void Process([NotNull] GetContentEditorWarningsArgs args)
        {
            DateTime expiration = DateUtil.IsoDateToDateTime(Nexus.LicenseApi.Expiration);
            double minimumNumberOfDaysToWarn = 7;
            double.TryParse(SettingsFixed.SettingsFromMaster.DefaultNumberOfDaysToWarn.ToString(), out minimumNumberOfDaysToWarn);
            if (MainUtil.GetBool(SettingsFixed.SettingsFromMaster.AlwaysWarn, false) || DateTime.Now.Date > expiration.AddDays(-minimumNumberOfDaysToWarn))
            {
                GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
                warning.Title = SettingsFixed.SettingsFromMaster.WarningTitle;
                warning.Text = SettingsFixed.SettingsFromMaster.WarningSubtitle.Replace("[date]", expiration.ToLongDateString()).Replace("[url]", WebUtil.GetServerUrl());
                warning.Icon = "Applications/16x16/delete.png";
            }
        }
    }
}