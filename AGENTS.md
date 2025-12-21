# AGENTS.md - AI Context & Operating Instructions

## 1. Project Identity & Purpose

* **Project Name:** Nimbus Station
* **Binary Name:** `ns`
* **Repository:** `nimbus-station`
* **Description:** A cloud-agnostic, session-based REPL workbench for cloud
  investigation and system stabilization.
* **Core Philosophy:** "Contextual Investigation." Anchors all operations to a
  persistent session (Jira Ticket ID), maintaining state, history, and a local
  directory for artifacts.

## 2. Technical Stack & Constraints

* **Language:** C# (.NET 8.0, 9.0, 10.0 multi-target)
* **Testing:** xUnit (Must maintain high coverage for core logic)
* **UI/UX:** `Spectre.Console` for all terminal rendering.
* **Auth:** `Azure.Identity` (DefaultAzureCredential) for the Azure Provider.
* **Config:** TOML (`~/.config/nimbus/config.toml`).

## 3. Solution Architecture

The solution is modular. The `.sln` file resides in the root, while all
projects (including tests) reside under the `src/` directory:

* **src/NimbusStation.Cli**: Entry point, REPL loop, and Command definitions.
* **src/NimbusStation.Core**: Domain models, Session management, and Provider
  interfaces.
* **src/NimbusStation.Infrastructure**: Configuration (TOML), Shell-piping,
  and File System logic.
* **src/NimbusStation.Providers.Azure**: Azure-specific logic (Cosmos,
  Storage).
* **src/NimbusStation.Tests**: xUnit test suites mirroring the internal
  project structure.

## 4. Key Architectural Patterns

### Provider-Agnostic Core

The REPL interacts with `ICloudProvider` abstractions. Azure-specific code
must stay in the `Providers.Azure` project.

### Stateful "Station" Sessions

* **Path:** `~/.nimbus/sessions/{TICKET_ID}/`
* **Session.json:** Persists the "Active Context" (e.g., active aliases).
* **Auto-Persistence:** Query results saved to `/queries/`; downloads to
  `/downloads/`.

### The Shell Pipe (`|`)

* The REPL must intercept `|` before execution.
* Internal output is streamed to the `StandardInput` of the detected system
  process (e.g., `jq`, `grep`).

## 5. Coding Standards

* **Structure:** No single-file projects. Use clear namespaces
  (e.g., `NimbusStation.Core.Services`).
* **Testability:** Use interfaces for all external dependencies (SDKs,
  Filesystem) to allow mocking in xUnit.
* **DI:** Use `Microsoft.Extensions.DependencyInjection`.
* **Async:** Native async/await throughout.

## 6. Directory Structure

```text
/nimbus-station
├── NimbusStation.sln
├── AGENTS.md
└── src/
    ├── NimbusStation.Cli/
    ├── NimbusStation.Core/
    ├── NimbusStation.Infrastructure/
    ├── NimbusStation.Providers.Azure/
    └── NimbusStation.Tests/
