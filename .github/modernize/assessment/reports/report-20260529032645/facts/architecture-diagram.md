# Architecture Diagram

This repository contains a single worker service that consumes notification events and sends SMTP emails. The architecture is queue-driven with external RabbitMQ and SMTP dependencies.

## Application Architecture

```mermaid
flowchart TD
    subgraph Producer["Event Producers"]
        TxSvc["Transaction Systems"]
    end
    subgraph App["Application Layer - .NET Framework Worker"]
        Worker["Notify Worker\nProgram.cs"]
        Parser["JSON/XML Parser"]
        Mailer["SMTP Sender"]
    end
    subgraph Messaging["Messaging Layer"]
        Queue[("RabbitMQ queue q.notify.email")]
        DLX[("RabbitMQ dead-letter exchange zava.dlx")]
    end
    subgraph External["External Services"]
        SMTP[("SMTP server / Mailhog")]
    end

    TxSvc -->|"publish notifications"| Queue
    Worker -->|"BasicGet"| Queue
    Worker -->|"parse payload"| Parser
    Parser -->|"notification model"| Mailer
    Mailer -->|"send email"| SMTP
    Worker -->|"nack failures"| DLX
```

### Technology Stack Summary

| Layer | Technology | Version | Purpose |
|---|---|---|---|
| Runtime | .NET Framework | net48 | Executes worker process |
| Messaging | RabbitMQ.Client | 5.2.0 | Pull and acknowledge queue messages |
| Email | System.Net.Mail | .NET BCL | Deliver notification emails |
| Deployment | Docker + Mono | mono:6.12 | Containerized runtime |

### Data Storage & External Services

The service does not own a database. It integrates with RabbitMQ for inbound messages and an SMTP endpoint for outbound email delivery.

### Key Architectural Decisions

- Uses a polling loop with `BasicGet` and explicit ack/nack for message handling.
- Supports both JSON and XML payload parsing for compatibility.
- Routes failed message processing to a dead-letter exchange via queue arguments.

## Component Relationships

```mermaid
flowchart LR
    subgraph Presentation["Presentation"]
        Main["Program.Main"]
    end
    subgraph Business["Business Logic"]
        Poll["PollQueue"]
        Parse["ParseNotificationMessage"]
        Send["SendEmail"]
    end
    subgraph DataAccess["Data Access"]
        Rabbit["RabbitMQ Channel"]
        Msg["NotificationMessage"]
    end
    subgraph Infrastructure["Infrastructure"]
        Config["app.config + env vars"]
        SMTP2["SmtpClient"]
    end

    Main -->|"loop"| Poll
    Poll -->|"read"| Rabbit
    Poll -->|"decode body"| Parse
    Parse -->|"map model"| Msg
    Poll -->|"deliver"| Send
    Send -->|"uses"| SMTP2
    Main -.->|"loads"| Config
```

### Component Inventory

| Component | Layer | Type | Responsibility |
|---|---|---|---|
| Program.Main | Presentation | Entry Point | Starts lifecycle and graceful shutdown handling |
| PollQueue | Business Logic | Orchestrator | Pulls messages and controls ack/nack |
| ParseNotificationMessage | Business Logic | Parser | Detects JSON/XML and maps notification fields |
| SendEmail | Business Logic | Notifier | Formats email subject/body and sends message |
| RabbitMQ channel | Data Access | Messaging Client | Declares queue and receives message payload |
| app.config/env | Infrastructure | Configuration | Provides RabbitMQ/SMTP/runtime settings |
