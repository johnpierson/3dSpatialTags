using System.Reflection;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Serilog;
using Serilog.Events;
using ThreeDeeRoomTags.Classes;
using ThreeDeeRoomTags.Licensing;

namespace ThreeDeeRoomTags
{
    public class AppCommand : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            application.ControlledApplication.ApplicationInitialized += ControlledApplicationOnApplicationInitialized;

            // Attach custom event handler
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;

            //get the revit version
            Global.RevitVersion = application.ControlledApplication.VersionNumber;

            //CreateLogger();

            CreateRisePanel(application);


            return Result.Succeeded;
        }

        private void ControlledApplicationOnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            Autodesk.Revit.ApplicationServices.Application app = sender as Autodesk.Revit.ApplicationServices.Application;


            CheckLicense();
        }



        private async void CheckLicense()
        {
            //see if it needs to phone home
            bool check = SimpleLicense.NeedsToPhoneHome();

#if DEBUG
            check = false;
#endif

            if (check)
            {
                try
                {
                    //check if the license is valid
                    await SimpleLicense.Validate("ThreeDeeRoomTags");


                    //if it is not active try to activate it
                    if (!Global.SimpleLicense.active)
                    {
                        await SimpleLicense.Activate("ThreeDeeRoomTags");
                    }
                }
                catch (Exception exception)
                {
                    string whatHappen = exception.Message;
                }


                //check if the product ids are matching.
                if (Global.SimpleLicense != null && Global.SimpleLicense.valid)
                {
                    bool matchingId = Global.SimpleLicense.meta.product_id == Global.ProductId;
                    Global.SimpleLicense.active = matchingId;

                    //if the id does not match, alert the user.
                    if (!matchingId)
                    {
                        Global.ThreeDeeRoomTagPushButton.ToolTip =
                            "Hi. Your product id does not match the license key you entered. Please confirm on your account that you entered the correct product key. Press F1 to navigate there now.";
                        ContextualHelp contextualHelp =
                            new ContextualHelp(ContextualHelpType.Url, "https://app.lemonsqueezy.com/my-orders");
                        Global.ThreeDeeRoomTagPushButton.SetContextualHelp(contextualHelp);
                    }
                }

                Global.ThreeDeeRoomTagPushButton.Enabled = Global.SimpleLicense.active;

                if (Global.SimpleLicense.active)
                {
                    //set the last phone home since the license check was successful
                    Properties.Settings.Default.LastPhoneHome = Utilities.StringUtils.EncryptString(DateTime.Now.ToShortDateString());
                    Properties.Settings.Default.Save();
                }
            }
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        internal void CreateRisePanel(UIControlledApplication app)
        {
            RibbonPanel ribbonPanel = app.GetRibbonPanels().FirstOrDefault(r => r.Name.Equals(Global.PanelName)) ??
                                      app.CreateRibbonPanel(Global.PanelName);

            //create the code compliance button
            ThreeDeeRoomTagButton.ThreeDeeRoomTagCommand.CreateButton(ribbonPanel);
        }
        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Get assembly name
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";

            // Get resource name
            var resourceName = Global.EmbeddedLibraries.FirstOrDefault(x => x.EndsWith(assemblyName));
            if (resourceName == null)
            {
                return null;
            }

            // Load assembly from resource
            using (var stream = Global.ExecutingAssembly.GetManifestResourceStream(resourceName))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return Assembly.Load(bytes);
            }
        }
        private static void CreateLogger()
        {
            const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Global.LogFile,LogEventLevel.Debug, outputTemplate)
                .MinimumLevel.Debug()
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                Log.Fatal(exception, "Domain unhandled exception");
            };
        }
    }
}
