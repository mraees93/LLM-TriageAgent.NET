# 🤖 LLM-TriageAgent

> **Resilient Event-Driven Support Dashboard with Background AI Triage Workers**

LLM-TriageAgent is a decoupled, API-first distributed software platform designed to manage and classify technical DevOps support tickets automatically. By offloading heavy AI text analysis and classification workflows away from the web browser client, the system guarantees a highly responsive user experience. 

The application is built using a modern **C# .NET Web API** gateway, a **React Vite TypeScript** frontend dashboard, and an asynchronous background worker network executing automated triage labels utilizing both local large language models and cloud simulators.

---

## 🏛️ Advanced System Design Architecture Pillars

To demonstrate production-grade architectural patterns, this project implements three distinct distributed system design strategies:

### 1. Asynchronous Event-Driven Architecture (MassTransit Queue)
Rather than forcing a public web user to experience latency or browser freeze-ups while a large language model parses error strings, the web client API gateway remains blazingly fast. The incoming ticket payload is immediately written to the primary table and dispatched as an event onto a memory bus broker via **MassTransit**. An isolated backend worker thread picks up the message asynchronously to handle heavy computational logic safely in the background.

### 2. Service Idempotency Shields (Fault-Tolerant Duplicate Guard)
In high-traffic cloud environments, network packet retries can cause identical messages to enter an event queue multiple times. LLM-TriageAgent is hardened against data duplication and state corruption. Every background consumer runs an automatic telemetry state verification check first. If a ticket is already flagged as `Processing` or `Resolved`, the worker exits early—saving valuable database and processing cycles.

### 3. Automated Dead Letter Queue (DLQ) Fault Consumers
To prevent corrupted data blocks or downstream infrastructure outages (such as an offline AI endpoint) from permanently clogging your primary queue channels, the app incorporates self-healing message routing. If a background analysis task fails, MassTransit automatically wraps the message in an error envelope and routes it to a secondary **Fault Consumer**. This component safely intercepts the crash, updates the ticket state to `Failed` dynamically, logs a detailed system quarantine alert, and keeps the rest of the message highway fluid and functional.

---

## ⚙️ Tech Stack & Distributed Deployment Topology

* **Frontend UI Gateway:** React 19, Vite, TypeScript, Tailwind CSS (Hosted globally via Vercel Edge Server Networks)
* **Backend API Gateway & Workers:** C# .NET 10, Entity Framework Core, MassTransit (Containerized on Render Linux Nodes)
* **Persistent Enterprise Datastore:** PostgreSQL (Hosted via Aiven Datacenters)
* **Local Development Database:** SQLite 3 (Flat local transactional mock environment)
* **Local Intelligence Core:** Ollama Engine executing the `phi3` model locally on hardware graphics channels

---

## 🏎️ How to Simulate a Live Cloud Infrastructure Outage

This project features live, real-time UI modal components designed to intercept asynchronous network errors dynamically. To see the self-healing **Dead Letter Queue Fault Interceptor Modal** trigger over the live internet:

1. Open `LLM-TriageAgent.API/Services/TicketConsumer.cs`.
2. Locate the cloud production block (`if (isProductionCloud)`).
3. Intentionally introduce a hardware exception block at the top of that `try` block:
   ```csharp
   throw new Exception("CRITICAL ERROR: Cloud AI Model Gateway Timeout.");
   ```
4. Push the change to GitHub and deploy it to Render.
5. Open your live Vercel URL and submit a ticket. 

Because the backend event bus instantly explodes under the fake crash rule, MassTransit redirects the packet to your `TicketFaultConsumer`. The exact millisecond your frontend polling ticker reads the updated data row, **a prominent red "Critical Infrastructure Fault" Modal will pop up right over your screen** live to notify you that the system has safely quarantined the failure block!

---

## 🧪 Robust Automated Testing Matrix

The core business classification metrics, state mapping loops, and idempotency logic are fully guarded by an automated **xUnit testing framework** utilizing an isolated, thread-safe memory datastore simulation layer.

Run the test suite locally by executing:
```bash
dotnet test
```

### Automated Testing Matrix Coverage:
* `TicketFaultConsumer_Should_Quarantine_And_Fail_Ticket_When_Triggered` ✅
* `TicketConsumer_Should_Assign_Bug_Label_When_Description_Contains_404` ✅
* `TicketConsumer_Should_Assign_Investigate_Label_For_Generic_Errors` ✅
* `TicketConsumer_Should_Skip_Processing_If_Ticket_Is_Already_Resolved` ✅
