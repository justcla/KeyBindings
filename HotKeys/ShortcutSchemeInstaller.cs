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

        private readonly Package package;
        private readonly string SublimeVSKFileSrc = "SublimeWithCSharp.vsk";
        private readonly string SublimeVSKFileDest = "Sublime with C#.*";

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public ShortcutSchemeInstaller(Package package)
        {
            this.package = package;
        }

        public void InstallVSKs()
        {
            try
            {
                // if the emacs keybindings are not installed and the user is not running elevated
                // then we need to copy the emacs.vsk for our extension to work
                // get an ok from the user first
                //if (!IsVskInstalled(EmacsVskFile))
                {

                    string installPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    installPath = Path.Combine(installPath, SublimeVSKFileSrc);

                    var dlg = new InstallVSKConfirmDialog();
                    dlg.StartPosition = FormStartPosition.CenterScreen;
                    if (IsAdministrator || dlg.ShowDialog() == DialogResult.OK)
                    {
                        CopyVskUsingXCopy(installPath);
                    }
                }
            }
            catch (Exception e)
            {
                var message = e.Message;
            }
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

        private void CopyVskUsingXCopy(string vskSrcPath)
        {
            var vskDestPath = Path.Combine(VSInstallationPath, SublimeVSKFileDest);

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = @"xcopy.exe";
            process.StartInfo.Arguments = string.Format(@" /Y ""{0}"" ""{1}""", vskSrcPath, vskDestPath);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major > 5)
            {
                process.StartInfo.Verb = "runas";
            }
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
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
