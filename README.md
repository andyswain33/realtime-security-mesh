# Real-Time Security Event Mesh

An enterprise-grade, high-concurrency event brokering gateway designed for modern Physical Access Control Systems (PACS). 

This project demonstrates a decoupled, highly scalable architecture capable of ingesting, processing, and visualizing thousands of concurrent IoT security events (e.g., people counting, door forced alarms, occupancy sensors) in real-time. It is built to the performance and reliability standards required by global ecosystems like SwiftConnect and ASSA ABLOY.

## 🚀 The Mission & Constraints
- **High Throughput:** Handle 100+ events per second, per sensor, without applying backpressure to the edge devices.
- **Guaranteed Delivery:** Ensure "at-least-once" message processing using manual acknowledgments and robust Dead Letter Exchange (DLX) routing.
- **Targeted Telemetry:** Broadcast real-time updates to web clients utilizing specific SignalR group subscriptions to prevent network saturation.

## 🏗️ Architecture Overview

The system strictly adheres to Clean Architecture principles, separating the domain logic from infrastructure and presentation concerns. 

* **Gateway.Core:** The enterprise domain model. Contains cross-cutting interfaces, core security event entities, and custom exceptions.
* **Gateway.Ingress (Producer):** A lightweight .NET 10 Web API. Acts as the front door for IoT devices. It performs rapid, non-blocking payload serialization and immediately publishes to the message broker, returning an `HTTP 202 Accepted`.
* **Gateway.Processor (Consumer):** A scalable .NET 10 Background Worker Service. It consumes the RabbitMQ queues, processes the business logic (e.g., maintaining global occupancy state), and pushes telemetry via a SignalR client.
* **Gateway.Dashboard (SignalR):** A real-time web hub utilizing the SignalR "Group" pattern, allowing frontend clients to subscribe exclusively to the telemetry of the Security Zones they are actively monitoring.

### 🛡️ Security & Persistence Posture
Event gateways are prime targets for malicious payloads. To protect downstream data stores and audit logs, the ingestion pipeline avoids fragile Regex word-boundary checking for payload sanitization. Instead, it utilizes Microsoft's `Microsoft.SqlServer.TransactSql.ScriptDom` library to parse SQL-bound telemetry into an Abstract Syntax Tree (AST), making command injection mathematically impossible before data reaches the database.

## ⚙️ Technology Stack
- **Framework:** .NET 10 (Minimal APIs, Worker Services, SignalR)
- **Messaging Broker:** RabbitMQ (Topic Exchanges, DLX topology)
- **Caching / Backplane:** Redis (Alpine)
- **Containerization:** Docker & Docker Compose
- **Frontend:** Vanilla HTML/JS with SignalR Client

## 🛠️ Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Node.js](https://nodejs.org/) (For serving the frontend UI)

### 1. Local Infrastructure Setup
The required infrastructure (RabbitMQ with Management UI and Redis) is containerized for immediate local development.

```bash
# Spin up the broker and cache
docker-compose up -d
```
*Verify RabbitMQ is running at `http://localhost:15672` (User: `admin`, Password: `AdminPacs2026!`)*

### 2. Running the Microservices
For local testing, run the three .NET services simultaneously. You can use this PowerShell one-liner from the repository root to launch them in separate console windows:

```powershell
Start-Process "dotnet" -ArgumentList "run --project src/Gateway.Dashboard" ; Start-Process "dotnet" -ArgumentList "run --project src/Gateway.Processor" ; Start-Process "dotnet" -ArgumentList "run --project src/Gateway.Ingress"
```

### 3. Starting the Real-Time UI
Navigate to the `client` directory and start a local web server to view the dashboard:

```bash
cd client
npx serve -p 3000
```
Open your browser to `http://localhost:3000` and click "Monitor Zone" to connect to the SignalR Hub.

## ⚡ Load Testing & Concurrency Validation

To validate the non-blocking nature of the `Gateway.Ingress` API and the throughput of the RabbitMQ consumer, a stress-test script is included. This script utilizes `System.Net.Http.HttpClient` to fire 500 asynchronous, concurrent requests directly at the ingestion endpoint.

**To run the test:**
1. Ensure all services and the UI are running.
2. Open a PowerShell window at the repository root.
3. Execute the following script:

```powershell
$apiUrl = "http://localhost:5000/api/events/telemetry"
$totalEvents = 500

$httpClient = [System.Net.Http.HttpClient]::new()
$tasks = @()

Write-Host "Queuing $totalEvents concurrent requests..." -ForegroundColor Yellow

for ($i = 1; $i -le $totalEvents; $i++) {
    $zone = if ($i % 3 -eq 0) { "Server Room" } else { "Main Lobby" }
    $occupancyDelta = Get-Random -Minimum -2 -Maximum 5
    $eventType = if ($i % 100 -eq 0) { 1 } else { 0 } 

    $jsonPayload = @"
    {
        "EventId": "$([guid]::NewGuid().ToString())",
        "ZoneId": "$zone",
        "DeviceId": "Simulated-Sensor-$($i % 10)",
        "Type": $eventType,
        "Timestamp": "$( (Get-Date).ToUniversalTime().ToString("o") )",
        "OccupancyDelta": $occupancyDelta
    }
"@

    $content = [System.Net.Http.StringContent]::new($jsonPayload, [System.Text.Encoding]::UTF8, "application/json")
    $tasks += $httpClient.PostAsync($apiUrl, $content)
}

[System.Threading.Tasks.Task]::WaitAll($tasks)
Write-Host "Stress test complete! $totalEvents events blasted to the Ingress API." -ForegroundColor Green
$httpClient.Dispose()
```

**Expected Result:** The API will instantly absorb all 500 payloads returning HTTP 202s, while the background worker systematically drains the RabbitMQ queue and updates the web UI in real-time without freezing.

## 📂 Project Structure
```text
realtime-security-mesh/
├── docker-compose.yml           # Local infrastructure definition
├── client/
│   └── index.html               # Vanilla JS SignalR Dashboard
└── src/
    ├── SecurityEventMesh.sln    
    ├── Gateway.Core/            # Enterprise Domain Models & AST Security
    ├── Gateway.Ingress/         # Edge API & RabbitMQ Producer
    ├── Gateway.Processor/       # RabbitMQ Consumer & State Manager
    └── Gateway.Dashboard/       # SignalR Real-Time Hub
```