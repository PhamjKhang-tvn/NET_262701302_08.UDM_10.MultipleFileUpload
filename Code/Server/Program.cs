using System;
using System.Windows.Forms;
using Udm10.UI;
namespace Udm10.Server
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Theme.InstallErrorHandlers(); Application.Run(new ServerForm());
        }
    }
}
