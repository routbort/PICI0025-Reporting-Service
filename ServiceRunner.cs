using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using System.Net.Mail;
using System.Data.SqlClient;

namespace PICI0025_Reporting_Service
{
    public partial class ServiceRunner : ServiceBase
    {

        Timer _workTimer;
        bool _ServiceStartupWorkDone = false;
        object _lockObject = new object();
        Config _config;

        public ServiceRunner()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("PICI0025 Reporting Service"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "PICI0025 Reporting Service", String.Empty);
            }
            _EventLog.Source = "PICI0025 Reporting Service";
        }

        protected override void OnStart(string[] args)
        {
            _EventLog.WriteEntry("Raw service start");
            _config = Config.GetDefaultConfig();
            _workTimer = new Timer(new TimerCallback(DoWork), null, 1000, Timeout.Infinite); //run loop once on service startup
        }

        protected override void OnStop()
        {
        }

        ThreadWorker _ThreadWorker;

        void DoWork(object state)
        {

            lock (_lockObject)  //thread synchronization should not be necessary with the current callback model, but it's here anyways
            {
                if (!_ServiceStartupWorkDone)
                {
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                    LogEvent("Service started on machine " + Environment.MachineName + " at " + DateTime.Now.ToLongTimeString(), true);
                    _ThreadWorker = new ThreadWorker(_config);
                    _ThreadWorker.LogEvent += _ThreadWorker_LogEvent;
                    _ServiceStartupWorkDone = true;
                }
                try
                {
                    _ThreadWorker.DoWork();
                }
                catch (Exception ex)
                {
                    LogEvent(ex.Message, true, "ERROR");
                }

                _workTimer.Change(1000 * _config.MainLoopPeriod, Timeout.Infinite);  //define the next loop tick

            }
        }

        private void _ThreadWorker_LogEvent(object sender, ThreadWorker.LogInfoEventArgs e)
        {
            LogEvent(e.Message, e.SendEmail, e.Type.ToString());
        }

        void MailMessage(string from, string to, string body, string subject, string server, int port)
        {
            MailMessage emailMessage = new MailMessage();
            emailMessage.From = new MailAddress(from);
            foreach (var toAddress in to.Split(new char[] { ',', ';' }))
                emailMessage.To.Add(new MailAddress(toAddress));
            emailMessage.Subject = subject;
            emailMessage.Body = body;
            SmtpClient MailClient = new SmtpClient(server, port);
            try { MailClient.Send(emailMessage); }
            catch (Exception ex)
            {
                _EventLog.WriteEntry("Error - unable to send email: " + ex.Message);
            }
        }


        public void LogEvent(string message, bool SendEmail, string messageType = "")
        {
            _EventLog.WriteEntry(message);
            if (SendEmail)
                MailMessage(
                    _config.EmailFrom, 
                    _config.EmailTo, message, 
                    "PICI0025 Reporting Service Message: " + messageType,
                    _config.SMTPServer,
                    _config.SMTPPort);     
        }


        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogEvent("Unhandled exception: " + e.ExceptionObject.ToString(), true);
        }
    }
}
