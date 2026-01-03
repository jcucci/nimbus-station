# Nimbus Station

A session-based CLI workbench for streamlining cloud support operations.

## Overview

Nimbus Station (`ns`) is a cloud-agnostic, session-based REPL workbench designed for cloud investigation and system stabilization. It anchors all operations to a persistent session (tied to a GitHub Issue ID), maintaining state, command history, and a local directory for artifacts.

## Features

- **Session-Based Workflow**: Tie investigations to ticket IDs with persistent state
- **Azure Integration**: Query Cosmos DB and browse Blob Storage
- **Shell Piping**: Pipe output to external tools like `jq`, `grep`, etc.
- **Command Aliases**: Create shortcuts with parameter substitution
- **Theming**: 25+ built-in color themes
- **Tab Completion**: Auto-complete commands and subcommands

## Installation

```bash
# Build from source
dotnet build

# Run the CLI
dotnet run --project src/NimbusStation.Cli
```

## Quick Start

```bash
# Start a session tied to a ticket
ns> session start TICKET-123

# Set your active Cosmos DB context
ns> use cosmos prod

# Query Cosmos DB and pipe to jq
ns> cosmos query "SELECT * FROM c WHERE c.status = 'error'" | jq '.[].message'

# Set blob storage context and browse
ns> use blob logs
ns> blob search --download
```

## Commands

### Session Management

| Command | Description |
|---------|-------------|
| `session start <name>` | Create or resume a session |
| `session list` | List all sessions |
| `session status` | Show current session details |
| `session resume <name>` | Resume a previous session |
| `session leave` | Deactivate current session |
| `session delete <name>` | Delete a session |

### Context Management

| Command | Description |
|---------|-------------|
| `use` | Show current active contexts |
| `use cosmos <alias>` | Set active Cosmos DB alias |
| `use blob <alias>` | Set active Blob storage alias |
| `use storage <alias>` | Set active Storage account alias |
| `use clear` | Clear all active contexts |

### Azure Cosmos DB

| Command | Description |
|---------|-------------|
| `cosmos query "<SQL>"` | Execute a SQL query |
| `cosmos query "<SQL>" --max-items 50` | Limit results |

Query results are automatically saved to the session's `queries/` directory.

### Azure Blob Storage

| Command | Description |
|---------|-------------|
| `blob containers` | List containers (requires storage alias) |
| `blob list [prefix]` | List blobs with optional prefix |
| `blob get <path>` | Output blob content to stdout |
| `blob download <path>` | Download blob to session directory |
| `blob search [prefix]` | Interactive blob search |

### Authentication

| Command | Description |
|---------|-------------|
| `auth` | Show Azure CLI auth status |
| `auth login` | Initiate Azure CLI login |

### Command Aliases

| Command | Description |
|---------|-------------|
| `alias list` | List all aliases |
| `alias add <name> "<expansion>"` | Create an alias |
| `alias remove <name>` | Remove an alias |
| `alias test <name> [args]` | Test alias expansion |

#### Alias Parameters

```bash
# Positional parameters
alias add finduser "cosmos query \"SELECT * FROM c WHERE c.userId = '{0}'\""
finduser user123

# Built-in variables: {ticket}, {session-dir}, {today}, {now}, {user}
alias add savequery "cosmos query \"{0}\" > {session-dir}/queries/{today}-results.json"
```

### Theming

| Command | Description |
|---------|-------------|
| `theme` | Show current theme |
| `theme list` | List available presets |
| `theme preview <name>` | Preview a theme |

Available presets: catppuccin-mocha, dracula, one-dark, gruvbox-dark, nord, tokyo-night, and more.

### Other Commands

| Command | Description |
|---------|-------------|
| `help` | List all commands |
| `help <command>` | Show command usage |
| `info` | Show active resource context |
| `exit` | Exit the REPL |

## Shell Piping

Pipe internal command output to external shell commands:

```bash
cosmos query "SELECT * FROM c" | jq '.[] | .name'
blob get logs/app.json | jq '.errors'
blob list | grep -i error
```

## Configuration

Configuration file: `~/.config/nimbus/config.toml`

```toml
default_provider = "azure"

[theme]
preset = "catppuccin-mocha"

# Cosmos DB aliases
[cosmos.prod]
endpoint = "https://myaccount.documents.azure.com:443/"
database = "MyDatabase"
container = "MyContainer"

# Blob storage aliases (container-level)
[blob.logs]
account = "mystorageaccount"
container = "application-logs"

# Storage account aliases (for container listing)
[storage.prod]
account = "mystorageaccount"
```

## File Locations

| Item | Path |
|------|------|
| Configuration | `~/.config/nimbus/config.toml` |
| Aliases | `~/.config/nimbus/aliases.toml` |
| Sessions | `~/.nimbus/sessions/{session-name}/` |
| Downloads | `~/.nimbus/sessions/{name}/downloads/` |
| Query Results | `~/.nimbus/sessions/{name}/queries/` |

## Global Options

| Option | Description |
|--------|-------------|
| `--verbose` | Enable debug output |
| `--quiet` | Suppress non-essential output |
| `--no-color` | Disable ANSI colors |
| `--yes` | Skip confirmation prompts |
| `--version` | Show version |

## License

See [LICENSE](LICENSE) for details.
