using System.Collections.Generic;
using System.ComponentModel;

namespace AutoMessages;

public sealed class Config
{
    [Description("Включает автоматические сообщения и связанные команды отправки.")]
    public bool IsEnabled { get; set; } = true;

    [Description("Включает подробное диагностическое логирование без посекундного спама.")]
    public bool Debug { get; set; }

    [Description("Задержка до первого сообщения в секундах.")]
    public float StartDelay { get; set; } = 600f;

    [Description("Интервал между началами показа сообщений в секундах.")]
    public float Repeat { get; set; } = 600f;

    [Description("Длительность сообщения по умолчанию в секундах.")]
    public float DefaultTime { get; set; } = 20f;

    [Description("Выбирает сообщения случайно без немедленного повторения.")]
    public bool RandomOrder { get; set; }

    [Description("ОСТОРОЖНО: очистка удаляет также Broadcast других плагинов. По умолчанию выключено.")]
    public bool ClearPreviousBroadcasts { get; set; }

    [Description("Разрешает автоматический цикл только во время активного раунда.")]
    public bool ShowOnlyDuringRound { get; set; } = true;

    [Description("Список информационных сообщений. Rich Text и HEX-цвета передаются без изменений.")]
    public List<MessageConfig>? Messages { get; set; } =
    [
        new MessageConfig
        {
            Id = "admin_recruitment",
            IsEnabled = true,
            Time = 20f,
            Message = "<color=#FFD700>Хотите стать администратором?</color>\nЗаходите в наш <color=#5865F2>Discord</color> и пройдите собеседование!",
        },
        new MessageConfig
        {
            Id = "discord",
            IsEnabled = true,
            Time = 20f,
            Message = "<color=#5865F2>Наш Discord:</color>\n<color=#FFFFFF>discord.gg/example</color>",
        },
        new MessageConfig
        {
            Id = "rules",
            IsEnabled = true,
            Time = 15f,
            Message = "<color=#FF5555>Не нарушайте правила сервера!</color>\nНезнание правил не освобождает от ответственности.",
        },
        new MessageConfig
        {
            Id = "welcome",
            IsEnabled = true,
            Time = 20f,
            Message = "<color=#00FF00>Приятной игры на нашем сервере!</color>",
        },
    ];
}
