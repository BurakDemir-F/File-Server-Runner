using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace FileServerRunner;

public partial class MainWindow : Window
{
    private const string _fileServerExePath = "FileServer.exe";
    private string _servePath;
    private Process _serverProcess;
    public MainWindow()
    {
        InitializeComponent();
        StartServerButton.Click += StartServerButtonClicked;
        PortNumberText.TextChanged += PortNumberTextChanged;
        ServingFolderButton.Click += ChooseFolderButtonClicked;
        CloseServerButton.Click += CloseServerButtonClicked;
        CloseServerButton.IsEnabled = false;
    }

    protected override void OnClosed(EventArgs e)
    {
        StartServerButton.Click -= StartServerButtonClicked;
        PortNumberText.TextChanged -= PortNumberTextChanged;
        ServingFolderButton.Click -= ChooseFolderButtonClicked;
        CloseServerButton.Click -= CloseServerButtonClicked;
        base.OnClosed(e);
    }

    private void CloseServerButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill();
        }

        StartServerButton.IsEnabled = true;
        CloseServerButton.IsEnabled = false;
    }

    private void StartServerButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_servePath) || string.IsNullOrEmpty(PortNumberText.Text))
        {
            ServerInfo.Text = "folder path or port number is empty";
            return;
        }

        if (!File.Exists(Path.GetFullPath(_fileServerExePath)))
        {
            ServerInfo.Text = "FileServer.exe not found!";
            return;
        }
        
        var processStartInfo =
            new ProcessStartInfo(_fileServerExePath, $"--path {_servePath} --port {PortNumberText.Text}");

        _serverProcess = new Process();
        _serverProcess.StartInfo = processStartInfo;
        _serverProcess.Start();
        CloseServerButton.IsEnabled = true;
    }

    private async void ChooseFolderButtonClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            { Title = "Choose Folder To Serve", AllowMultiple = false });
        
        if (folders.Count == 0)
        {
            ServerInfo.Text = "something wrong";
            return;
        }

        var folder = folders[0];
        _servePath = folder.Path.LocalPath;
        FolderPathBlock.Text = _servePath;
    }

    private void PortNumberTextChanged(object? sender, TextChangedEventArgs e)
    {
        IpAddressBlock.Text = GetServeUrl(PortNumberText.Text);
    }
    
    private string GetServeUrl(string port)
    {
        return $"http://{GetLocalIpAddress()}:{port}";
    }

    private  string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}