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
using System.Net.Mail;
using Sitecore.Diagnostics;
using Sitecore.License.Expiration.Module.FixedPaths.System.Modules.SitecoreLicenseExpirationModule;
using Sitecore.Reflection;
using Sitecore.Web;

namespace Sitecore.License.Expiration.Module
{
    public class SitecoreLicenseExpirationAgent
    {
        private readonly string _url;
        public SitecoreLicenseExpirationAgent(string url)
        {
            _url = url;
        }

        /// <summary>
        /// Runs this SitecoreLicenseExpirationAgent.
        /// </summary>
        public void Run()
        {
            Log.Info(string.Format("SitecoreLicenseExpirationAgent runs for Site: {0}", _url), this);
            DateTime expiration = DateUtil.IsoDateToDateTime(Nexus.LicenseApi.Expiration);
            double minimumNumberOfDaysToWarn = 7;
            double.TryParse(SettingsFixed.SettingsFromMaster.DefaultNumberOfDaysToWarn.ToString(), out minimumNumberOfDaysToWarn);
            if (DateTime.Now.Date > expiration.AddDays(-minimumNumberOfDaysToWarn) && !MainUtil.GetBool(SettingsFixed.SettingsFromMaster.DisableMail, false))
            {
                SendEmail();
            }
        }

        /// <summary>
        /// Sends the email.
        /// </summary>
        private void SendEmail()
        {
            try
            {
                var message = new MailMessage(SettingsFixed.SettingsFromMaster.MailFrom, SettingsFixed.SettingsFromMaster.MailTo)
                    {
                        Subject = SettingsFixed.SettingsFromMaster.MailSubject.Replace("[date]", DateUtil.IsoDateToDateTime(Nexus.LicenseApi.Expiration).ToLongDateString()).Replace("[url]", _url),
                        Body = SettingsFixed.SettingsFromMaster.MailContent.Replace("[date]", DateUtil.IsoDateToDateTime(Nexus.LicenseApi.Expiration).ToLongDateString()).Replace("[url]", _url),
                        IsBodyHtml = true
                    };

                MainUtil.SendMail(message);
            }
            catch (Exception exception)
            {
                Log.Error("SitecoreLicenseExpirationAgent failure", exception, this);
            }
        }
    }
}