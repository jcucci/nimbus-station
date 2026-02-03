# CLAUDE.md - AI Context & Operating Instructions

## 1. Project Identity & Purpose

* **Project Name:** Nimbus Station
* **Binary Name:** `ns`
* **Repository:** `nimbus-station`
* **Description:** A cloud-agnostic, session-based REPL workbench for cloud
  investigation and system stabilization.
* **Core Philosophy:** "Contextual Investigation." Anchors all operations to a
  persistent session (GitHub Issue ID), maintaining state, history, and a local
  directory for artifacts.
* **Issue Tracking:** GitHub Issues only. All tickets/issues reside in the
  associated `nimbus-station` GitHub repository.

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

### Code Style Preferences

* **One type per file:** Each public class, record, or interface should be in
  its own file. Exceptions: private nested types that are internal to the
  containing type.
* **Expression-bodied members:** Prefer expression bodies (`=>`) for simple
  single-statement methods and properties.
* **Inline conditionals:** Use inline `if` statements for simple early returns
  (e.g., `if (x) return y;`).
* **Named parameters:** Use named parameters when calling methods with multiple
  arguments to improve readability, especially for boolean flags or when the
  meaning isn't obvious from the value alone.
* **Consolidate shared logic:** When similar parsing or utility logic appears
  in multiple places, extract it to a shared location in Core (e.g.,
  `InputTokenizer` in `Core.Parsing`).
* **Alias naming:** Alias names must start with a letter and contain only
  letters, numbers, hyphens, and underscores. Leading numbers are not allowed
  to avoid confusion with numeric arguments.
* **No #region directives:** Avoid using `#region`/`#endregion` in code and
  test files. Use clear method naming and file organization instead.

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
