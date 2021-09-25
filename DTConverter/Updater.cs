using System;
using System.Linq;
using System.Configuration;
using System.Net.Http;
using System.ComponentModel;

namespace DTConverter
{
    public enum CheckUpdateFrequencies { Daily, Weekly, Monthly };

    public class Updater :  INotifyPropertyChanged
    {
        /// <summary>
        /// This constructor is used only for bindings. A Configuration must be passed in parameter.
        /// </summary>
        public Updater()
        { }

        public Updater(Configuration configurationFile)
        {
            Config = configurationFile;
        }

        private const string BetaReleasesKey = "BetaReleases";
        private const string CheckUpdateFrequencyKey = "CheckUpdateFrequency";
        private const string LastCheckUpdateKey = "LastCheckUpdate";
        private const string UpdateAvailableKey = "UpdateAvailable";
        private const string AvailableVersionKey = "AvailableVersion";
        public Configuration Config;
        HttpClient client;

        public bool BetaReleases
        {
            get
            {
                // Default value
                bool returnValue = false;
                
                if (Config != null)
                {
                    try
                    {
                        string strBetaReleases = Config.AppSettings.Settings[BetaReleasesKey].Value;
                        return bool.Parse(strBetaReleases);
                    }
                    catch (Exception E)
                    {
                        BetaReleases = returnValue;
                    }
                }
                return returnValue;
            }
            set
            {
                if (Config != null)
                {
                    if (Config.AppSettings.Settings.AllKeys.Contains(BetaReleasesKey))
                    {
                        Config.AppSettings.Settings[BetaReleasesKey].Value = value.ToString();
                    }
                    else
                    {
                        Config.AppSettings.Settings.Add(BetaReleasesKey, value.ToString());
                    }
                }
                OnPropertyChanged("BetaReleases");
            }
        }

        public CheckUpdateFrequencies CheckUpdateFrequency
        {
            get
            {
                // Default value
                CheckUpdateFrequencies returnValue = CheckUpdateFrequencies.Weekly;
                if (Config != null)
                {
                    try
                    {
                        string strCheckUpdateFrequency = Config.AppSettings.Settings[CheckUpdateFrequencyKey].Value;
                        return (CheckUpdateFrequencies) Enum.Parse(typeof(CheckUpdateFrequencies), strCheckUpdateFrequency);
                    }
                    catch (Exception E)
                    {
                        CheckUpdateFrequency = returnValue;
                    }
                }
                return returnValue;
            }
            set
            {
                if (Config != null)
                {
                    if (Config.AppSettings.Settings.AllKeys.Contains(CheckUpdateFrequencyKey))
                    {
                        Config.AppSettings.Settings[CheckUpdateFrequencyKey].Value = value.ToString();
                    }
                    else
                    {
                        Config.AppSettings.Settings.Add(CheckUpdateFrequencyKey, value.ToString());
                    }
                }
                OnPropertyChanged("CheckUpdateFrequency");
            }
        }
        public bool IsCheckUpdateFrequencyDaily
        {
            get => CheckUpdateFrequency == CheckUpdateFrequencies.Daily;
            set
            {
                CheckUpdateFrequency = CheckUpdateFrequencies.Daily;
                OnPropertyChanged("IsCheckUpdateFrequencyDaily");
                OnPropertyChanged("IsCheckUpdateFrequencyWeekly");
                OnPropertyChanged("IsCheckUpdateFrequencyMonthly");
            }
        }
        public bool IsCheckUpdateFrequencyWeekly
        {
            get => CheckUpdateFrequency == CheckUpdateFrequencies.Weekly;
            set
            {
                CheckUpdateFrequency = CheckUpdateFrequencies.Weekly;
                OnPropertyChanged("IsCheckUpdateFrequencyDaily");
                OnPropertyChanged("IsCheckUpdateFrequencyWeekly");
                OnPropertyChanged("IsCheckUpdateFrequencyMonthly");
            }
        }
        public bool IsCheckUpdateFrequencyMonthly
        {
            get => CheckUpdateFrequency == CheckUpdateFrequencies.Monthly;
            set
            {
                CheckUpdateFrequency = CheckUpdateFrequencies.Monthly;
                OnPropertyChanged("IsCheckUpdateFrequencyDaily");
                OnPropertyChanged("IsCheckUpdateFrequencyWeekly");
                OnPropertyChanged("IsCheckUpdateFrequencyMonthly");
            }
        }

        public DateTime LastCheckUpdate
        {
            get
            {
                // Default value
                DateTime returnValue = DateTime.MinValue;
                if (Config != null)
                {
                    try
                    {
                        string strLastCheckUpdate = Config.AppSettings.Settings[LastCheckUpdateKey].Value;
                        return DateTime.Parse(strLastCheckUpdate);
                    }
                    catch (Exception E)
                    {
                        LastCheckUpdate = returnValue;
                    }
                }
                return returnValue;
            }
            set
            {
                if (Config != null)
                {
                    if (Config.AppSettings.Settings.AllKeys.Contains(LastCheckUpdateKey))
                    {
                        Config.AppSettings.Settings[LastCheckUpdateKey].Value = value.ToShortDateString();
                    }
                    else
                    {
                        Config.AppSettings.Settings.Add(LastCheckUpdateKey, value.ToShortDateString());
                    }
                }
                OnPropertyChanged("LastCheckUpdate");
            }
        }

        public bool UpdateAvailable
        {
            get
            {
                // Default value
                bool returnValue = false;
                if (Config != null)
                {
                    try
                    {
                        string strUpdateAvailableKey = Config.AppSettings.Settings[UpdateAvailableKey].Value;
                        return bool.Parse(strUpdateAvailableKey);
                    }
                    catch (Exception E)
                    {
                        UpdateAvailable = returnValue;
                    }
                }
                return returnValue;
            }
            set
            {
                if (Config != null)
                {
                    if (Config.AppSettings.Settings.AllKeys.Contains(UpdateAvailableKey))
                    {
                        Config.AppSettings.Settings[UpdateAvailableKey].Value = value.ToString();
                    }
                    else
                    {
                        Config.AppSettings.Settings.Add(UpdateAvailableKey, value.ToString());
                    }
                }
                OnPropertyChanged("UpdateAvailable");
            }
        }

        public Version AvailableVersion
        {
            get
            {
                // Default value
                Version returnValue = new Version("0.0.0.0");
                if (Config != null)
                {
                    try
                    {
                        string strAvailableVersion = Config.AppSettings.Settings[AvailableVersionKey].Value;
                        return Version.Parse(strAvailableVersion);
                    }
                    catch (Exception E)
                    {
                        AvailableVersion = returnValue;
                    }
                }
                return returnValue;
            }
            set
            {
                if (Config != null)
                {
                    if (Config.AppSettings.Settings.AllKeys.Contains(AvailableVersionKey))
                    {
                        Config.AppSettings.Settings[AvailableVersionKey].Value = value.ToString();
                    }
                    else
                    {
                        Config.AppSettings.Settings.Add(AvailableVersionKey, value.ToString());
                    }
                }
                OnPropertyChanged("AvailableVersion");
                OnPropertyChanged("AvailableVersionUri");
            }
        }

        public Uri AvailableVersionUri
        {
            get
            {
                return new Uri("https://github.com/daniznf/DTConverter/releases/tag/v" + AvailableVersion.ToString(3));
            }
        }

        public async void CheckUpdate(Action<string, bool> outMessages)
        {
            try
            {
                Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                Version comparingVersion = Version.Parse("0.0.0.0");
                bool preRelease;

                string responseBody;
                client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("DTConverter", currentVersion.ToString(3)));
                HttpResponseMessage response;

#if DEBUG
                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
#else
                response = await client.GetAsync("repos/daniznf/DTConverter/releases");
#endif

                if (response.IsSuccessStatusCode)
                {
#if DEBUG
                    currentVersion = new Version("0.10.0.0");
                    responseBody = "[{\"url\":\"https://api.example.com/url1\"," +
                        "\"many_tags\":\"values\"," +
                        "\"tag_name\":\"v0.32.26\"," +
                        "\"draft\":false," +
                        "\"prerelease\":true," +
                        "\"other_tags\":\"other_values}," +
                        "{\"url\":\"https://api.example.com/url2\"," +
                        "\"many_tags\":\"values\"," +
                        "\"tag_name\":\"v0.27.16\"," +
                        "\"draft\":false," +
                        "\"prerelease\":false," +
                        "\"other_tags\":\"other_values},\"" +
                        "{\"url\":\"https://api.example.com/url3\"," +
                        "\"many_tags\":\"values\"," +
                        "\"tag_name\":\"v0.25.71\"," +
                        "\"draft\":false," +
                        "\"prerelease\":true," +
                        "\"other_tags\":\"other_values}]\"";
#else
                    responseBody = await response.Content.ReadAsStringAsync();
#endif

                    string[] responseSplitted;
                    // this is not really a compliant way to handle json data, but does the job
                    responseSplitted = responseBody.Split(',');
                    foreach (string line in responseSplitted)
                    {
                        if (line != null)
                        {
                            if (line.Contains("\"tag_name\":"))
                            {
                                string[] lineSplitted;
                                lineSplitted = line.Split(':');
                                if (lineSplitted.Length > 0)
                                {
                                    string versionRead;
                                    versionRead = lineSplitted[1].Replace("\"", "").Trim();
                                    versionRead = versionRead.Replace("v", "");
                                    if (Version.TryParse(versionRead, out comparingVersion))
                                    {
                                        // Reset preRelease value, that will be parsed later
                                        preRelease = false;
                                    }
                                }
                            }
                            else if (line.Contains("\"prerelease\":"))
                            {
                                string[] lineSplitted;
                                lineSplitted = line.Split(':');
                                if (lineSplitted.Length > 0)
                                {
                                    string prereleaseRead;
                                    prereleaseRead = lineSplitted[1].Replace("\"", "").Trim();
                                    if (bool.TryParse(prereleaseRead, out preRelease))
                                    {
                                        if ((comparingVersion > currentVersion && BetaReleases) ||
                                            (comparingVersion > currentVersion && !preRelease))
                                        {
                                            AvailableVersion = comparingVersion;
                                            UpdateAvailable = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    LastCheckUpdate = DateTime.Now;
                }
            }
            catch (Exception E)
            {
                outMessages(E.Message, true);
            }
        }

        // This implements INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
