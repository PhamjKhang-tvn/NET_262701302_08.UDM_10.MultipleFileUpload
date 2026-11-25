using System;
using System.Windows.Forms;
using Udm10.UI;
namespace Udm10.Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Theme.InstallErrorHandlers(); Application.Run(new ClientForm());
        }
    }
}
