using System.Reflection;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Serilog;
using Serilog.Events;
using ThreeDeeRoomTags.Classes;

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
                if (stream is null)
                {
                    return null;
                }

                // Read in a loop rather than trusting one call to fill the buffer: Stream.Read is
                // allowed to return fewer bytes than asked for, and a short read here hands
                // Assembly.Load a truncated image.
                var bytes = new byte[stream.Length];
                var offset = 0;

                while (offset < bytes.Length)
                {
                    var read = stream.Read(bytes, offset, bytes.Length - offset);

                    if (read == 0)
                    {
                        return null;
                    }

                    offset += read;
                }

                return Assembly.Load(bytes);
            }
        }
       
    }
}
