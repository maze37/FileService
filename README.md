# FileService

Сервис хранения и жизненного цикла файлов платформы. Принимает метаданные,
выдаёт подписанные ссылки для загрузки и скачивания, хранит состояние ассетов
в PostgreSQL и сообщает другим сервисам о готовности и удалении файлов.
Байты файлов хранятся в S3-совместимом хранилище; локально используется MinIO.

## Цели проекта

- Вынести работу с файлами в отдельный сервис с единым API.
- Поддерживать обычную и multipart-загрузку без передачи содержимого файла через API приложения.
- Разделить метаданные, физическое хранение и бизнес-сущность, к которой относится файл.
- Дать другим сервисам стабильный `AssetId`, а клиентам — временные ссылки на содержимое.
- Надёжно доставлять факты изменения состояния через RabbitMQ, даже если брокер временно недоступен.
- На практике реализовать транзакции, outbox, интеграционные контракты и проверку отказов.

FileService отвечает за ассет и его содержимое. DirectoryService отвечает за
локацию и решение о том, какой ассет прикреплён к ней. FileService хранит ссылку
на владельца (`context`, `entityId`), но не управляет самой локацией.

## Навигация

- [Архитектура](#архитектура)
- [GitHub-токен](#github-токен-и-приватные-nuget-пакеты)
- [Запуск](#запуск)
- [API и загрузка](#api-и-загрузка)
- [Асинхронные события](#асинхронные-события)
- [Тесты](#тесты)
- [Диагностика](#диагностика)

## Архитектура

| Проект | Ответственность |
| --- | --- |
| `FileService.Web` | ASP.NET Core API, контроллеры, Swagger, конфигурация приложения |
| `FileService.Core` | Сценарии загрузки/удаления/чтения, интерфейсы, настройка Wolverine |
| `FileService.Domain` | Ассеты, типы, статусы и правила переходов |
| `FileService.Infrastructure.Postgres` | EF Core, репозитории, транзакции, outbox и миграции |
| `FileService.Infrastructure.S3` | S3/MinIO, presigned URL, multipart, создание бакетов |
| `FileService.Contracts` | HTTP DTO и клиент взаимодействия |
| `FileService.Shared.Messaging` | Контракты событий и правила маршрутизации |
| `tests/FileService.IntegrationTests` | Интеграционные сценарии с Testcontainers |

Стек: .NET 10, ASP.NET Core, EF Core/Npgsql, PostgreSQL, Wolverine, RabbitMQ,
MinIO, Redis/HybridCache, Serilog/Seq, xUnit, Testcontainers и Respawn.
Версии пакетов управляются в `Directory.Packages.props`.

```mermaid
flowchart LR
    Client[Клиент] -->|Метаданные и завершение загрузки| FS[FileService]
    FS -->|Presigned URL| Client
    Client -->|PUT содержимого| S3[MinIO / S3]
    FS -->|Метаданные и outbox| DB[(PostgreSQL files)]
    DB -->|Wolverine| MQ[RabbitMQ file-events]
    MQ --> Q[directory.asset-events]
    Q --> DS[DirectoryService]
```

### Модель ассета

`MediaAsset` содержит идентификатор, владельца, метаданные, ключ хранилища и статус.
В домене есть видео, аудио, документы и изображения/превью.

| Назначение | Бакет |
| --- | --- |
| `VIDEO` | `videos` |
| `AUDIO` | `audio` |
| `DOCUMENT` | `documents` |
| `AVATAR`, `COVER`, `PREVIEW`, `THUMBNAIL` | `previews` |

В текущем преобразовании входных строк `avatar`, `cover`, `preview`, `thumbnail`
используется `PREVIEW`. Не следует ожидать сохранения каждого из этих входных
названий как отдельного типа в событии.

Основной путь статусов: `PENDING` → `UPLOADING` → `UPLOADED`.
Также предусмотрены `CANCELLED` и `DELETED`; допустимость перехода проверяет домен.
`UPLOADED` означает завершённую загрузку, а не обработанное/перекодированное видео.

Поддерживаемые контексты владельца: `location`, `lesson`, `module`, `user`,
`department`, `course`. `entityId` должен быть непустым GUID.

## GitHub-токен и приватные NuGet-пакеты

Оба сервиса используют пакеты `Mazeland.*` из GitHub Packages. Без доступа к ним
`dotnet restore`, сборка Docker-образа и запуск тестов могут завершиться ошибкой.
Источник `mazeland-private` уже задан в [nuget.config](nuget.config):

```text
https://nuget.pkg.github.com/maze37/index.json
```

Для локальной разработки создайте **Personal access token (classic)** с правом
`read:packages`. Владелец токена также должен иметь доступ к приватным пакетам;
само наличие токена этот доступ не выдаёт. Для скачивания не нужны права публикации
или удаления пакетов. См. [документацию GitHub Packages](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry).

### Для Docker Compose

Создайте `.env` рядом с `docker-compose.yml` в каждом проекте:

```dotenv
GITHUB_TOKEN=your_personal_access_token
```

Подставьте собственный токен вместо примера. `.env` исключён из Git.
Compose подставляет переменную в `build.args.GITHUB_TOKEN`; Dockerfile использует
её при восстановлении зависимостей. Токен нужен на этапе сборки.

### Для dotnet CLI и IDE

Compose читает `.env` автоматически, а `dotnet restore` — нет. Перед запуском CLI
передайте токен в окружение текущего терминала. Если `.env` создан вами и содержит
обычные доверенные присваивания переменных:

```sh
set -a
. ./.env
set +a
dotnet restore
```

В `nuget.config` пароль задан как `%GITHUB_TOKEN%`: NuGet подставляет значение
переменной окружения. Для IDE задайте ту же переменную в окружении процесса,
выполняющего restore; настройка только профиля запуска приложения может не влиять
на восстановление пакетов.

В текущем конфиге указан username `maze37`. При использовании другого GitHub-аккаунта
согласуйте username с владельцем токена; в Dockerfile DirectoryService username
также задан явно. Не меняйте адрес feed на свой username: пакеты размещены у `maze37`.

Не записывайте настоящий токен в README, исходники или коммиты. Текущие Dockerfile
передают секрет через build argument, а DirectoryService записывает credentials
на этапе сборки. Для CI и распространяемых build cache стоит перейти на BuildKit
secrets. Имя переменной `GITHUB_TOKEN` в локальном проекте обозначает ваш PAT;
автоматический токен GitHub Actions — отдельный механизм с собственными правами.

При `401/403` проверьте срок действия токена, `read:packages`, доступ аккаунта к
пакету и наличие переменной именно в процессе restore/build.

## Запуск

### Требования

- Docker Engine/Desktop с Compose v2.
- .NET SDK 10 для локального запуска, миграций и тестов.
- Доступ к приватным NuGet-пакетам.
- Свободные порты из таблицы ниже.

Все команды этого README выполняются из корня FileService, если не указано иначе.

| Компонент | Адрес с компьютера | Адрес внутри Docker |
| --- | --- | --- |
| API / Swagger | `http://localhost:8003/swagger/index.html` | `http://file-service:8003` |
| PostgreSQL | `localhost:15434` | `postgres:5432` в сети FileService |
| RabbitMQ AMQP | `localhost:5672` | `platform-rabbitmq:5672` в общей сети |
| RabbitMQ UI | `http://localhost:15672` | — |
| MinIO API | `http://localhost:9000` | `http://minio:9000` |
| MinIO Console | `http://localhost:9001` | — |
| Redis | `localhost:6381` | `redis:6379` |
| Seq UI | `http://localhost:8082` | — |
| Seq ingestion | `http://localhost:5342` | `http://seq:5341` |

Локальные credentials из compose: PostgreSQL `postgres / 1234`, RabbitMQ
`guest / guest`, MinIO `minioadmin / minioadmin`. Это конфигурация разработки.

### Общая сеть и инфраструктура

Оба compose используют внешнюю сеть `shared-network`. Создайте её один раз:

```sh
docker network create shared-network
```

Если сеть уже существует, повторное создание не требуется.

```sh
docker compose up -d postgres rabbitmq minio redis seq
docker compose ps
```

RabbitMQ поднимается только в FileService. DirectoryService подключается к этому
же брокеру через alias `platform-rabbitmq`; второй экземпляр на порту 5672 не нужен.

### Конфигурация локального процесса

Загрузите `GITHUB_TOKEN` в окружение, затем задайте подключения:

```sh
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Database='Host=localhost;Port=15434;Database=file_service_db;Username=postgres;Password=1234'
export ConnectionStrings__RabbitMq='amqp://guest:guest@localhost:5672'
export ConnectionStrings__Redis='localhost:6381'
export Serilog__WriteTo__1__Args__serverUrl='http://localhost:5342'
dotnet restore
```

Используется ключ **`Database`**, а не `FileServiceDb`. В development-конфиге
остались старые значения Redis/Seq, поэтому выше они явно переопределены под compose.
В контейнере `localhost` обозначает сам контейнер: compose уже задаёт внутренние адреса.

### База данных

Бизнес-миграции не применяются автоматически в текущем startup приложения.
Для новой базы примените их отдельно. Нужен установленный `dotnet-ef` версии 10.x:

```sh
dotnet ef database update \
  --project src/FileService.Infrastructure.Postgres \
  --startup-project src/FileService.Web
```

Метаданные находятся в `files.media_assets`. Wolverine использует ту же БД и
схему `files` для своих таблиц сообщений. Наличие таблиц Wolverine не заменяет
миграцию таблиц приложения.

### Приложение в Docker

После подготовки БД и `.env`:

```sh
docker compose up -d --build file-service
docker compose logs -f file-service
```

Откройте Swagger по адресу из таблицы. Compose явно задаёт порт 8003 через
`ASPNETCORE_URLS`; при запуске образа отдельно от compose учитывайте, что Dockerfile
пока содержит порт 8002.

### Приложение из IDE или CLI

Инфраструктура остаётся в Docker, приложение запускается на компьютере:

```sh
dotnet run --project src/FileService.Web --no-launch-profile --urls http://localhost:8003
```

Не запускайте одновременно контейнер API и локальный процесс на одном порту.
Для остановки контейнера API: `docker compose stop file-service`.

### Настройки хранилища

`FileStorageOptions:Endpoint` — адрес S3 для сервиса, `ExternalEndpoint` — адрес,
доступный клиенту, который использует подписанную ссылку. При локальном Docker-запуске
это соответственно `http://minio:9000` и `http://localhost:9000`.
Изменять hostname в готовом presigned URL нельзя: подпись может стать недействительной.
Если клиент работает на другом компьютере, `ExternalEndpoint` должен быть доступен ему.

При изменении MinIO credentials синхронно измените `FileStorageOptions__AccessKey`
и `FileStorageOptions__SecretKey` у приложения. Значения приложения в compose сейчас
заданы отдельно от `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD`.

## API и загрузка

Полные DTO, ограничения и ответы доступны в Swagger и `FileService.Contracts`.

| Метод | Маршрут | Назначение |
| --- | --- | --- |
| POST | `/api/files/upload/initiate` | Начать обычную загрузку |
| POST | `/api/files/upload/{id}/complete` | Подтвердить загрузку |
| GET | `/api/files/{id}` | Получить информацию и ссылку на файл |
| GET | `/api/files` | Получить файлы по целевой сущности |
| GET | `/api/files/{id}/exists` | Проверить существование и готовность |
| PATCH | `/api/files/{id}/cancel` | Отменить загрузку |
| DELETE | `/api/files/{id}` | Удалить ассет |
| POST | `/api/files/multipart/start` | Начать multipart-загрузку |
| POST | `/api/files/multipart/complete` | Завершить multipart-загрузку |
| POST | `/api/files/multipart/abort` | Прервать multipart-загрузку |

### Обычная загрузка

1. Клиент отправляет имя, MIME-тип, размер, назначение и владельца.
2. FileService возвращает `assetId`, `uploadUrl` и срок действия ссылки.
3. Клиент выполняет PUT байтов непосредственно в MinIO/S3.
4. Клиент вызывает `complete`.
5. FileService проверяет объект в хранилище, изменяет состояние и сохраняет событие.

Пример для изображения локации. Замените GUID на ID существующей локации,
а `fileSize` — на фактический размер `photo.jpg`:

```sh
curl -X POST http://localhost:8003/api/files/upload/initiate \
  -H 'Content-Type: application/json' \
  -d '{
    "fileName": "photo.jpg",
    "contentType": "image/jpeg",
    "fileSize": 1024,
    "context": "location",
    "entityId": "020aa11d-1792-451b-afe6-a927eab3747f",
    "assetType": "preview"
  }'
```

Из ответа скопируйте URL целиком и ID ассета:

```sh
curl -X PUT 'UPLOAD_URL_FROM_RESPONSE' \
  -H 'Content-Type: image/jpeg' --data-binary @photo.jpg
curl -X POST http://localhost:8003/api/files/upload/ASSET_ID/complete
```

Используйте MIME-тип, согласованный при инициализации. Один только PUT не переводит
ассет в готовое состояние: нужен успешный `complete`.

### Multipart

`multipart/start` возвращает upload ID и ссылки для частей. Клиент загружает части,
сохраняет их номера и ETag, затем передаёт `mediaAssetId`, `uploadId`, `partETags`
в `multipart/complete`. После успешного завершения публикуется тот же `AssetReady`.
Рабочий пример полного цикла есть в `MultipartUploadFileTests`.

## Асинхронные события

| Событие | Когда создаётся | Routing key |
| --- | --- | --- |
| `AssetReady` | Успешное завершение обычной/multipart-загрузки | `asset.ready.{entityType}` |
| `AssetDeleted` | Успешное изменение состояния на удалённое | `asset.deleted.{entityType}` |

Контракты содержат `AssetId`, `EntityId`, `EntityType`, `AssetType`, `OccurredAt`.
Они не содержат EF-сущностей, навигаций или постоянных download URL.
Namespace контрактов — `IntegrationEvents.Files.Events`.

Путь сообщения:

1. Use case изменяет ассет и вызывает `IOutboxService.PublishAsync`.
2. Изменение и envelope записываются в общей транзакции PostgreSQL.
3. После commit Wolverine доставляет сообщение в topic exchange `file-events`.
4. Binding `asset.*.*` направляет его в очередь `directory.asset-events`.
5. Wolverine в DirectoryService принимает сообщение и запускает обработчик.

Создание record само по себе ничего не отправляет. `PublishAsync` вызывается явно.
Outbox защищает промежуток между commit БД и отправкой брокеру; таблица
`files.wolverine_outgoing_envelopes` очищается по мере успешной доставки.
Пустая таблица после доставки — нормальное состояние, а не признак отсутствия публикации.

**Очередь и binding должны существовать до первой публикации.** Для локальной
проверки сначала запустите DirectoryService и убедитесь, что он создал очередь.
Exchange сам не хранит события для будущих подписчиков.

Согласованность с MinIO не является общей транзакцией PostgreSQL/S3. Например,
удаление объекта выполняется отдельно; неудача очистки логируется после изменения
состояния ассета. `AssetDeleted` означает логическое удаление.

## Тесты

```sh
dotnet test tests/FileService.IntegrationTests/FileService.IntegrationTests.csproj
```

Только события/outbox:

```sh
dotnet test tests/FileService.IntegrationTests/FileService.IntegrationTests.csproj \
  --filter FullyQualifiedName~AssetEventsTests
```

Тесты создают отдельные PostgreSQL, MinIO и RabbitMQ на случайных портах.
Кэш заменяется памятью процесса. Рабочие контейнеры приложения не требуются.
Respawn очищает бизнес-таблицу, не удаляя служебное состояние работающего Wolverine.

Проверяются обычная и multipart-загрузка, чтение, отмена, удаление, публикация
Ready/Deleted, откат данных вместе с сообщением, сохранение outbox при отключённом
брокере и доставка после его восстановления. Тест отказа выключает только свой
тестовый RabbitMQ. Последний проверенный запуск: **31 успешный тест**.

Транспорт проверяется через отдельную очередь `tests.asset-events` и RabbitMQ
management API. Эти тесты не запускают реальный DirectoryService: его обработчики
проверяются в его собственном репозитории.

## Диагностика

```sh
docker compose ps
docker compose logs --tail=150 file-service rabbitmq
docker compose exec rabbitmq rabbitmq-diagnostics -q ping
```

| Симптом | Что проверить |
| --- | --- |
| Restore возвращает 401/403 | GitHub PAT, права, feed, окружение процесса |
| Port is already allocated | Порт занят другим compose/процессом; проверить опубликованные порты |
| Swagger не открывается | Логи API, порт 8003, успешный старт Wolverine и подключения |
| `database.error` | Исключение в логах, ключ `Database`, применённые миграции |
| Нет сообщений в очереди | Существуют ли binding и очередь, одинаковы ли broker/vhost, вызван ли complete |
| Ready/Unacked равны нулю | Consumer мог уже забрать событие; проверить DirectoryService и inbox |
| Нет логов в Seq | С компьютера ingestion 5342, из контейнера `seq:5341` |
| Presigned URL возвращает 403 | Срок ссылки, подпись, hostname, MIME-тип и credentials |

SQL для проверки в БД FileService:

```sql
SELECT id, status FROM files.media_assets ORDER BY id DESC LIMIT 20;
SELECT * FROM files.wolverine_outgoing_envelopes LIMIT 20;
SELECT * FROM files.wolverine_dead_letters LIMIT 20;
```

`docker compose down` останавливает окружение, сохраняя именованные volumes.
`docker compose down -v` удаляет данные volumes — это сброс окружения, а не обычная остановка.

## Текущие границы реализации

Реализованы lifecycle-события ассетов и взаимодействие с DirectoryService.
Обработка/перекодирование видео и события `VideoProcessingCompleted` /
`VideoProcessingFailed` не реализованы в согласованном объёме учебного задания.
Получение свежего content URL остаётся синхронным HTTP-запросом.
