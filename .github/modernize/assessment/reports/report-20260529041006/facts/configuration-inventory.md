# Configuration & Externalized Settings Inventory

This project externalizes runtime behavior through environment variables and app.config application settings, with a minimal single-service configuration footprint.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| app.config | XML appSettings | /tmp/workspace/crgarcia12/mnm-ZavaNotifyWorker/app.config | Default runtime values for RabbitMQ, worker, SMTP |
| Environment Variables | Process environment | Runtime host/container | Overrides app.config defaults |
| ZavaNotifyWorker.csproj | Build config | /tmp/workspace/crgarcia12/mnm-ZavaNotifyWorker/ZavaNotifyWorker.csproj | Debug/Release configurations and target framework |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | Build configuration | Local development build output and symbols | .NET Framework build targets |
| Release | Build configuration | Optimized production build output | .NET Framework build targets |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | Implicit | app.config | RabbitMQ, queue, worker interval, SMTP settings |
| Environment Override | Set env vars | app.config + environment | Env vars take precedence over appSettings |

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| RabbitMQ.Host / RABBITMQ_HOST | rabbitmq | Default, Env Override | app.config / env |
| RabbitMQ.Port / RABBITMQ_PORT | 5672 | Default, Env Override | app.config / env |
| RabbitMQ.VHost / RABBITMQ_VHOST | /zavabank | Default, Env Override | app.config / env |
| RabbitMQ.User / RABBITMQ_USER | zava_app | Default, Env Override | app.config / env |
| RabbitMQ.Password / RABBITMQ_PASSWORD | [MASKED] | Default, Env Override | app.config / env |
| RabbitMQ.NotificationQueue / NOTIFICATIONS_QUEUE | q.notify.email | Default, Env Override | app.config / env |
| RabbitMQ.DeadLetterExchange / RABBITMQ_DLX | zava.dlx | Default, Env Override | app.config / env |
| Worker.PollIntervalMs / WORKER_POLL_INTERVAL_MS | 5000 | Default, Env Override | app.config / env |
| Smtp.Host / SMTP_HOST | mailhog | Default, Env Override | app.config / env |
| Smtp.Port / SMTP_PORT | 1025 | Default, Env Override | app.config / env |
| Smtp.From / SMTP_FROM | no-reply@zavabank.local | Default, Env Override | app.config / env |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| ZavaNotifyWorker | None explicitly configured | Not specified | Not specified |

## Startup Dependency Chain

1. ZavaNotifyWorker → waits for → RabbitMQ availability (connection open during poll cycle)
2. ZavaNotifyWorker → waits for → SMTP host availability (required when sending notifications)

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| RabbitMQ.Password / RABBITMQ_PASSWORD | Message broker credential | app.config/env ([MASKED]) |

### Secrets Provisioning Workflow

Secrets are expected from environment variables at runtime, with app.config fallback defaults present for local-style execution. The worker process reads the value at startup and uses it to establish RabbitMQ connectivity.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| None detected | N/A | N/A |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework | 4.8 | ZavaNotifyWorker.csproj |
| C# Language Version | 7.3 | ZavaNotifyWorker.csproj |
| RabbitMQ.Client | 5.2.0 | packages.config |
