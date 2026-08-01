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
            //get the revit version
            Global.RevitVersion = application.ControlledApplication.VersionNumber;

            CreateLogger();

            Log.Information("3d Spatial Tags {Version} starting on Revit {RevitVersion}",
                Global.Version, Global.RevitVersion);

            CreateRisePanel(application);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Log.CloseAndFlush();

            return Result.Succeeded;
        }

        /// <summary>
        /// Sets up the log this add-in writes when something goes wrong.
        ///
        /// There was no logging at all. Three Serilog assemblies shipped in every build, the
        /// only call to configure them was commented out, and seven catch blocks discarded
        /// whatever they caught — so a user reporting "it says tags could not be created" left
        /// nothing behind to work from: not the exception, not the Revit version, not the
        /// document.
        ///
        /// Warnings and above only, rolling daily, capped at a megabyte a file and a week of
        /// them, so an add-in nobody is having trouble with writes almost nothing and one that
        /// is cannot fill a disk. Nothing more identifying than a document title is recorded.
        /// </summary>
        private static void CreateLogger()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(
                        Global.LogFile,
                        restrictedToMinimumLevel: LogEventLevel.Warning,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        fileSizeLimitBytes: 1_000_000,
                        rollOnFileSizeLimit: true,
                        shared: true)
                    .WriteTo.Debug()
                    .CreateLogger();
            }
            catch (Exception)
            {
                // A log that cannot be opened must not stop the add-in loading. Serilog's
                // silent logger keeps every Log.* call downstream harmless.
                Log.Logger = Serilog.Core.Logger.None;
            }
        }

        internal void CreateRisePanel(UIControlledApplication app)
        {
            RibbonPanel ribbonPanel = app.GetRibbonPanels().FirstOrDefault(r => r.Name.Equals(Global.PanelName)) ??
                                      app.CreateRibbonPanel(Global.PanelName);

            //create the code compliance button
            ThreeDeeRoomTagButton.ThreeDeeRoomTagCommand.CreateButton(ribbonPanel);
        }
    }
}
