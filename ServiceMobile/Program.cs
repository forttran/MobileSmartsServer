using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ServiceMobile
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main(string[] args)
        {
            /*ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);*/
            if (Environment.UserInteractive)
            {
                if (args == null || args.Length == 0)
                    return;
                switch (args[0])
                {
                    case "--install":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new string[1]
                            {
                Assembly.GetExecutingAssembly().Location
                            });
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            break;
                        }
                    case "--uninstall":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new string[2]
                            {
                "/u",
                Assembly.GetExecutingAssembly().Location
                            });
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            break;
                        }
                }
            }
            else
                ServiceBase.Run(new ServiceBase[1]
                {
          (ServiceBase) new Service1()
                });
        }
    }
}
