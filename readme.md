# LLM-TriageAgent: Resilient AI-driven Architecture 

[![Automated Unit Testing Matrix](https://github.com/mraees93/LLM-TriageAgent.NET/actions/workflows/tests.yml/badge.svg)](https://github.com/mraees93/LLM-TriageAgent.NET/actions/workflows/tests.yml)

LLM-TriageAgent is a full-stack, Decoupled Distributed Architecture, Docker-containerised DevOps support ticket triage service designed for asynchronous scale, cloud resilience, and intelligent automation. Built with a modern .NET 10 backend and a React + Tailwind frontend, it leverages a decoupled MVC architecture and an event-driven messaging topology to offload heavy AI model analysis and classification pipelines away from the user interface.

---

## 🏛️ System Design & Distributed Architecture Principles Implemented

LLM-TriageAgent was engineered following modern distributed systems patterns to ensure fault tolerance, operational observability, and environmental portability:

1. **Decoupled Architecture (SoC)**: The system is split into distinct layers (Client Gateway, Application Broker, and Persistent Datastore) communicating via RESTful APIs. This ensures that frontend UI monitoring frames and backend workers scale independently.

2. **Asynchronous Event-Driven Message Queue**: To prevent slow AI inference loops or large language model token generation pipelines from freezing user screens, the API gateway utilizes **MassTransit** as an event bus broker. Incoming support tickets are instantly saved to the primary datastore and dispatched onto an asynchronous memory bus channel, letting independent background consumer threads process heavy text computational logic out-of-band.

3. **Cloud-Native & Containerised**: The backend environment is packaged via Docker containers for consistent execution across environments. Using a Universal DB Strategy, the application dynamically flips between SQLite for local hardware testing and PostgreSQL for cloud production based on automated environment detection.

4. **Application-Level Identity (GUIDs) for High Availability**: To eliminate the common database auto-increment identity handshake blocks during fast, high-volume write loops, unique String GUIDs are generated directly inside the C# application code prior to persistence. This database-agnostic strategy guarantees high write availability across distributed environments.

5. **Temporal Database Constraint Layouts**: Configured explicit timezone column mapping schemas utilizing Entity Framework Fluent API attributes (`timestamp with time zone`). This creates strict, structural validation checks that automatically normalize telemetry timestamps between local flat SQLite database files and live cloud relational pools.

6. **Service Idempotency Shields (Fault-Tolerant Duplicate Guard)**: Hardened against network packet retries and distributed message duplication bugs common in cloud routing infrastructures. Background consumer workers run an automatic telemetry state check upon picking up a message; if a ticket record is already flagged as `Processing` or `Resolved`, the worker exits safely to protect data lines and save CPU cycles.

7. **Automated Dead Letter Queue (DLQ) Fault Consumers**: Designed with self-healing message routing pipelines to handle downstream infrastructure outages (such as an offline local AI engine). If an event worker encounters a runtime crash, MassTransit catches the exception and routes the data envelope straight to a dedicated **Fault Consumer**, which automatically updates the record state to `Failed`, logs an infrastructure log statement, and keeps the primary message highway fluid and functional.

8. **Isolated Database Schema Multi-Tenancy**: The application targets custom table naming suffixes (`_Final_v6`) generated via Entity Framework mapping handlers. This explicit naming boundary pattern isolates database tables, allowing multiple decoupled full-stack projects to safely co-exist within a single shared cloud PostgreSQL database instance without encountering namespace collisions or data corruption cross-talk.

---

## 💻 Tech Stack

* **Frontend Framework**: React 19 (TypeScript), Tailwind CSS, hosted on Vercel.
* **Backend Framework**: ASP.NET Core 10 Web API + MassTransit (Dockerized), hosted on Render.
* **Database Management**: Aiven PostgreSQL (Production) and SQLite (Local).
* **Intelligence Core**: Local Ollama Engine executing the `phi3` model (Local) and an asynchronous telemetry cloud simulator (Production).
* **CI/CD Integration**: Automated integration and deployment pipelines via GitHub.

---

## 🌟 Key Features

1. **Permanent Operations Log**: A cloud-hosted PostgreSQL database ensures that your entire historical support metric deck survives server restarts and container recycles.
2. **Server Cold-Start Notification**: Smart frontend tracking logic that displays an explicit, red warning notice to users when a free-tier server instance is waking up from inactivity.
3. **Automated Labeling & Fix Generation**: Background AI consumer threads parse the semantic payload of incoming ticket descriptions to autonomously apply classification labels (`bug` vs. `investigate`) and generate targeted technical fix commentary.
4. **Interactive Cloud Re-Triage**: Full CRUD capabilities allowing web users to edit ticket content using pre-filled inline modal inputs. Saving modifications preserves the original record ID while resetting status flags to re-trigger the background AI queue pipeline in real-time.

---

## 🧪 Automated Quality Assurance & Testing

LLM-TriageAgent includes a separate, root-level test project built with **xUnit** and **Moq** to validate core background consumer logic, state machine patterns, and idempotency guards using an isolated, thread-safe In-Memory database tracker layer.

### Core Coverage Matrices
* **Idempotency Checks**: Verifies that backend consumer workers immediately intercept and drop redundant messages—safeguarding data channels if the React client sends a duplicate network packet by accident or if an internet protocol loops a transaction—preventing duplicate processing loops to preserve absolute database state integrity.

* **AI Rule Classification**: Validates pattern matching logic against high-risk error indicators (e.g., forcing a `bug` label string when descriptions contain a `"404"` error flag).
* **Fault Consumer Resilience**: Assures the Dead Letter Queue worker properly isolates exception envelopes and saves quarantine logs during system crashes.

### Running the Test Engine
To clear build artifacts and execute the automated backend test suites from the root directory, open your terminal and run:
```bash
dotnet test
```

---

## 🔍 Manual Verification Guide
To verify the full functionality of the LLM-TriageAgent system live in your browser, follow these manual testing pipelines:

### 1. Verification of Core CRUD & Real-Time Polling Loops
* **Submit a Ticket**: Type an issue statement into the form and click Submit.
* **Observe the State Machine**: The card immediately slides into the monitor list showing a pulsing yellow **`PROCESSING`** status badge.
* **AI Completion**: After a few random seconds of calculation, the background worker automatically updates the card over real-time polling channels to an emerald green **`RESOLVED`** badge, revealing the assigned AI label and automated fix message.
* **Inline Editing**: Click the blue **✏️ Edit ticket** button on a resolved card. A modal form pre-filled with the original text will pop up, tracking the original Ticket ID at the top. Modifying text and clicking save resets the card to yellow to re-run the background AI triage loop.

### 2. Destruction & Cleanup Handshake
* **Delete tickets**: Click the red **🗑️ Delete ticket** button on any resolved card. 
* **Custom Modal Reuse**: A custom theme modal will pop up right over the screen requesting a secure confirmation. Clicking confirm executes the database delete command over the API and makes the card slide off the dashboard array list smoothly with zero browser alert popups.

### 3. How to Simulate a Live Cloud Infrastructure Outage
To witness the system's asynchronous error-catching capability over the live internet:
* Open `LLM-TriageAgent.API/Services/TicketConsumer.cs`.
* Go to the cloud production block (`if (isProductionCloud)`) and force a manual runtime exception statement at the top of the `try` block:
  ```csharp
  throw new Exception("CRITICAL ERROR: Cloud AI Model Gateway Timeout.");
  ```
* Push the change to GitHub and let Render deploy it. 
* Submit a fresh ticket on your live Vercel site. 

Because the background thread crashes instantly, MassTransit catches the exception, reroutes the message to your `TicketFaultConsumer`, and turns the card a flat red box. Simultaneously, your custom **Notification Modal will instantly pop up right over your screen** with a red cross icon to alert you that the system has successfully isolated and quarantined an infrastructure failure block!
