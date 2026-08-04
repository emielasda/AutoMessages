# AutoMessages

`AutoMessages` — прозрачный серверный плагин для SCP: Secret Laboratory, который показывает общие информационные сообщения через стандартный Broadcast.

Проект проверен для **SCP:SL 14.2.7** (dedicated-server build `23340795`) и **LabAPI 1.1.7**. Используются только официальные API LabAPI и игровые сборки: EXILED и NwPluginAPI не нужны.

## Возможности

- последовательный или случайный порядок без немедленного повтора;
- отдельная длительность каждого сообщения и общая длительность по умолчанию;
- исходные SCP:SL Rich Text/HEX-теги и многострочный YAML без преобразования;
- ровно один MEC-цикл в основном Unity-потоке;
- остановка при завершении/рестарте раунда, выгрузке и reload плагина;
- проверка отрицательных, нулевых, пустых и слишком больших значений;
- глобальный `LabApi.Features.Wrappers.Server.SendBroadcast`;
- RemoteAdmin-команда `automessages` с разрешением `automessages.manage`.

`repeat` — интервал между **началами** двух показов. Например, при `start_delay: 600` и `repeat: 600` сообщения начинаются на 10-й, 20-й и 30-й минутах. `time` не прибавляется к интервалу.

## Структура

```text
AutoMessages/
├── AutoMessages.csproj
├── Plugin.cs
├── Config.cs
├── MessageConfig.cs
├── ConfigValidator.cs
├── MessageManager.cs
├── Commands.cs
├── config.example.yml
└── README.md
```

## Требования

- SCP:SL Dedicated Server, совместимый с LabAPI 1.1.7;
- LabAPI 1.1.7;
- .NET SDK 8.x для сборки;
- .NET Framework 4.8 targeting pack либо reference assemblies, которые SDK получает при restore;
- путь к каталогу `SCPSL_Data/Managed` установленного dedicated server.

## Сборка

Из каталога репозитория выполните одну из команд:

```powershell
dotnet build .\AutoMessages.csproj -c Release -p:ScpSlServerPath="D:\Servers\SCP Secret Laboratory Dedicated Server"
```

или укажите Managed-каталог напрямую:

```powershell
dotnet build .\AutoMessages.csproj -c Release -p:ScpSlManagedPath="D:\Servers\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed"
```

Готовый файл находится в `bin/Release/net48/AutoMessages.dll`. Проект получает `LabApi.dll` из официального NuGet-пакета `Northwood.LabAPI` 1.1.7, а `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll` (MEC), `CommandSystem.Core.dll` и `UnityEngine.CoreModule.dll` — из вашей версии сервера. Они имеют `Private=false` и не копируются в результат.

## Установка

1. Установите и запустите LabAPI 1.1.7 хотя бы один раз.
2. Скопируйте только `AutoMessages.dll` в `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global` для всех портов либо в папку конкретного порта внутри `plugins`.
3. Запустите сервер. LabAPI создаст `%AppData%\SCP Secret Laboratory\LabAPI\configs\<порт>\AutoMessages\config.yml` со значениями по умолчанию.
4. Отредактируйте конфиг и выполните `automessages reload` в RemoteAdmin либо перезапустите плагин/сервер.

## Настройка

Полный готовый вариант находится в [`config.example.yml`](config.example.yml). Основные поля:

- `is_enabled`: полностью запрещает цикл и отправку;
- `debug`: диагностические сообщения о границах раунда, старте/остановке и отправке;
- `start_delay`: секунд до первого показа;
- `repeat`: секунд между началами показов, строго больше нуля;
- `default_time`: длительность, если у сообщения `time: 0`;
- `random_order`: случайный порядок без повторения одного элемента подряд;
- `clear_previous_broadcasts`: очистка общей очереди Broadcast перед отправкой; может удалить сообщения других плагинов;
- `show_only_during_round`: запрещает показ вне активного раунда. Если выключено, цикл может работать в ожидании игроков и после окончания раунда, но на каждой границе старый MEC-цикл сначала уничтожается;
- `messages`: список объектов `id`, `is_enabled`, `time`, `message`.

Многострочный текст удобнее задавать через YAML `|-`:

```yaml
message: |-
  <color=#FF0000>Красный</color> и <color=#00FF00>зелёный</color>
  <size=28>Вторая строка</size>
```

Можно также использовать строку с `\n`. Плагин не очищает, не экранирует и не меняет Rich Text.

## Команды и права

```text
automessages status
automessages send <id>
automessages reload
automessages start
automessages stop
automessages next
```

Добавьте `automessages.manage` нужной группе в `%AppData%\SCP Secret Laboratory\LabAPI\configs\permissions.yml`. Серверная консоль разрешена встроенной системой LabAPI; обычный игрок без разрешения получит отказ.

`send` не меняет индекс и таймер автоматического цикла. `next` меняет индекс/состояние случайного выбора, но также не сбрасывает таймер. `start` не создаёт второй цикл. `reload` останавливает старый цикл, вызывает реальный `LoadConfigs()`, проверяет значения и запускает один новый цикл, если это разрешено текущим состоянием раунда.

## Проверка

1. **Последовательно:** установите `start_delay: 10`, `repeat: 30`, `random_order: false`; после старта раунда проверьте показ 1-го сообщения через 10 секунд и следующих через каждые 30 секунд.
2. **Случайно:** включите `random_order`; по debug-логам убедитесь, что соседние Id не совпадают. С одним активным сообщением сервер не зависает.
3. **Окончание:** завершите раунд; старый handle должен быть убит, а при `show_only_during_round: true` новых сообщений быть не должно.
4. **Новый раунд:** дважды инициируйте логику запуска; в логах остаётся один цикл, Broadcast не дублируется.
5. **Пустой список:** задайте `messages: []`, выполните reload; появится предупреждение, исключения и цикл отсутствуют.
6. **Цвет:** отправьте `automessages send welcome` и проверьте цвет, затем тестовую строку с двумя `<color=#...>` и переносом.
7. **Некорректные числа:** задайте отрицательный `start_delay`, `repeat: 0`, `default_time: 0`; после reload должны примениться 600, 600 и 20 секунд с понятными логами.
8. **Один случайный элемент:** оставьте один активный элемент и включите случайный режим; он повторяется без цикла повторного выбора.
9. **Reload/Disable:** несколько раз перезагрузите плагин; старые события и MEC Coroutine не должны продолжать отправку.

## Возможные проблемы совместимости

- SCP:SL и LabAPI обновляются вместе. При несовпадении игровых DLL с LabAPI 1.1.7 сборка либо загрузчик могут сообщить об отсутствующем типе/методе; используйте серверную версию, для которой выпущен LabAPI 1.1.7, либо пересоберите после официального обновления LabAPI.
- `Assembly-CSharp-firstpass.dll` содержит MEC в текущей серверной сборке. Если Northwood перенесёт MEC в другую сборку, потребуется обновить только ссылку проекта и подтвердить API нового релиза.
- `clear_previous_broadcasts: true` намеренно вызывает глобальную очистку и может затронуть Broadcast других плагинов.
- При повреждённом YAML стандартный `Plugin<TConfig>.LoadConfigs()` LabAPI использует безопасную конфигурацию по умолчанию и пишет предупреждение.
- Файлы с кириллицей должны сохраняться в UTF-8.
