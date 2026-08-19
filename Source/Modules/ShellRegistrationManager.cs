using Microsoft.Win32;
using System.Windows.Forms;
using System.Windows.Input;

namespace PaperCounter.Utility
{
    public enum RegState
    {
        Disabled,
        Corrupted,
        Enabled
    }
    public class ShellRegistrationManager
    {
        private readonly string appExe;
        private readonly string appDir;
        //private const string serverGuid = "{FFA07888-75BD-471A-B325-59274E73400A}";
        private const string serverGuid = "{DC25FEE6-4DE1-4C42-9461-22F1A88CF138}";

        public ShellRegistrationManager(string assemblyPath)
        {
            appExe = System.IO.Path.ChangeExtension( assemblyPath,"exe");
            appDir = System.IO.Path.GetDirectoryName(assemblyPath);
        }

        /// <summary>
        /// Проверка регистрации для PDF файлов
        /// </summary>
        public bool ForceServerRegistration()
        {
            try
            {
                object currentValue;
                string targetValue;

                string keyPath = $@"SOFTWARE\Classes\CLSID\{serverGuid}";
                using (RegistryKey clsidKey = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (clsidKey == null)
                        return false;

                    currentValue = clsidKey.GetValue("AppId");
                    if (currentValue == null || currentValue.ToString() != serverGuid)
                        clsidKey.SetValue("AppId", serverGuid);

                    targetValue = "PaperCounter Context Menu Verb";
                    currentValue = clsidKey.GetValue("");
                    if (currentValue == null || currentValue.ToString() != targetValue)
                        clsidKey.SetValue("", targetValue);
                }

                keyPath = $@"SOFTWARE\Classes\CLSID\{serverGuid}\LocalServer32";
                using (RegistryKey localServerKey = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (localServerKey == null)
                        return false;

                    targetValue = $"{appDir}\\COM.Server.exe ah # {appExe} #f";
                    currentValue = localServerKey.GetValue("");
                    if (currentValue == null || currentValue.ToString() != targetValue)
                        localServerKey.SetValue("", targetValue);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Проверка регистрации для определенных расширений файлов
        /// </summary>
        public RegState IsRegisteredForExts(params string[] exts)
        {
            foreach (var ext in exts)
            {
                if (Registry.CurrentUser.OpenSubKey(@"Software\Classes\SystemFileAssociations\" + ext + @"\shell\CountPages") == null)
                    return RegState.Disabled;
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\SystemFileAssociations\" + ext + @"\shell\CountPages\command"))
                    if (key == null || key.GetValue("DelegateExecute")?.ToString() != serverGuid)
                        return RegState.Corrupted;
            }

            return RegState.Enabled;
        }

        /// <summary>
        /// Проверка регистрации для папок и фона папки
        /// </summary>
        public RegState IsRegisteredForFolders
        {
            get
            {
                using (var key1 = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Directory\shell\CountPages"))
                using (var key2 = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Directory\Background\shell\CountPages"))
                    if (key1 == null || key2 == null) return RegState.Disabled;

                using (var key1 = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Directory\shell\CountPages\command"))
                using (var key2 = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Directory\Background\shell\CountPages\command"))
                    if (key1 == null || key1.GetValue("DelegateExecute")?.ToString() != serverGuid
                         || key2 == null || key2.GetValue("DelegateExecute")?.ToString() != serverGuid) return RegState.Corrupted;

                return RegState.Enabled;
            }
        }

        /// <summary>
        /// Изменение состояния регистрации для определенных расширений файлов
        /// </summary>
        public void RegisterForExts(bool state,params string[] exts)
        {
            foreach (var ext in exts)
            {
                string path = @"Software\Classes\SystemFileAssociations\" + ext + @"\shell\CountPages";

                if (state)
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(path))
                    {
                        key.SetValue("", $"Подсчёт листов PDF");
                        key.SetValue("Icon", appExe + ",0");
                    }
                    using (var key = Registry.CurrentUser.CreateSubKey(path + @"\command"))
                        key.SetValue("DelegateExecute", serverGuid);
                }
                else
                {
                    Registry.CurrentUser.DeleteSubKeyTree(path, false);
                }
            }
        }

        /// <summary>
        /// Добавление или удаление пункта для папок и фона
        /// </summary>
        public void RegisterForFolders(bool state)
        {
            string folderPath = @"Software\Classes\Directory\shell\CountPages";
            string backgroundPath = @"Software\Classes\Directory\Background\shell\CountPages";

            if (state)
            {
                using (var key = Registry.CurrentUser.CreateSubKey(folderPath))
                {
                    key.SetValue("", $"Подсчёт листов PDF");
                    key.SetValue("Icon", appExe + ",0");
                }
                using (var key = Registry.CurrentUser.CreateSubKey(folderPath + @"\command"))
                    key.SetValue("DelegateExecute", serverGuid);

                using (var key = Registry.CurrentUser.CreateSubKey(backgroundPath))
                {
                    key.SetValue("", $"Подсчёт листов PDF");
                    key.SetValue("Icon", appExe + ",0");
                }
                using (var key = Registry.CurrentUser.CreateSubKey(backgroundPath + @"\command"))
                    key.SetValue("DelegateExecute", serverGuid);
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(folderPath, false);
                Registry.CurrentUser.DeleteSubKeyTree(backgroundPath, false);
            }
        }

    }
}
