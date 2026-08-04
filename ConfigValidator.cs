using System;
using System.Collections.Generic;
using LabApi.Features.Console;

namespace AutoMessages;

internal static class ConfigValidator
{
    internal const float DefaultStartDelay = 600f;
    internal const float DefaultRepeat = 600f;
    internal const float DefaultBroadcastTime = 20f;
    internal const ushort MaximumBroadcastTime = ushort.MaxValue;

    public static void Validate(Config config)
    {
        if (config.StartDelay < 0f || float.IsNaN(config.StartDelay) || float.IsInfinity(config.StartDelay))
        {
            Logger.Warn($"[AutoMessages] StartDelay не может быть отрицательным или нечисловым. Используется {DefaultStartDelay} секунд.");
            config.StartDelay = DefaultStartDelay;
        }

        if (config.Repeat <= 0f || float.IsNaN(config.Repeat) || float.IsInfinity(config.Repeat))
        {
            Logger.Error($"[AutoMessages] Значение Repeat должно быть больше нуля. Используется {DefaultRepeat} секунд.");
            config.Repeat = DefaultRepeat;
        }

        if (config.DefaultTime <= 0f || float.IsNaN(config.DefaultTime) || float.IsInfinity(config.DefaultTime))
        {
            Logger.Warn($"[AutoMessages] DefaultTime должен быть больше нуля. Используется {DefaultBroadcastTime} секунд.");
            config.DefaultTime = DefaultBroadcastTime;
        }

        if (config.DefaultTime > MaximumBroadcastTime)
        {
            Logger.Warn($"[AutoMessages] DefaultTime превышает предел Broadcast. Используется {MaximumBroadcastTime} секунд.");
            config.DefaultTime = MaximumBroadcastTime;
        }

        if (config.Messages is null)
        {
            Logger.Warn("[AutoMessages] Messages равен null. Используется пустой список.");
            config.Messages = new List<MessageConfig>();
        }

        ValidateMessages(config.Messages, config.Debug);
    }

    private static void ValidateMessages(IReadOnlyList<MessageConfig> messages, bool debug)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < messages.Count; index++)
        {
            MessageConfig? message = messages[index];
            if (message is null)
            {
                Logger.Warn($"[AutoMessages] Элемент messages[{index}] равен null и будет пропущен.");
                continue;
            }

            if (message.Time < 0f || float.IsNaN(message.Time) || float.IsInfinity(message.Time))
            {
                Logger.Warn($"[AutoMessages] У сообщения '{DisplayId(message, index)}' некорректное Time; будет использован DefaultTime.");
                message.Time = 0f;
            }
            else if (message.Time > MaximumBroadcastTime)
            {
                Logger.Warn($"[AutoMessages] Time сообщения '{DisplayId(message, index)}' ограничено значением {MaximumBroadcastTime}.");
                message.Time = MaximumBroadcastTime;
            }

            if (string.IsNullOrWhiteSpace(message.Id))
            {
                Logger.Debug($"[AutoMessages] У messages[{index}] нет Id; для логов будет использован индекс.", debug);
            }
            else if (!ids.Add(message.Id))
            {
                Logger.Warn($"[AutoMessages] Id '{message.Id}' повторяется. Команда send выберет первое совпадение.");
            }

            if (message.IsEnabled && string.IsNullOrWhiteSpace(message.Message))
            {
                Logger.Warn($"[AutoMessages] Сообщение '{DisplayId(message, index)}' пустое и будет пропущено.");
            }
        }
    }

    internal static string DisplayId(MessageConfig message, int index) =>
        string.IsNullOrWhiteSpace(message.Id) ? $"#{index + 1}" : message.Id;
}
