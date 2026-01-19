using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using Tharga.Communication.Contract;
using Tharga.Communication.MessageHandler;

namespace Tharga.Communication.Client;

internal sealed class SignalRHostedService : BackgroundService, ISignalRHostedService
{
    private readonly IInstanceService _instanceService;
    private readonly IMessageExecutor _messageExecutor;
    private readonly CommunicationOptions _options;
    private readonly ILogger<SignalRHostedService> _logger;

    private HubConnection _connection;

    private readonly string _serverAddress;

    public SignalRHostedService(IInstanceService instanceService, IMessageExecutor messageExecutor, IOptions<CommunicationOptions> options, ILogger<SignalRHostedService> logger)
    {
        _instanceService = instanceService;
        _messageExecutor = messageExecutor;
        _options = options.Value;
        _logger = logger;

        _serverAddress = $"{_options.ServerAddress.TrimEnd('/')}/{_options.Pattern.TrimStart('/')}";
    }

    public event EventHandler<HubConnectionStateChangedEventArgs> HubConnectionStateChangedEvent;
    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public Task SendAsync(string methodName, object payload)
    {
        return _connection.SendAsync(methodName, payload);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                BuildConnection();

                await StartConnectionAsync(stoppingToken);
                await WaitForDisconnectAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                //_logger.LogWarning(e, "SignalR loop failure.");
                _logger.LogWarning("SignalR loop failure. {message}", e.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private void BuildConnection()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_serverAddress, options =>
            {
                options.Headers.Add(Constants.Header.Instance, _instanceService.AgentInstanceKey.ToString());
                options.Headers.Add(Constants.Header.Machine, Environment.MachineName);

                var assembly = Assembly.GetEntryAssembly()?.GetName();
                if (!string.IsNullOrEmpty(assembly?.Name))
                {
                    options.Headers.Add(Constants.Header.Type, assembly.Name);
                }

                var version = assembly?.Version?.ToString();
                if (!string.IsNullOrEmpty(version))
                {
                    options.Headers.Add(Constants.Header.Version, version);
                }
            })
            .WithAutomaticReconnect(_options.ReconnectDelays)
            .Build();

        if (Debugger.IsAttached)
        {
            _connection.ServerTimeout = TimeSpan.FromMinutes(10);
            _connection.KeepAliveInterval = TimeSpan.FromSeconds(30);
        }

        _connection.Reconnecting += error =>
        {
            RaiseStateChanged(HubConnectionState.Connected, HubConnectionState.Reconnecting);
            _logger.LogWarning(error, "SignalR reconnecting");
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            RaiseStateChanged(HubConnectionState.Reconnecting, HubConnectionState.Connected);
            _logger.LogInformation("SignalR reconnected");
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            RaiseStateChanged(_connection.State, HubConnectionState.Disconnected);
            _logger.LogWarning(error, "SignalR connection closed");
            return Task.CompletedTask;
        };

        _connection.On<RequestWrapper>(Constants.PostMessage, payload =>
        {
            Task.Run(() =>
            {
                _messageExecutor.ExecuteAsync(null, payload);
            });
        });

        _connection.On<RequestWrapper>(Constants.SendMessage, payload =>
        {
            Task.Run(async () =>
            {
                var response = await _messageExecutor.ExecuteAsync(null, payload);
                await _connection.SendAsync(Constants.ResponseMessage, response);
            });
        });

        _connection.On<RequestWrapper>(Constants.ResponseMessage, async payload =>
        {
            Debugger.Break();
            throw new NotImplementedException();
        });
    }

    private async Task StartConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        RaiseStateChanged(HubConnectionState.Disconnected, HubConnectionState.Connecting);
        _logger.LogInformation("Connecting to SignalR server {Server}...", _serverAddress);

        await _connection.StartAsync(cancellationToken);

        RaiseStateChanged(HubConnectionState.Connecting, HubConnectionState.Connected);
        _logger.LogInformation("Connected to SignalR server {Server}.", _serverAddress);
    }

    private async Task WaitForDisconnectAsync(CancellationToken cancellationToken)
    {
        while (_connection.State != HubConnectionState.Disconnected && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.StopAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private void RaiseStateChanged(HubConnectionState before, HubConnectionState after)
    {
        HubConnectionStateChangedEvent?.Invoke(this, new HubConnectionStateChangedEventArgs(before, after));
    }
}