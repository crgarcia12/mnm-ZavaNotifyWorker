# Architecture Diagram

This worker application processes queued notification events and sends outbound emails. The architecture is a single-process background service with messaging and SMTP integrations.

## Application Architecture

```mermaid
flowchart TD
    subgraph Producer["Producer Systems"]
        TxnSvc["Transaction Producers"]
    end
    subgraph App["Application Layer - .NET Framework 4.8 Worker"]
        Worker["Notification Worker Loop"]
        Parser["JSON and XML Parser"]
        Mailer["SMTP Sender"]
    end
    subgraph Data["Messaging Layer"]
        Queue[("RabbitMQ Queue q.notify.email")]
        DLX[("RabbitMQ Dead Letter Exchange")]
    end
    subgraph External["External Services"]
        SMTP["SMTP Server mailhog"]
    end

    TxnSvc -->|"publish notification events"| Queue
    Worker -->|"poll and consume"| Queue
    Worker -->|"parse payload"| Parser
    Parser -->|"validated notification message"| Mailer
    Mailer -->|"send notification email"| SMTP
    Worker -->|"nack on failure"| DLX
```

### Technology Stack Summary

| Layer | Technology | Version | Purpose |
|---|---|---|---|
| Runtime | .NET Framework Console App | v4.8 | Background worker host |
| Messaging | RabbitMQ.Client | 5.2.0 | Queue polling and ack/nack handling |
| Configuration | app.config + environment variables | N/A | Runtime settings and overrides |
| Integration | System.Net.Mail SMTP | .NET BCL | Email notification delivery |

### Data Storage & External Services

The application uses RabbitMQ as its durable message source and dead-letter flow target. It integrates with an SMTP host for outbound message delivery and does not persist domain data in a database.

### Key Architectural Decisions

- Uses pull-based queue consumption (`BasicGet`) with manual acknowledgements for explicit success/failure handling.
- Supports dual message formats (JSON and XML) to absorb producer variability.
- Prioritizes simple deployment by keeping all processing in a single worker process.

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
        MQ["RabbitMQ Channel"]
    end
    subgraph Infra["Infrastructure"]
        Config["LoadConfig Env AppSettings"]
        Factory["CreateFactory"]
        Log["Console Logging"]
    end

    Main -->|"loads config"| Config
    Main -->|"poll loop"| Poll
    Poll -->|"creates connection"| Factory
    Poll -->|"basic get ack nack"| MQ
    Poll -->|"parses payload"| Parse
    Parse -->|"returns model"| Send
    Send -->|"sends mail"| Log
    Log -.->|"execution telemetry"| Main
```

### Component Inventory

| Component | Layer | Type | Responsibility |
|---|---|---|---|
| Program.Main | Presentation | Entry Point | Starts worker lifecycle and signal handlers |
| LoadConfig | Infrastructure | Config Loader | Binds environment/appSettings values |
| CreateFactory | Infrastructure | Connection Factory | Builds RabbitMQ connection settings |
| PollQueue | Business Logic | Orchestrator | Retrieves messages and controls ack/nack |
| ParseNotificationMessage | Business Logic | Parser Router | Chooses JSON or XML parser path |
| ParseJson / ParseXml | Business Logic | Translators | Converts payload into NotificationMessage |
| SendEmail | Business Logic | Integration Service | Sends SMTP messages to recipients |
| RabbitMQ Channel | Data Access | Message Client | Queue declare/get/ack/nack operations |
