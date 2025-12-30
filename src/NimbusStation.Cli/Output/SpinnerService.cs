using NimbusStation.Core.Options;
using Spectre.Console;

namespace NimbusStation.Cli.Output;

/// <summary>
/// Provides spinner and progress bar functionality that respects global options and piping context.
/// </summary>
public sealed class SpinnerService
{
    private readonly GlobalOptions _options;
    private readonly bool _isOutputRedirected;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpinnerService"/> class.
    /// </summary>
    /// <param name="options">The global CLI options.</param>
    public SpinnerService(GlobalOptions options)
    {
        _options = options;
        _isOutputRedirected = Console.IsOutputRedirected;
    }

    /// <summary>
    /// Gets a value indicating whether spinners should be shown.
    /// Returns false if quiet mode is enabled or output is redirected (piping).
    /// </summary>
    public bool ShouldShowSpinner => !_options.Quiet && !_isOutputRedirected;

    /// <summary>
    /// Runs an async operation with a spinner, if spinners are enabled.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="message">The message to display while spinning.</param>
    /// <param name="work">The async work to perform.</param>
    /// <returns>The result of the work.</returns>
    public async Task<T> RunWithSpinnerAsync<T>(string message, Func<Task<T>> work)
    {
        if (!ShouldShowSpinner)
            return await work();

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync(message, async _ => await work());
    }

    /// <summary>
    /// Runs an async operation with a spinner, if spinners are enabled.
    /// </summary>
    /// <param name="message">The message to display while spinning.</param>
    /// <param name="work">The async work to perform.</param>
    public async Task RunWithSpinnerAsync(string message, Func<Task> work)
    {
        if (!ShouldShowSpinner)
        {
            await work();
            return;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync(message, async _ => await work());
    }

    /// <summary>
    /// Runs an operation with a progress bar for multiple items.
    /// </summary>
    /// <typeparam name="TItem">The type of items being processed.</typeparam>
    /// <typeparam name="TResult">The type of results.</typeparam>
    /// <param name="items">The items to process.</param>
    /// <param name="processItem">The function to process each item.</param>
    /// <param name="getDescription">Function to get a description for each item.</param>
    /// <returns>The results of processing each item.</returns>
    public async Task<IReadOnlyList<TResult>> RunWithProgressAsync<TItem, TResult>(
        IReadOnlyList<TItem> items,
        Func<TItem, Task<TResult>> processItem,
        Func<TItem, string>? getDescription = null)
    {
        var results = new List<TResult>(items.Count);

        if (!ShouldShowSpinner || items.Count <= 1)
        {
            foreach (var item in items)
                results.Add(await processItem(item));
            return results;
        }

        await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Processing", maxValue: items.Count);

                foreach (var item in items)
                {
                    if (getDescription is not null)
                        task.Description = getDescription(item);

                    results.Add(await processItem(item));
                    task.Increment(1);
                }
            });

        return results;
    }
}
