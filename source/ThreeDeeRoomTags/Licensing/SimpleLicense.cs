using System.Net.Http;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using ThreeDeeRoomTags.Classes;

namespace ThreeDeeRoomTags.Licensing
{
    internal class SimpleLicense
    {
        private static readonly HttpClient client = new HttpClient();

        private static string LicenseURL = "https://api.lemonsqueezy.com/v1/licenses/";
        private static readonly string InstanceName = Environment.UserName;

        internal class LicenseKey
        {
            public string status;
            public string key;
            public int activation_limit;
            public int activation_usage;
        }

        internal class Meta
        {
            public int product_id;
            public string product_name;
        }
        internal class Instance
        {
            public string id;
            public string name;
        }

        //internal license stuff
        public LicenseKey license_key { get; set; }
        public Meta meta { get; set; }
        public Instance instance { get; set; }

        public bool valid { get; set; }
        public bool active { get; set; }
        public string error { get; set; }
        public bool activationLimitMet { get; set; }

        //1901B7AE-6F81-400C-A333-78F0FD193B5E
        public static async Task Activate(string appName)
        {
            //get the key from the registry
            var licenseKey = DtuRegistry.GetKey(appName);
            var values = new Dictionary<string, string>
            {
                { "license_key", licenseKey },
                { "instance_name", InstanceName }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync($"{LicenseURL}activate", content);

            var responseString = await response.Content.ReadAsStringAsync();

            //generate the license object
            Global.SimpleLicense = JsonConvert.DeserializeObject<SimpleLicense>(responseString);

            //set the license params
            if (Global.SimpleLicense.valid)
            {
                Global.SimpleLicense.active = Global.SimpleLicense.license_key.status.Equals("active");
            }
            else
            {
                Global.SimpleLicense.active = false;

                Global.ThreeDeeRoomTagPushButton.ToolTip =
                    "You're license key was not found on the server. Please verify that you have a valid license key and that it was entered correctly on installation. To view your orders,press F1 to navigate there now.";
                ContextualHelp contextualHelp =
                    new ContextualHelp(ContextualHelpType.Url, "https://app.lemonsqueezy.com/my-orders");
                Global.ThreeDeeRoomTagPushButton.SetContextualHelp(contextualHelp);

                return;
            }

            Global.SimpleLicense.activationLimitMet = Global.SimpleLicense.error ==
                "This license key has reached the activation limit.";

            //if the activation limit is good, store the instance id
            if (!Global.SimpleLicense.activationLimitMet)
            {
                Properties.Settings.Default.InstanceId = Global.SimpleLicense.instance.id;
                Properties.Settings.Default.Save();
            }


            //out of activations, add the info to the tooltip
            if (Global.SimpleLicense.instance is null)
            {
                Global.SimpleLicense.active = false;

                Global.ThreeDeeRoomTagPushButton.ToolTip =
                    "You are out of activations. Please visit your account to deactivate some instances. Press F1 to navigate there now.";
                ContextualHelp contextualHelp =
                    new ContextualHelp(ContextualHelpType.Url, "https://app.lemonsqueezy.com/my-orders");
                Global.ThreeDeeRoomTagPushButton.SetContextualHelp(contextualHelp);
            }
        }


        public static async Task Validate(string appName)
        {
            //get the key from the registry
            var licenseKey = DtuRegistry.GetKey(appName);
            var instanceId = Properties.Settings.Default.InstanceId;


       
            var values = new Dictionary<string, string>
            {
                { "license_key", licenseKey },
                { "instance_id", instanceId }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync($"{LicenseURL}validate", content);

            var responseString = await response.Content.ReadAsStringAsync();


            //generate the license object
            Global.SimpleLicense = JsonConvert.DeserializeObject<SimpleLicense>(responseString);

            //set the license params
            if (Global.SimpleLicense.license_key != null)
            {
                Global.SimpleLicense.active = Global.SimpleLicense.license_key.status.Equals("active");
            }
        }

        public static async void Deactivate(string appName)
        {
            //get the key from the registry
            var licenseKey = DtuRegistry.GetKey(appName);

            var values = new Dictionary<string, string>
            {
                { "license_key", licenseKey },
                //{ "instance_id", instanceId }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync($"{LicenseURL}deactivate", content);

            var responseString = await response.Content.ReadAsStringAsync();

        }
        internal static bool NeedsToPhoneHome()
        {
            try
            {
                string lastPhoneHomeString = Utilities.StringUtils.DecryptString(Properties.Settings.Default.LastPhoneHome);
                var lastPhoneHome = DateTime.Parse(lastPhoneHomeString);
                return (DateTime.Now - lastPhoneHome).Days >= 7;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
