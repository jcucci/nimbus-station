using NimbusStation.Spike.Piping;
using Spectre.Console;

// ============================================================================
// REPL Piping Spike - Proof of Concept
// ============================================================================
// This spike demonstrates:
// 1. Parsing pipe characters in input (respecting quotes/escapes)
// 2. Streaming internal command output to external process stdin
// 3. Capturing external process stdout/stderr
// 4. Handling cancellation, errors, and edge cases
// ============================================================================

AnsiConsole.Write(new FigletText("Piping Spike").Color(Color.Cyan1));
AnsiConsole.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    AnsiConsole.MarkupLine("[yellow]Cancellation requested...[/]");
};

await RunAllDemos(cts.Token);

// ============================================================================
// Demo Runners
// ============================================================================

async Task RunAllDemos(CancellationToken ct)
{
    // Demo 1: Pipeline Parser
    RunParserDemos();

    if (ct.IsCancellationRequested) return;

    // Demo 2: External Process Execution
    await RunProcessDemos(ct);

    if (ct.IsCancellationRequested) return;

    // Demo 3: Integration (simulated internal command → external process)
    await RunIntegrationDemos(ct);

    if (ct.IsCancellationRequested) return;

    // Demo 4: Edge Cases
    await RunEdgeCaseDemos(ct);

    AnsiConsole.MarkupLine("\n[green]All demos completed![/]");
}

// ============================================================================
// Demo 1: Pipeline Parser
// ============================================================================

void RunParserDemos()
{
    AnsiConsole.Write(new Rule("[cyan]Demo 1: Pipeline Parser[/]").LeftJustified());
    AnsiConsole.WriteLine();

    var testCases = new[]
    {
        // Basic cases
        ("Simple command (no pipe)", "cosmos query SELECT * FROM c"),
        ("Single pipe", "cosmos query \"SELECT *\" | jq ."),
        ("Multiple pipes", "cmd1 | cmd2 | cmd3"),

        // Quote handling
        ("Pipe inside double quotes", "cosmos query \"a | b\" | jq ."),
        ("Pipe inside single quotes", "echo 'hello | world' | grep hello"),

        // Escape handling
        ("Escaped pipe", @"echo a\|b"),
        ("Escaped quote", @"echo ""hello \""world\"" "" | cat"),

        // Edge cases
        ("Trailing pipe (error)", "cmd |"),
        ("Leading pipe (error)", "| cmd"),
        ("Empty segment (error)", "cmd1 | | cmd2"),
        ("Complex real-world", @"cosmos query ""SELECT * FROM c WHERE c.type = 'user'"" | jq '.[] | .id' | head -5"),
    };

    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Test Case")
        .AddColumn("Input")
        .AddColumn("Result");

    foreach (var (name, input) in testCases)
    {
        var result = PipelineParser.Parse(input);

        string resultText;
        if (result.IsValid)
        {
            var segments = string.Join(" → ", result.Segments.Select(s =>
                $"[{(s.IsFirst ? "green" : "blue")}]{Markup.Escape(Truncate(s.Content, 30))}[/]"));
            resultText = $"{result.Segments.Count} segment(s): {segments}";
        }
        else
        {
            resultText = $"[red]Error: {Markup.Escape(result.Error!)}[/]";
        }

        table.AddRow(
            Markup.Escape(name),
            $"[dim]{Markup.Escape(Truncate(input, 40))}[/]",
            resultText);
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
}

// ============================================================================
// Demo 2: External Process Execution
// ============================================================================

async Task RunProcessDemos(CancellationToken ct)
{
    AnsiConsole.Write(new Rule("[cyan]Demo 2: External Process Execution[/]").LeftJustified());
    AnsiConsole.WriteLine();

    var executor = new ExternalProcessExecutor();

    // Test 2.1: Simple command without stdin
    AnsiConsole.MarkupLine("[bold]2.1 Simple command (echo):[/]");
    var result = await executor.ExecuteAsync("echo", "Hello from external process", cancellationToken: ct);
    PrintResult(result);

    if (ct.IsCancellationRequested) return;

    // Test 2.2: Command with stdin
    AnsiConsole.MarkupLine("[bold]2.2 Pipe to cat:[/]");
    result = await executor.ExecuteAsync("cat", stdinContent: "This was piped via stdin!", cancellationToken: ct);
    PrintResult(result);

    if (ct.IsCancellationRequested) return;

    // Test 2.3: JSON through jq (if available)
    AnsiConsole.MarkupLine("[bold]2.3 Pipe JSON to jq:[/]");
    var json = """{"name":"test","value":42,"items":["a","b","c"]}""";
    result = await executor.ExecuteAsync("jq", ".", json, ct);
    if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
        result.Error?.Contains("No such file", StringComparison.OrdinalIgnoreCase) == true)
    {
        AnsiConsole.MarkupLine("[yellow]jq not installed - skipping[/]");
    }
    else
    {
        PrintResult(result);
    }

    if (ct.IsCancellationRequested) return;

    // Test 2.4: Non-existent command
    AnsiConsole.MarkupLine("[bold]2.4 Non-existent command:[/]");
    result = await executor.ExecuteAsync("nonexistent_command_12345", cancellationToken: ct);
    PrintResult(result);

    if (ct.IsCancellationRequested) return;

    // Test 2.5: Command that exits with error
    AnsiConsole.MarkupLine("[bold]2.5 Command with non-zero exit (grep no match):[/]");
    result = await executor.ExecuteAsync("grep", "xyz", "abc\ndef\nghi", ct);
    PrintResult(result);

    AnsiConsole.WriteLine();
}

// ============================================================================
// Demo 3: Integration (Internal Command → External Process)
// ============================================================================

async Task RunIntegrationDemos(CancellationToken ct)
{
    AnsiConsole.Write(new Rule("[cyan]Demo 3: Integration Demo[/]").LeftJustified());
    AnsiConsole.WriteLine();

    var executor = new ExternalProcessExecutor();

    // Simulate internal command output (like a Cosmos query result)
    AnsiConsole.MarkupLine("[bold]3.1 Simulated Cosmos query → grep:[/]");

    var simulatedCosmosOutput = """
        {"id":"user-001","name":"Alice","type":"admin"}
        {"id":"user-002","name":"Bob","type":"user"}
        {"id":"user-003","name":"Charlie","type":"admin"}
        {"id":"user-004","name":"Diana","type":"user"}
        """;

    AnsiConsole.MarkupLine("[dim]Simulated internal command output:[/]");
    AnsiConsole.WriteLine(simulatedCosmosOutput);

    // Parse the pipeline
    var pipeline = PipelineParser.Parse("cosmos query \"SELECT *\" | grep admin");
    AnsiConsole.MarkupLine($"[dim]Pipeline: {pipeline.Segments.Count} segments[/]");

    // Execute external part (grep admin)
    var (cmd, args) = ExternalProcessExecutor.ParseCommand("grep admin");
    var result = await executor.ExecuteAsync(cmd, args, simulatedCosmosOutput, ct);

    AnsiConsole.MarkupLine("[bold]Result after piping through grep:[/]");
    PrintResult(result);

    if (ct.IsCancellationRequested) return;

    // 3.2: Large output test
    AnsiConsole.MarkupLine("[bold]3.2 Large output → head -5:[/]");

    var largeOutput = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line {i}: some data here"));
    AnsiConsole.MarkupLine($"[dim]Generated {10000} lines of output[/]");

    result = await executor.ExecuteAsync("head", "-5", largeOutput, ct);
    AnsiConsole.MarkupLine("[bold]Result (first 5 lines only):[/]");
    PrintResult(result);

    if (ct.IsCancellationRequested) return;

    // 3.3: Word count
    AnsiConsole.MarkupLine("[bold]3.3 Multi-line text → wc -l:[/]");
    var multiLineText = "line one\nline two\nline three\nline four\nline five";
    result = await executor.ExecuteAsync("wc", "-l", multiLineText, ct);
    PrintResult(result);

    AnsiConsole.WriteLine();
}

// ============================================================================
// Demo 4: Edge Cases
// ============================================================================

async Task RunEdgeCaseDemos(CancellationToken ct)
{
    AnsiConsole.Write(new Rule("[cyan]Demo 4: Edge Cases[/]").LeftJustified());
    AnsiConsole.WriteLine();

    var executor = new ExternalProcessExecutor();

    // 4.1: Process that closes stdin early (head -1 with lots of input)
    AnsiConsole.MarkupLine("[bold]4.1 Process closes stdin early (head -1 with 1000 lines):[/]");
    var manyLines = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i}"));
    var result = await executor.ExecuteAsync("head", "-1", manyLines, ct);
    PrintResult(result);

    if (ct.IsCancellationRequested) return;

    // 4.2: Binary-ish data (bytes 0-255)
    AnsiConsole.MarkupLine("[bold]4.2 Non-text data through cat:[/]");
    // Just use printable ASCII for this test
    var binaryIsh = string.Concat(Enumerable.Range(32, 95).Select(i => (char)i));
    result = await executor.ExecuteAsync("cat", stdinContent: binaryIsh, cancellationToken: ct);
    AnsiConsole.MarkupLine($"[dim]Input length: {binaryIsh.Length}, Output length: {result.StandardOutput.Length}[/]");
    AnsiConsole.MarkupLine(result.IsSuccess ? "[green]✓ Passed[/]" : "[red]✗ Failed[/]");

    if (ct.IsCancellationRequested) return;

    // 4.3: stderr output
    AnsiConsole.MarkupLine("[bold]4.3 Command that writes to stderr:[/]");
    result = await executor.ExecuteAsync("sh", "-c \"echo 'stdout message' && echo 'stderr message' >&2\"", cancellationToken: ct);
    PrintResult(result, showStderr: true);

    if (ct.IsCancellationRequested) return;

    // 4.4: ParseCommand helper
    AnsiConsole.MarkupLine("[bold]4.4 ParseCommand helper:[/]");
    var parseTests = new[]
    {
        "jq",
        "jq .",
        "jq '.name'",
        "grep -i \"hello world\"",
        "  cmd   with   spaces  ",
    };

    foreach (var test in parseTests)
    {
        var (cmd, args) = ExternalProcessExecutor.ParseCommand(test);
        AnsiConsole.MarkupLine($"  [dim]{Markup.Escape(test)}[/] → cmd=[cyan]{Markup.Escape(cmd)}[/], args=[cyan]{Markup.Escape(args ?? "(null)")}[/]");
    }

    AnsiConsole.WriteLine();
}

// ============================================================================
// Helpers
// ============================================================================

void PrintResult(ProcessResult result, bool showStderr = false)
{
    if (result.Error is not null)
    {
        AnsiConsole.MarkupLine($"  [red]Startup Error:[/] {Markup.Escape(result.Error)}");
        return;
    }

    var statusColor = result.IsSuccess ? "green" : (result.WasKilled ? "yellow" : "red");
    var status = result.IsSuccess ? "Success" : (result.WasKilled ? "Killed" : $"Failed (exit {result.ExitCode})");

    AnsiConsole.MarkupLine($"  [{statusColor}]{status}[/]");

    if (!string.IsNullOrEmpty(result.StandardOutput))
    {
        AnsiConsole.MarkupLine("  [dim]stdout:[/]");
        foreach (var line in result.StandardOutput.TrimEnd().Split('\n').Take(10))
        {
            AnsiConsole.MarkupLine($"    {Markup.Escape(line)}");
        }
        if (result.StandardOutput.Split('\n').Length > 10)
        {
            AnsiConsole.MarkupLine("    [dim]... (truncated)[/]");
        }
    }

    if (showStderr && !string.IsNullOrEmpty(result.StandardError))
    {
        AnsiConsole.MarkupLine("  [red]stderr:[/]");
        foreach (var line in result.StandardError.TrimEnd().Split('\n').Take(5))
        {
            AnsiConsole.MarkupLine($"    {Markup.Escape(line)}");
        }
    }

    AnsiConsole.WriteLine();
}

string Truncate(string s, int maxLength) =>
    s.Length <= maxLength ? s : s[..(maxLength - 3)] + "...";
