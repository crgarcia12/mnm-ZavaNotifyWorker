# Configuration & Externalized Settings Inventory

Configuration is centralized in `app.config` with environment variable overrides, primarily for RabbitMQ and SMTP runtime behavior.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| app.config | XML app settings | /tmp/workspace/crgarcia12/mnm-ZavaNotifyWorker/app.config | Default runtime values |
| Environment variables | Process environment | Host/container environment | Overrides app.config values when present |
| Dockerfile | Container config | /tmp/workspace/crgarcia12/mnm-ZavaNotifyWorker/Dockerfile | Build/runtime image and startup command |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Release | `msbuild /p:Configuration=Release` | Container build output | Mono msbuild + NuGet restore |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | Standard process startup | app.config | RabbitMQ and SMTP defaults |
| Environment-overridden | Environment variables at runtime | app.config + env | Host/port/queue/credentials/poll interval |

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| RabbitMQ.Host / `RABBITMQ_HOST` | rabbitmq | Default, env-overridden | app.config, env |
| RabbitMQ.Port / `RABBITMQ_PORT` | 5672 | Default, env-overridden | app.config, env |
| RabbitMQ.VHost / `RABBITMQ_VHOST` | /zavabank | Default, env-overridden | app.config, env |
| RabbitMQ.User / `RABBITMQ_USER` | zava_app | Default, env-overridden | app.config, env |
| RabbitMQ.Password / `RABBITMQ_PASSWORD` | [MASKED] | Default, env-overridden | app.config, env |
| RabbitMQ.NotificationQueue / `NOTIFICATIONS_QUEUE` | q.notify.email | Default, env-overridden | app.config, env |
| RabbitMQ.DeadLetterExchange / `RABBITMQ_DLX` | zava.dlx | Default, env-overridden | app.config, env |
| Worker.PollIntervalMs / `WORKER_POLL_INTERVAL_MS` | 5000 | Default, env-overridden | app.config, env |
| Smtp.Host / `SMTP_HOST` | mailhog | Default, env-overridden | app.config, env |
| Smtp.Port / `SMTP_PORT` | 1025 | Default, env-overridden | app.config, env |
| Smtp.From / `SMTP_FROM` | no-reply@zavabank.local | Default, env-overridden | app.config, env |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| ZavaNotifyWorker | None explicitly defined in repo | Not specified | Not specified |

## Startup Dependency Chain

1. RabbitMQ should be available before worker polling starts.
2. SMTP endpoint should be reachable before processing messages.
3. Worker starts main loop and polls queue at configured interval.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `RABBITMQ_PASSWORD` / `RabbitMQ.Password` | Queue credential | Environment variable or app.config (masked) |

### Secrets Provisioning Workflow

Secrets are expected from deployment environment variables (or fallback app.config values). The worker resolves environment values first, then local configuration, and binds credentials when creating RabbitMQ and SMTP client connections.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| None detected | N/A | N/A |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework target | net48 | ZavaNotifyWorker.csproj |
| RabbitMQ.Client | 5.2.0 | packages.config |
| Docker base image | mono:6.12 | Dockerfile |
