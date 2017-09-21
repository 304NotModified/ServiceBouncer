using System.ComponentModel;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using ServiceBouncer.Annotations;

namespace ServiceBouncer
{
    using System;

    public sealed class ServiceViewModel : INotifyPropertyChanged
    {
        private readonly ServiceController controller;
        private string name;
        private string status;
        public event PropertyChangedEventHandler PropertyChanged;

        public ServiceViewModel(ServiceController controller)
        {
            this.controller = controller;
            name = controller.DisplayName;
        }

        public string Name
        {
            get { return name; }
        }

        public string ServiceName
        {
            get { return controller.ServiceName; }
        }

        public string Status
        {
            get { return status; }
            set
            {
                if (value == status) return;
                status = value;
                OnPropertyChanged("Status");
            }
        }

        public string StartupType
        {
            get
            {
                var wmiManagementObject =
                    new ManagementObject(new ManagementPath(string.Format("Win32_Service.Name='{0}'", this.ServiceName)));

                return wmiManagementObject["StartMode"].ToString();
            }
            set
            {
                var wmiManagementObject =
                    new ManagementObject(new ManagementPath(string.Format("Win32_Service.Name='{0}'", this.ServiceName)));

                var parameters = new object[1];
                parameters[0] = value.ToString();
                wmiManagementObject.InvokeMethod("ChangeStartMode", parameters);
                OnPropertyChanged("Startuptype");
            }
        }

        public void Refresh()
        {
            try
            {
                controller.Refresh();
                Status = controller.Status.ToString();
            }
            catch
            {
                Status = "Unknown";
            }
        }

        public void Start()
        {
            if (controller.Status == ServiceControllerStatus.Stopped || controller.Status == ServiceControllerStatus.Paused)
            {
                controller.Start();
            }
        }

        public void Restart()
        {
            if (controller.Status == ServiceControllerStatus.Running)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                        controller.Start();
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }

        public void Stop()
        {
            if (controller.Status == ServiceControllerStatus.Running)
            {
                controller.Stop();
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}