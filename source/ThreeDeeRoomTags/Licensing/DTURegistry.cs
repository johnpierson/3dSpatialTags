using Microsoft.Win32;

namespace ThreeDeeRoomTags.Licensing
{
    internal class DtuRegistry
    {
        internal static string GetKey(string appName)
        {
            using (RegistryKey key =
                   Registry.LocalMachine.OpenSubKey($"Software\\Wow6432Node\\DTU\\{appName}\\Settings"))
            {
                return key.GetValue("License").ToString();
            }
        }
        internal static string GetInstanceId(string appName)
        {
            using (RegistryKey key =
                   Registry.LocalMachine.OpenSubKey($"Software\\Wow6432Node\\DTU\\{appName}\\Settings"))
            {
                return key.GetValue("InstanceId").ToString();
            }
        }
        internal static void StoreInstanceId(string appName, string instanceId)
        {

            using (RegistryKey key =
                   Registry.LocalMachine.OpenSubKey($"Software\\Wow6432Node\\DTU\\{appName}\\Settings"))
            {
                key.SetValue("InstanceId", instanceId);

            }
        }
    }
}
