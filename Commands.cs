using System;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;

namespace AutoMessages;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class AutoMessagesCommand : ParentCommand
{
    public override string Command => "automessages";

    public override string[] Aliases => new[] { "amsg" };

    public override string Description => "Управление автоматическими сообщениями.";

    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new StatusCommand());
        RegisterCommand(new SendCommand());
        RegisterCommand(new ReloadCommand());
        RegisterCommand(new StartCommand());
        RegisterCommand(new StopCommand());
        RegisterCommand(new NextCommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!CommandAccess.Authorize(sender, out response))
            return false;

        response = "Подкоманды: status, send <id>, reload, start, stop, next.";
        return true;
    }
}

internal static class CommandAccess
{
    public const string Permission = "automessages.manage";

    public static bool Authorize(ICommandSender sender, out string response)
    {
        if (!sender.HasPermission(Permission))
        {
            response = $"Недостаточно прав. Требуется разрешение '{Permission}'.";
            return false;
        }

        response = string.Empty;
        return true;
    }

    public static bool TryGetPlugin(ICommandSender sender, out Plugin plugin, out string response)
    {
        if (!Authorize(sender, out response))
        {
            plugin = null!;
            return false;
        }

        if (Plugin.Instance is null || Plugin.Instance.MessageManager is null)
        {
            plugin = null!;
            response = "AutoMessages сейчас недоступен.";
            return false;
        }

        plugin = Plugin.Instance;
        return true;
    }
}

public sealed class StatusCommand : ICommand
{
    public string Command => "status";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Показывает состояние AutoMessages.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!CommandAccess.TryGetPlugin(sender, out Plugin plugin, out response))
            return false;

        MessageManager manager = plugin.MessageManager!;
        Config config = plugin.Config;
        StringBuilder result = new StringBuilder();
        result.AppendLine($"AutoMessages: {(config.IsEnabled ? "включён" : "выключен")}");
        result.AppendLine($"Цикл: {(manager.IsRunning ? "запущен" : "остановлен")}");
        result.AppendLine($"Активных сообщений: {manager.ActiveMessageCount}");
        result.AppendLine($"Режим: {(config.RandomOrder ? "случайный" : "последовательный")}");
        result.AppendLine($"Интервал: {config.Repeat:0.###} секунд");
        result.AppendLine($"Длительность по умолчанию: {config.DefaultTime:0.###} секунд");
        result.Append($"Следующее сообщение: {manager.NextMessageId}");
        response = result.ToString();
        return true;
    }
}

public sealed class SendCommand : ICommand
{
    public string Command => "send";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Немедленно отправляет сообщение по Id, не меняя таймер.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!CommandAccess.TryGetPlugin(sender, out Plugin plugin, out response))
            return false;

        if (arguments.Count != 1)
        {
            response = "Использование: automessages send <id>";
            return false;
        }

        if (!plugin.CanSendNow(out response))
            return false;

        return plugin.MessageManager!.SendById(arguments.At(0), out response);
    }
}

public sealed class ReloadCommand : ICommand
{
    public string Command => "reload";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Безопасно перезагружает конфигурацию AutoMessages.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!CommandAccess.TryGetPlugin(sender, out Plugin plugin, out response))
            return false;

        return plugin.ReloadConfiguration(out response);
    }
}

public sealed class StartCommand : ICommand
{
    public string Command => "start";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Запускает один цикл автоматических сообщений.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!CommandAccess.TryGetPlugin(sender, out Plugin plugin, out response))
            return false;

        return plugin.StartFromCommand(out response);
    }
}

public sealed class StopCommand : ICommand
{
    public string Command => "stop";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Останавливает цикл автоматических сообщений.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!CommandAccess.TryGetPlugin(sender, out Plugin plugin, out response))
            return false;

        plugin.MessageManager!.Stop();
        response = "Цикл сообщений остановлен.";
        return true;
    }
}

public sealed class NextCommand : ICommand
{
    public string Command => "next";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Немедленно показывает следующее сообщение и обновляет порядок.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!CommandAccess.TryGetPlugin(sender, out Plugin plugin, out response))
            return false;

        if (!plugin.CanSendNow(out response))
            return false;

        return plugin.MessageManager!.SendNext(out response);
    }
}
