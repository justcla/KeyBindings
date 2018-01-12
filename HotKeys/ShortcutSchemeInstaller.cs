using HotKeys;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HotKeys
{
    class ShortcutSchemeInstaller
    {

        private Package package;
        //private readonly string SublimeVSKFileSrc = "SublimeWithCSharp.vsk";
        //private readonly string SublimeVSKFileDest = "Sublime with C#.*";

        private readonly string MappingSchemeFolder = "MappingSchemes";

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        //private static ShortcutSchemeInstaller instance;

        //private ShortcutSchemeInstaller() { }

        //public static ShortcutSchemeInstaller Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            instance = new ShortcutSchemeInstaller();
        //        }
        //        return instance;
        //    }
        //}

        //public static void Initialize(Package pagkage)
        //{
        //    Instance.package = pagkage;
        //}

        public ShortcutSchemeInstaller(Package package)
        {
            this.package = package;
        }

        public void InstallVSKs()
        {
            try
            {
                //if (!IsVskInstalled(EmacsVskFile))
                {
                    //string installPath = Path.Combine(GetExtensionFolder(), SublimeVSKFileSrc);

                    //var dlg = new InstallVSKConfirmDialog();
                    //dlg.StartPosition = FormStartPosition.CenterScreen;
    
                    // get an ok from the user first
                    if (ConfirmInstallVSKs())
                    {
                        CopyVskUsingXCopy();
                    }
                }
            }
            catch (Exception e)
            {
                var message = e.Message;
                MessageBox.Show("Error occurred while installing keyboard mapping schemes:\n\n" + message);
            }
        }

        private static bool ConfirmInstallVSKs()
        {
            const string title = "Install Keyboard Mapping Schemes";
            const string message =
                "To install the keyboard mapping scheme files, you will need administrator permission.\n" +
                "Click OK to continue, then approve the administrator prompt.";
            return MessageBox.Show(message, title, MessageBoxButtons.OKCancel) == DialogResult.OK;
        }

        string GetVSKSourceFolder()
        {
            return Path.Combine(GetExtensionFolder(), MappingSchemeFolder);
        }

        private static string GetExtensionFolder()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        internal bool IsVskInstalled(string vskFilename)
        {
            return File.Exists(GetVSFilePath(vskFilename));
        }

        private string GetVSFilePath(string vskFilename)
        {
            return Path.Combine(VSInstallationPath, vskFilename);
        }

        internal string VSInstallationPath
        {
            get { return GetVsInstallPath(); }
        }

        string GetVsInstallPath()
        {
            var reg = ServiceProvider.GetService(typeof(SLocalRegistry)) as ILocalRegistry2;

            string root = null;
            reg.GetLocalRegistryRoot(out root);

            using (var key = Registry.LocalMachine.OpenSubKey(root))
            {
                var installDir = key.GetValue("InstallDir") as string;

                return Path.GetDirectoryName(installDir);
            }
        }

        private void CopyVskUsingXCopy()
        {
            var scriptPath = Path.Combine(GetExtensionFolder(), "InstallKeyboardSchemes.bat");
            var vskSrcPath = GetVSKSourceFolder();
            var vskDestPath = VSInstallationPath;

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = scriptPath;
            process.StartInfo.Arguments = $@"""{vskSrcPath}"" ""{vskDestPath}""";
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major > 5)
            {
                process.StartInfo.Verb = "runas";
            }
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
        }

        bool IsAdministrator
        {
            get
            {
                WindowsIdentity wi = WindowsIdentity.GetCurrent();
                WindowsPrincipal wp = new WindowsPrincipal(wi);

                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public void SetVskSelected(string vskFilename)
        {
            try
            {
                if (this.IsVskInstalled(vskFilename))
                {
                    //var reg = this.ServiceProvider.GetService<SLocalRegistry, ILocalRegistry2>();
                    var reg = ServiceProvider.GetService(typeof(SLocalRegistry)) as ILocalRegistry2;

                    string root = null;
                    reg.GetLocalRegistryRoot(out root);

                    using (var vsKey = Registry.CurrentUser.OpenSubKey(root))
                    {
                        if (vsKey != null)
                        {
                            using (var keyboardKey = vsKey.OpenSubKey("Keyboard"))
                            {
                                if (keyboardKey != null)
                                {
                                    keyboardKey.SetValue("SchemeName", GetVSFilePath(vskFilename));  // Set Scheme
                                    //var schemeName = keyboardKey.GetValue("SchemeName") as string;
                                    //return !string.IsNullOrEmpty(schemeName) && string.Equals("Emacs.vsk", Path.GetFileName(schemeName), StringComparison.InvariantCultureIgnoreCase);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                //return false;
            }
            return;
        }
    }
}
