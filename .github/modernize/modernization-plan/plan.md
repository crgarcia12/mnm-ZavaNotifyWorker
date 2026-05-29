# Modernization Plan: modernization-plan

**Project**: ZavaNotifyWorker

---

## Technical Framework

- **Language**: C# 7.3 on .NET Framework 4.8
- **Framework**: .NET Framework console worker
- **Build Tool**: MSBuild / NuGet and Docker
- **Database**: None identified in the repository
- **Key Dependencies**: RabbitMQ.Client 5.2.0, System.Net.Mail,
  System.Configuration, System.Web.Extensions

---

## Overview

> This migration moves the notification worker from self-managed messaging,
> email, and hosting dependencies to Azure-managed services. The application
> currently runs as a legacy .NET Framework worker that polls RabbitMQ,
> sends mail through SMTP, and builds in a Mono-based container. The new
> architecture will:
>
> - move queue processing to Azure-managed messaging for simpler operations
> - move email delivery to an Azure-managed service for cloud-aligned delivery
> - host the worker on Azure Container Apps for managed container execution
>
> The migration follows a phased approach that updates Azure-facing
> integrations first, remediates security issues next, and deploys the worker
> to Azure after the application changes are complete.

---

## Migration Impact Summary

| Application | Original Service | New Azure Service | Authentication | Comments |
|-------------|------------------|-------------------|----------------|----------|
| ZavaNotifyWorker | RabbitMQ | Azure Service Bus | Managed Identity | Queue processing |
| ZavaNotifyWorker | SMTP relay | Azure Communication Services Email | Managed Identity | Email delivery |
| ZavaNotifyWorker | Mono container | Azure Container Apps | Managed Identity | Deployment target |

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include environment/infrastructure provisioning? →
  A: No — focus on code migration only because no infrastructure request or
  existing IaC scope was provided.
- [x] Q: Should the plan include integration testing to verify migrated
  services? → A: No standalone integration-test task was added because the
  request did not explicitly ask for integration testing.
- [x] Q: Should the plan include a security scan and CVE remediation task? →
  A: Yes — include security and CVE remediation by default.
- [x] Q: Which Azure deployment target should the plan use? →
  A: Azure Container Apps by default.
- [x] Q: Should the plan include containerization? →
  A: Containerization is covered by the Azure Container Apps deployment task,
  so no separate containerization task is needed.
