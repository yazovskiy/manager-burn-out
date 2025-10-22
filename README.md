# Call Wellbeing Monitor

Сервис выявляет «молчащих менеджеров» и риск перегрузки по паттернам звонков, комбинируя аналитику речевых метрик и LLM-оценку.

## Архитектура

```mermaid
graph TD
  subgraph Worker
    PC[CallProcessor]
    MS[MetricsService]
    AS[AnomalyService]
    LC[LlmClient]
  end
  subgraph Infra
    DB[(SQLite / PostgreSQL)]
    EX[Exolve API]
    GC[GigaChat API]
  end
  subgraph API
    AlertsEndpoint[/GET /alerts/]
    MetricsEndpoint[/GET /metrics/last/]
    StatsEndpoint[/GET /managers/{id}/stats/]
  end

  PC -->|Transcript| EX
  EX -->|Segments| PC
  PC --> MS --> DB
  MS --> AS
  PC --> LC --> GC
  PC -->|Alerts| DB
  API --> DB
  API -->|Stream alerts & metrics| Clients
```

## Стек

- .NET 8, C# 12, Minimal API + BackgroundService
- EF Core с SQLite (dev) и PostgreSQL (prod)
- HttpClient интеграции с Exolve и GigaChat
- Serilog, структурированные логи, OpenTelemetry экспортеры
- IOptions/Options pattern, строгая конфигурация
- Docker + docker-compose, GitHub Actions (сборка, тесты, публикация контейнеров)

## Быстрый старт (локально)

```bash
# 1. Настроить переменные окружения
export ConnectionStrings__Default="Data Source=./data/callwellbeing.db"
export CALLWELLBEING_SEED=true

# 2. Запустить воркер (обработка очереди + сидинг данных)
dotnet run --project src/CallWellbeing.Worker/CallWellbeing.Worker.csproj

# 3. Во второй консоли запустить API
dotnet run --project src/CallWellbeing.Api/CallWellbeing.Api.csproj
```

API будет доступен на `http://localhost:5183` (порт из вывода `dotnet run`). Для демонстрации достаточно встроенного синтетического транскрайба и LLM-заглушек (включаются, если ключи не заданы).

Быстро выполнить только сидинг можно скриптом `./scripts/seed.sh` (использует тот же воркер в режиме `--seed-only`).

## Docker и docker-compose

```bash
# Собрать образ API (по умолчанию)
docker build -t callwellbeing-api .

# Собрать образ воркера
docker build -t callwellbeing-worker . \
  --build-arg PROJECT=src/CallWellbeing.Worker/CallWellbeing.Worker.csproj

# Запуск всего стека (API + Worker + PostgreSQL)
docker compose up --build
```

Стандартные переменные окружения для контейнеров задаются в `docker-compose.yml`. Для загрузки демо-данных оставьте `CALLWELLBEING_SEED=true`.

## Конфигурация

| Секция | Ключ | Назначение |
| ------ | ---- | ---------- |
| `ConnectionStrings:Default` | строка подключения | SQLite `Data Source=...` или PostgreSQL `Host=...` |
| `Exolve:{BaseUrl,ApiKey,AppId}` | доступ к службе транскрибаций | При отсутствии ключей включается синтетический генератор |
| `GigaChat:{BaseUrl,OAuthToken,Model}` | настройка LLM-клиента | При отсутствии токена создаётся низкий риск по умолчанию |
| `Anomaly:{PauseShare,LongCallMin,MinActions,UnansweredShare,Sigma}` | пороги детектора аномалий | см. `appsettings.json` |
| `Worker:{PollingIntervalSeconds,BatchSize}` | частота и размер пачки воркера | управление обработкой очереди |
| `CALLWELLBEING_SEED` | bool | при `true` выполняется первичное наполнение БД и очереди |

## REST API

```bash
# Список алертов по менеджеру
curl "http://localhost:8080/alerts?managerId=<uuid>"

# Последняя метрика
curl "http://localhost:8080/metrics/last?managerId=<uuid>"

# Агрегированные статистики
curl "http://localhost:8080/managers/<uuid>/stats"
```

Ответы возвращаются в JSON с флагами, долями речи, паузами, Z-score-индикаторами и т.д.

## Интеграции

### Exolve (получить транскрибацию)

```bash
curl -X POST \
  "https://api.exolve.ru/statistics/call-record/v1/GetTranscribation" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $EXOLVE_API_KEY" \
  -d '{
        "call_id": "12345",
        "app_id": "${EXOLVE_APP_ID}"
      }'
```

Ожидается, что сервис вернёт массив сегментов: `{ "segments": [{ "speaker": "manager", "start_ms": 0, ... }] }`.

### GigaChat (LLM оценка)

```bash
curl -X POST \
  "$GIGACHAT_BASE/api/v1/chat/completions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $GIGACHAT_TOKEN" \
  -d '{
        "model": "GigaChat-Pro",
        "messages": [
          {"role": "system", "content": "Ты ассистент службы заботы. Верни JSON {\"risk\",\"why\",\"advice\"}"},
          {"role": "user", "content": "... статистика и отрывок ..."}
        ]
      }'
```

Ответ должен быть в формате JSON `{ "risk": "средний", "why": "...", "advice": "..." }`.

## Тесты

```bash
dotnet test --configuration Release
```

## Безопасность и логирование

- Идентификаторы звонков хешируются (SHA-256) перед логированием и записью.
- Тексты транскриптов не логируются.
- Секреты (ключи API) никогда не попадают в логи — используются маски и fallback-режимы.
- Serilog конфигурирован на компактный JSON, легко экспортируемый в централизованные системы.
