# FileService

## События файлов

После завершения обычной или multipart-загрузки сервис публикует `AssetReady`,
после удаления — `AssetDeleted`. Контракты находятся в `FileService.Shared.Messaging`.
Изменение ассета и запись сообщения сохраняются в одной транзакции PostgreSQL.
Wolverine доставляет сообщение из outbox в RabbitMQ после commit.

Маршрутизация:

- exchange: `file-events`, тип `topic`;
- routing keys: `asset.ready.{entityType}`, `asset.deleted.{entityType}`;
- DirectoryService: очередь `directory.asset-events`, binding `asset.*.*`.

Оба сервиса должны подключаться к одному RabbitMQ и одному virtual host.
Очередь и binding должны существовать до первой публикации: для локальной
проверки сначала запустите DirectoryService. Exchange сам по себе не хранит сообщения.

DirectoryService сохраняет локальную проекцию `asset_states` и использует её при
прикреплении фотографии. Повторные события безопасны; состояние `Deleted`
не заменяется поздним `Ready`. Durable inbox хранит состояние обработки сообщений,
а SQL репозитория обеспечивает идемпотентность бизнес-изменений.

Пустая `files.wolverine_outgoing_envelopes` после успешной доставки — нормально:
это рабочая таблица outbox, а не история событий.

## Интеграционные тесты

Нужны .NET 10, запущенный Docker и восстановленные NuGet-зависимости.
Тесты сами поднимают отдельные PostgreSQL, MinIO и RabbitMQ на случайных портах.
Рабочие контейнеры из docker-compose для них не нужны. Кэш заменяется на память процесса.

```sh
dotnet test tests/FileService.IntegrationTests/FileService.IntegrationTests.csproj
```

Только проверки событий и outbox:

```sh
dotnet test tests/FileService.IntegrationTests/FileService.IntegrationTests.csproj --filter FullyQualifiedName~AssetEventsTests
```

Проверяются:

- публикация `AssetReady` с корректными идентификаторами и типом ассета;
- публикация `AssetDeleted` после удаления;
- `AssetReady` после multipart-загрузки (в существующем тесте полного цикла);
- откат бизнес-данных вместе с записанным сообщением при отсутствии commit;
- успешное завершение загрузки при выключенном RabbitMQ, наличие сообщения
  в outbox и его доставка после восстановления брокера.

Тест восстановления останавливает только RabbitMQ, созданный этим тестовым набором.
Сообщения читаются из отдельной очереди `tests.asset-events` через management API.
Это проверка транспорта FileService; обработчик DirectoryService здесь не запускается.

В репозитории DirectoryService, из каталога `backend`:

```sh
dotnet test tests/DirectoryService.IntegrationTests/DirectoryService.IntegrationTests.csproj --filter FullyQualifiedName~AssetStateHandlerTests
```

Эти тесты вызывают настоящие обработчики с PostgreSQL и проверяют повторный Ready,
повторный Deleted, поздний Ready после Deleted и конкурентную обработку.
Они проверяют бизнес-идемпотентность, а не дедупликацию envelope в inbox Wolverine.
Как и существующие тесты DirectoryService, создают текущую модель через EnsureCreated.

## Объём учебного задания

Реализован согласованный сценарий жизненного цикла ассетов и реакции DirectoryService.
`VideoProcessingCompleted` / `VideoProcessingFailed` не реализованы по договорённости:
они остаются отличием от полного исходного задания. `AssetReady` для видео означает
готовность загруженного исходника, а не результат обработки видео.
