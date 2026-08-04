using System;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;

namespace AutoMessages;

public sealed class Plugin : Plugin<Config>
{
    private bool _isEnabled;
    private MessageManager? _messageManager;

    public static Plugin? Instance { get; private set; }

    public override string Name => "AutoMessages";

    public override string Description => "Автоматические информационные сообщения для сервера SCP:SL";

    public override string Author => "emielasda";

    public override Version Version { get; } = new Version(1, 0, 0);

    public override Version RequiredApiVersion { get; } = new Version(1, 1, 7);

    public override bool IsTransparent => true;

    public override string ConfigFileName { get; set; } = "config.yml";

    public MessageManager? MessageManager => _messageManager;

    public override void LoadConfigs()
    {
        _messageManager?.Stop();
        base.LoadConfigs();
        ConfigValidator.Validate(Config);
        _messageManager?.UpdateConfig(Config);

        if (_isEnabled && _messageManager is not null && Config.IsEnabled &&
            (!Config.ShowOnlyDuringRound || Round.IsRoundInProgress))
        {
            _messageManager.TryStart(out _);
        }
    }

    public override void Enable()
    {
        if (_isEnabled)
            return;

        Instance = this;
        _messageManager = new MessageManager(Config);
        RegisterEvents();
        _isEnabled = true;

        if (Config.IsEnabled && (!Config.ShowOnlyDuringRound || Round.IsRoundInProgress))
            _messageManager.TryStart(out _);

        Logger.Info($"[AutoMessages] Плагин версии {Version} успешно включён.");
    }

    public override void Disable()
    {
        if (!_isEnabled)
            return;

        UnregisterEvents();
        _messageManager?.Stop();
        _messageManager = null;
        Instance = null;
        _isEnabled = false;
        Logger.Info("[AutoMessages] Плагин отключён.");
    }

    public bool ReloadConfiguration(out string response)
    {
        try
        {
            LoadConfigs();
            response = "Конфигурация AutoMessages перезагружена и проверена.";
            return true;
        }
        catch (Exception exception)
        {
            Logger.Error($"[AutoMessages] Ошибка перезагрузки конфигурации: {exception}");
            response = "Не удалось перезагрузить конфигурацию. Подробности записаны в консоль.";
            return false;
        }
    }

    public bool StartFromCommand(out string response)
    {
        if (!Config.IsEnabled)
        {
            response = "AutoMessages выключен в конфигурации.";
            return false;
        }

        if (Config.ShowOnlyDuringRound && !Round.IsRoundInProgress)
        {
            response = "Цикл нельзя запустить вне активного раунда: show_only_during_round включён.";
            return false;
        }

        if (_messageManager is null)
        {
            response = "Менеджер сообщений недоступен.";
            return false;
        }

        return _messageManager.TryStart(out response);
    }

    public bool CanSendNow(out string response)
    {
        if (!Config.IsEnabled)
        {
            response = "AutoMessages выключен в конфигурации.";
            return false;
        }

        if (Config.ShowOnlyDuringRound && !Round.IsRoundInProgress)
        {
            response = "Сообщения запрещены вне активного раунда: show_only_during_round включён.";
            return false;
        }

        response = string.Empty;
        return true;
    }

    private void RegisterEvents()
    {
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.RoundEnded += OnRoundEnded;
        ServerEvents.RoundRestarted += OnRoundRestarted;
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
    }

    private void UnregisterEvents()
    {
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.RoundEnded -= OnRoundEnded;
        ServerEvents.RoundRestarted -= OnRoundRestarted;
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
    }

    private void OnRoundStarted()
    {
        Logger.Debug("[AutoMessages] Раунд начался.", Config.Debug);
        if (!Config.IsEnabled || _messageManager is null)
            return;

        _messageManager.Restart(out _);
    }

    private void OnRoundEnded(RoundEndedEventArgs eventArgs)
    {
        Logger.Debug($"[AutoMessages] Раунд окончен. Победившая сторона: {eventArgs.LeadingTeam}.", Config.Debug);
        StopForRoundBoundary(restartOutsideRound: !Config.ShowOnlyDuringRound);
    }

    private void OnRoundRestarted()
    {
        Logger.Debug("[AutoMessages] Раунд перезапущен; старый цикл остановлен.", Config.Debug);
        _messageManager?.Stop();
    }

    private void OnWaitingForPlayers()
    {
        Logger.Debug("[AutoMessages] Сервер ожидает игроков; старый цикл остановлен.", Config.Debug);
        StopForRoundBoundary(restartOutsideRound: !Config.ShowOnlyDuringRound);
    }

    private void StopForRoundBoundary(bool restartOutsideRound)
    {
        _messageManager?.Stop();

        if (restartOutsideRound && Config.IsEnabled)
            _messageManager?.TryStart(out _);
    }
}
