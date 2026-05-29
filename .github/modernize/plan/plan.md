# Modernization Plan: ZavaNotifyWorker Azure Modernization

**Project**: ZavaNotifyWorker

---

## Technical Framework

- **Language**: C# / .NET Framework 4.8
- **Framework**: Console worker application
- **Build Tool**: MSBuild (legacy .csproj)
- **Database**: None
- **Key Dependencies**: RabbitMQ.Client, System.Net.Mail

---

## Overview

> This migration modernizes ZavaNotifyWorker for Azure and cloud-native operation. The application currently runs as a legacy .NET Framework worker with RabbitMQ and SMTP-based notifications. The new architecture will:
>
> - Upgrade the application runtime to the latest .NET LTS for long-term support
> - Modernize messaging and email integrations to Azure-native managed services
> - Deploy the application to Azure as a cloud-native workload with managed identity
>
> The migration follows a phased approach: runtime upgrade, service modernization, security remediation, and Azure deployment.

---

## Migration Impact Summary

| Application | Original Service | New Azure Service | Authentication | Comments |
|-------------|------------------|-------------------|----------------|----------|
| ZavaNotifyWorker | .NET Framework 4.8 | .NET 10 (LTS) | N/A | Modernize runtime to latest framework |
| ZavaNotifyWorker | RabbitMQ | Azure Service Bus | Managed Identity | Cloud-native messaging modernization |
| ZavaNotifyWorker | SMTP relay | Azure Communication Services Email | Managed Identity | Cloud-native email modernization |
| ZavaNotifyWorker | Local/hosted runtime | Azure Container Apps | Managed Identity | Deploy modernized worker to Azure |
