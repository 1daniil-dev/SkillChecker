using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using SkillChecker.Web;
using SkillCheckerServer;

namespace SkillChecker.Teacher
{
    public partial class MainWindow : Window
    {
        private const int ServerPort = 9000;
        private const string PanelUrl = "http://localhost:5000";

        private Server _server = null!;
        private WebApplication _webApp = null!;
        private List<string> _ipList = new List<string>();
        private bool _isClosing;

        public MainWindow()
        {
            InitializeComponent();
            StartServer();
            StartWebPanel();
            ShowAddresses();
            OpenPanelDelayed();
        }

        private void StartServer()
        {
            _server = new Server(ServerPort);
            Thread serverThread = new Thread(() =>
            {
                try
                {
                    _server.Start();
                }
                catch (Exception ex)
                {
                    ShowError("Не удалось запустить сервер: " + ex.Message);
                }
            });
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        private void StartWebPanel()
        {
            _webApp = WebPanelHost.Build(AppDomain.CurrentDomain.BaseDirectory);
            _webApp.Urls.Add(PanelUrl);
            Thread webThread = new Thread(() =>
            {
                try
                {
                    _webApp.Run();
                }
                catch (Exception ex)
                {
                    ShowError("Не удалось запустить веб-панель: " + ex.Message);
                }
            });
            webThread.IsBackground = true;
            webThread.Start();
        }

        private void OpenPanelDelayed()
        {
            Task.Run(async () =>
            {
                await Task.Delay(1500);
                if (_isClosing)
                {
                    return;
                }
                Dispatcher.Invoke(OpenPanel);
            });
        }

        private void ShowAddresses()
        {
            _ipList = GetLocalIps();
            if (_ipList.Count == 0)
            {
                _ipList.Add("127.0.0.1");
            }
            string ipText = "";
            for (int i = 0; i < _ipList.Count; i++)
            {
                if (i > 0)
                {
                    ipText += "\n";
                }
                ipText += _ipList[i];
            }
            IpAddressText.Text = ipText;
            PortText.Text = ServerPort.ToString();
        }

        private List<string> GetLocalIps()
        {
            List<string> ipList = new List<string>();
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                NetworkInterface ni = interfaces[i];
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                {
                    continue;
                }

                IPInterfaceProperties props = ni.GetIPProperties();
                UnicastIPAddressInformationCollection addresses = props.UnicastAddresses;
                foreach (UnicastIPAddressInformation addr in addresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipList.Add(addr.Address.ToString());
                    }
                }
            }
            return ipList;
        }

        private void OpenPanel()
        {
            Process.Start(new ProcessStartInfo(PanelUrl) { UseShellExecute = true });
        }

        private void OpenPanel_Click(object sender, RoutedEventArgs e)
        {
            OpenPanel();
        }

        private async void CopyAddress_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_ipList[0] + "\n" + ServerPort);
                CopyAddressButton.Content = "Скопировано ✓";
            }
            catch
            {
                CopyAddressButton.Content = "Не удалось";
            }
            await Task.Delay(2000);
            CopyAddressButton.Content = "Копировать";
        }

        private void ShowError(string message)
        {
            Dispatcher.Invoke(() => MessageBox.Show(this, message, "SkillChecker", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosing = true;
            _server.Stop();
            base.OnClosed(e);
        }
    }
}
