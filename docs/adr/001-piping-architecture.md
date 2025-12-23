# ADR-001: REPL Piping to External Processes

## Status

**Accepted** - Spike completed, approach validated.

## Context

Nimbus Station's REPL needs to support piping internal command output to
external processes (e.g., `cosmos query "SELECT *" | jq '.[] | .id'`). This is a
core architectural feature that enables users to leverage familiar Unix tools
for data transformation.

### Problem Statement

1. The REPL must parse pipes itself (not delegating to a shell)
2. Internal command output must stream to `Process.StandardInput`
3. External process stdout/stderr must display in the REPL
4. Must handle streaming for large outputs (not buffer entire output before
   piping)
5. Error handling for failed external processes
6. Cross-platform support (macOS, Linux, Windows)

### Constraints

- Time-boxed spike: 4-6 hours
- Must validate approach before production implementation
- Security: avoid shell injection vulnerabilities
- Performance: support large outputs without excessive memory usage

## Decision Drivers

1. **Reliability** - Process failures should be handled gracefully
2. **Usability** - Familiar pipe semantics for users
3. **Security** - No shell injection vulnerabilities
4. **Maintainability** - Code complexity should match use case frequency
5. **Performance** - Streaming for large outputs

## Considered Options

### Option A: Direct Process Spawning (No Shell)

REPL parses all pipes, spawns each external process individually, manually
connects stdout→stdin between processes.

**Pros:**

- Full control over each process lifecycle
- Can cancel individual processes on Ctrl+C
- Can report which process in the chain failed
- No shell injection vulnerabilities
- Consistent cross-platform behavior

**Cons:**

- Significantly more complex code (~200-300 additional lines for multi-pipe)
- Manual stream piping between N processes
- Concurrent async coordination required
- Deadlock prevention complexity
- Must handle buffering ourselves

### Option B: Shell Delegation for External Chain

REPL handles internal command → first pipe. Everything after the first `|` is
passed as a single string to the shell (`/bin/sh -c` or `cmd.exe /c`).

**Pros:**

- Much simpler code (~50-75 lines)
- Shell handles inter-process piping, buffering, deadlocks
- Familiar behavior for users

**Cons:**

- Security risk: must escape user input carefully
- Error reporting is coarser (shell returns single exit code)
- Harder to cancel mid-chain
- Platform differences (bash vs cmd.exe syntax)

### Option C: Hybrid Approach

- **Single external pipe** (`internal | external`): Direct process spawning
- **Multiple external pipes** (`internal | ext1 | ext2`): Shell delegation

## Decision

**Option C: Hybrid Approach**

Rationale:

1. **Single-pipe is the common case** (~95% of expected usage)
2. Direct spawning provides better error handling for the common case
3. Multi-pipe is rare enough that shell delegation's tradeoffs are acceptable
4. Keeps code complexity proportional to use case frequency

## Technical Implementation

### Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           ReplLoop                                   │
│                                                                     │
│  Input: "cosmos query \"SELECT *\" | jq . | head -5"               │
│                              │                                      │
│                              ▼                                      │
│                      PipelineParser                                 │
│                              │                                      │
│                              ▼                                      │
│              ┌───────────────┴───────────────┐                     │
│              │        ParsedPipeline          │                     │
│              │  Segments:                     │                     │
│              │    [0] cosmos query "SELECT *" │                     │
│              │    [1] jq .                    │                     │
│              │    [2] head -5                 │                     │
│              └───────────────┬───────────────┘                     │
│                              │                                      │
│                              ▼                                      │
│              ┌───────────────────────────────┐                     │
│              │    IOutputWriter (capture)    │◄─── Internal Cmd    │
│              └───────────────┬───────────────┘                     │
│                              │                                      │
│                              ▼                                      │
│              ┌───────────────────────────────┐                     │
│              │   ExternalProcessExecutor     │                     │
│              │   (or ShellDelegator for 2+)  │                     │
│              └───────────────┬───────────────┘                     │
│                              │                                      │
│                              ▼                                      │
│                        Console Output                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Components

#### 1. PipelineParser

Location: `src/NimbusStation.Core/Parsing/PipelineParser.cs`

Parses input into segments, respecting:

- Quoted strings (`"a | b"` stays together)
- Escaped pipes (`a\|b` stays together)
- Multiple pipes

```csharp
public static ParsedPipeline Parse(string? input);
public static bool ContainsPipe(string? input);
```

#### 2. ExternalProcessExecutor

Location: `src/NimbusStation.Infrastructure/ShellPiping/ExternalProcessExecutor.cs`

Executes external processes with stdin streaming:

```csharp
public Task<ProcessResult> ExecuteAsync(
    string command,
    string? arguments = null,
    string? stdinContent = null,
    CancellationToken cancellationToken = default);
```

Key implementation details:

- `UseShellExecute = false`
- `RedirectStandardInput/Output/Error = true`
- Concurrent read of stdout/stderr (prevents deadlocks)
- Handles `IOException` when process closes stdin early
- Supports cancellation via `CancellationToken` → `Process.Kill()`

#### 3. IOutputWriter Interface

Location: `src/NimbusStation.Core/Output/IOutputWriter.cs`

Commands write to this interface instead of directly to console:

```csharp
public interface IOutputWriter
{
    void Write(string text);
    void WriteLine(string text);
    Task WriteAsync(string text, CancellationToken ct = default);
    Task WriteLineAsync(string text, CancellationToken ct = default);

    // For structured data (JSON, tables)
    void WriteData(object data);

    // For Spectre.Console compatibility
    void WriteMarkup(string markup);
}
```

Implementations:

- `ConsoleOutputWriter` - Normal mode, writes to `AnsiConsole`
- `CaptureOutputWriter` - Pipe mode, captures to `StringBuilder`
- `StreamOutputWriter` - Binary/large data mode

### Flow for Piped Commands

1. `ReplLoop` receives input
2. `PipelineParser.ContainsPipe()` quick check
3. If pipe detected, `PipelineParser.Parse()` for full parsing
4. Execute internal command with `CaptureOutputWriter`
5. Pass captured output to `ExternalProcessExecutor`
6. Display external process stdout/stderr

### Edge Cases Handled

| Case                     | Behavior                               |
| ------------------------ | -------------------------------------- |
| Quoted pipes             | Preserved (`"a \| b"` → single token)  |
| Escaped pipes            | Preserved (`a\\|b` → single token)     |
| Empty segment            | Error: "Empty segment at position N"   |
| Trailing pipe            | Error: "No command after final pipe"   |
| Leading pipe             | Error: "No command before pipe"        |
| Process not found        | Error with helpful message             |
| Process closes stdin     | Handled gracefully (e.g., `head -1`)   |
| Non-zero exit code       | Reported to user                       |
| Ctrl+C during pipe       | Process killed, clean exit             |
| Large output (10K+ lines)| Streamed, not buffered entirely        |

## Spike Results

### Validated

- Pipeline parsing with quote/escape handling
- External process execution with stdin streaming
- Concurrent stdout/stderr reading (no deadlocks)
- Cancellation handling
- Early stdin close handling (e.g., `head -1`)
- Error reporting for non-existent commands
- Non-zero exit code handling

### Tools Tested

| Tool     | Status | Notes                          |
| -------- | ------ | ------------------------------ |
| `jq`     | Pass   | JSON processing works          |
| `grep`   | Pass   | Text filtering works           |
| `head`   | Pass   | Early stdin close handled      |
| `wc`     | Pass   | Line counting works            |
| `cat`    | Pass   | Pass-through works             |
| `pbcopy` | Pass   | macOS clipboard works          |

### Performance

- 10,000 lines → `head -5`: < 100ms
- Streaming verified (no full buffering)

## Consequences

### Positive

- Clear separation of parsing and execution
- Testable components (interfaces)
- Good error messages for users
- Handles common edge cases
- Familiar Unix pipe semantics

### Negative

- Multi-external-pipe requires shell delegation (security consideration)
- Windows support requires additional work (cmd.exe differences)
- Commands must be refactored to use `IOutputWriter`

### Risks

1. **Shell injection for multi-pipe** - Mitigate with careful escaping
2. **Binary data handling** - Need to use Stream, not string, for blobs
3. **Windows path handling** - Different executable resolution

## Production Implementation Effort

### Estimated Work

| Task                                    | Estimate |
| --------------------------------------- | -------- |
| Move PipelineParser to Core             | 1 hour   |
| Move ExternalProcessExecutor to Infra   | 1 hour   |
| Create IOutputWriter + implementations  | 3 hours  |
| Refactor existing commands to use IOutputWriter | 4 hours |
| Integrate piping into ReplLoop          | 2 hours  |
| Shell delegation for multi-pipe         | 3 hours  |
| Windows support                         | 4 hours  |
| Unit tests                              | 4 hours  |
| Integration tests                       | 2 hours  |
| **Total**                               | **24 hours** |

### Recommended Phases

**Phase 1: Single-pipe support (MVP)**

- PipelineParser
- ExternalProcessExecutor
- IOutputWriter (ConsoleOutputWriter only)
- Integrate into ReplLoop
- Estimated: 8-10 hours

**Phase 2: Full IOutputWriter**

- CaptureOutputWriter
- Refactor commands
- Estimated: 6-8 hours

**Phase 3: Multi-pipe & Windows**

- Shell delegation
- Windows cmd.exe support
- Estimated: 6-8 hours

## References

- [GitHub Issue #6](https://github.com/jcucci/nimbus-station/issues/6)
- Spike code: `src/NimbusStation.Spike.Piping/`
- .NET Process class: [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process)

## Decision Date

2024-12-23

## Participants

- Joe Cucci (author)
