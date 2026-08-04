using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;

namespace AutoMessages;

public sealed class MessageManager
{
    private readonly Random _random = new Random();
    private readonly List<MessageConfig> _activeMessages = new List<MessageConfig>();
    private Config _config;
    private CoroutineHandle? _cycleHandle;
    private int _cycleGeneration;
    private int _nextSequentialIndex;
    private int _lastRandomIndex = -1;

    public MessageManager(Config config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        RefreshMessages();
    }

    public bool IsRunning => _cycleHandle.HasValue && _cycleHandle.Value.IsRunning;

    public int ActiveMessageCount => _activeMessages.Count;

    public string NextMessageId
    {
        get
        {
            if (_activeMessages.Count == 0)
                return "нет";

            if (_config.RandomOrder)
                return "случайный выбор";

            int index = _nextSequentialIndex % _activeMessages.Count;
            return DisplayId(_activeMessages[index], index);
        }
    }

    public void UpdateConfig(Config config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        ResetSelection();
        RefreshMessages();
    }

    public bool TryStart(out string reason)
    {
        if (IsRunning)
        {
            reason = "Цикл сообщений уже запущен.";
            return false;
        }

        return StartCore(out reason);
    }

    public bool Restart(out string reason)
    {
        Stop();
        return StartCore(out reason);
    }

    public void Stop()
    {
        _cycleGeneration++;

        if (_cycleHandle.HasValue)
        {
            // KillCoroutines отменяет ожидание MEC на основном потоке и не оставляет старый цикл после reload/рестарта.
            Timing.KillCoroutines(_cycleHandle.Value);
            _cycleHandle = null;
            Logger.Debug("[AutoMessages] Цикл сообщений остановлен.", _config.Debug);
        }
    }

    public bool SendById(string id, out string response)
    {
        if (!_config.IsEnabled)
        {
            response = "AutoMessages выключен в конфигурации.";
            return false;
        }

        MessageConfig? selected = _activeMessages.FirstOrDefault(message =>
            string.Equals(message.Id, id, StringComparison.OrdinalIgnoreCase));

        if (selected is null)
        {
            response = $"Активное сообщение с Id '{id}' не найдено.";
            return false;
        }

        return Send(selected, _activeMessages.IndexOf(selected), out response);
    }

    public bool SendNext(out string response)
    {
        if (!_config.IsEnabled)
        {
            response = "AutoMessages выключен в конфигурации.";
            return false;
        }

        if (_activeMessages.Count == 0)
        {
            response = "В конфигурации нет активных сообщений.";
            return false;
        }

        int selectedIndex = SelectNextIndex();
        return Send(_activeMessages[selectedIndex], selectedIndex, out response);
    }

    private bool StartCore(out string reason)
    {
        if (!_config.IsEnabled)
        {
            reason = "AutoMessages выключен в конфигурации.";
            return false;
        }

        ResetSelection();
        RefreshMessages();
        if (_activeMessages.Count == 0)
        {
            Logger.Warn("[AutoMessages] В конфигурации нет активных сообщений. Цикл не запущен.");
            reason = "В конфигурации нет активных сообщений.";
            return false;
        }

        // MEC выполняет Coroutine в игровом Unity-потоке; второй цикл блокируется проверкой IsRunning.
        int generation = ++_cycleGeneration;
        _cycleHandle = Timing.RunCoroutine(MessageLoop(generation), Segment.Update);
        reason = $"Цикл сообщений запущен. Первое сообщение через {_config.StartDelay:0.###} секунд.";
        Logger.Debug($"[AutoMessages] {reason}", _config.Debug);
        return true;
    }

    private IEnumerator<float> MessageLoop(int generation)
    {
        yield return Timing.WaitForSeconds(_config.StartDelay);

        while (generation == _cycleGeneration)
        {
            try
            {
                SendNext(out _);
            }
            catch (Exception exception)
            {
                Logger.Error($"[AutoMessages] Необработанная ошибка цикла сообщений: {exception}");
            }

            // Repeat измеряется между началами двух показов; длительность Broadcast к нему не прибавляется.
            yield return Timing.WaitForSeconds(_config.Repeat);
        }
    }

    private int SelectNextIndex()
    {
        if (!_config.RandomOrder)
        {
            int selected = _nextSequentialIndex;
            _nextSequentialIndex = (_nextSequentialIndex + 1) % _activeMessages.Count;
            return selected;
        }

        if (_activeMessages.Count == 1)
        {
            _lastRandomIndex = 0;
            return 0;
        }

        // Выбор из N-1 вариантов исключает предыдущий индекс без потенциально бесконечного повторного броска.
        int candidate;
        if (_lastRandomIndex < 0)
        {
            candidate = _random.Next(_activeMessages.Count);
        }
        else
        {
            candidate = _random.Next(_activeMessages.Count - 1);
            if (candidate >= _lastRandomIndex)
                candidate++;
        }

        _lastRandomIndex = candidate;
        return candidate;
    }

    private bool Send(MessageConfig message, int index, out string response)
    {
        string id = DisplayId(message, index);
        ushort duration = ResolveDuration(message);

        try
        {
            // Глобальный LabAPI Broadcast отправляет только текущим клиентам; Rich Text передаётся без обработки.
            Server.SendBroadcast(message.Message, duration, shouldClearPrevious: _config.ClearPreviousBroadcasts);
            int playerCount = Server.PlayerCount;
            Logger.Debug($"[AutoMessages] Отправлено сообщение '{id}' для {playerCount} игроков на {duration} секунд.", _config.Debug);
            response = $"Сообщение '{id}' отправлено для {playerCount} игроков на {duration} секунд.";
            return true;
        }
        catch (Exception exception)
        {
            Logger.Error($"[AutoMessages] Не удалось отправить сообщение '{id}': {exception}");
            response = $"Не удалось отправить сообщение '{id}'. Подробности записаны в консоль.";
            return false;
        }
    }

    private ushort ResolveDuration(MessageConfig message)
    {
        float requested = message.Time > 0f ? message.Time : _config.DefaultTime;
        float clamped = Math.Min(requested, ConfigValidator.MaximumBroadcastTime);
        return (ushort)Math.Max(1f, (float)Math.Ceiling(clamped));
    }

    private void RefreshMessages()
    {
        _activeMessages.Clear();
        if (_config.Messages is null)
            return;

        foreach (MessageConfig? message in _config.Messages)
        {
            if (message is not null && message.IsEnabled && !string.IsNullOrWhiteSpace(message.Message))
                _activeMessages.Add(message);
        }

        if (_nextSequentialIndex >= _activeMessages.Count)
            _nextSequentialIndex = 0;

        if (_lastRandomIndex >= _activeMessages.Count)
            _lastRandomIndex = -1;
    }

    private void ResetSelection()
    {
        _nextSequentialIndex = 0;
        _lastRandomIndex = -1;
    }

    private static string DisplayId(MessageConfig message, int index) =>
        ConfigValidator.DisplayId(message, index);
}
