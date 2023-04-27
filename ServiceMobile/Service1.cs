using NLog.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace ServiceMobile
{
    public partial class Service1 : ServiceBase
    {
        public Logger currentClassLogger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            currentClassLogger.Debug("Старт сервиса");
            Task.Run((Action)(() => this.Processing()));
            this.EventLog.WriteEntry("Сервис запущен!", EventLogEntryType.Information);
        }

        private void Processing()
        {
            try
            {
                currentClassLogger.Debug("work");
                SmartMain.work();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override void OnStop()
        {
            currentClassLogger.Debug("Стоп сервиса");
            this.EventLog.WriteEntry("Сервис остановлен!", EventLogEntryType.Information);
        }
    }
}
