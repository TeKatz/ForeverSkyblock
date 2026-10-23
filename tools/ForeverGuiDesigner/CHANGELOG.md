# Forever GUI Designer changelog

## Development

- Replaced the updater's `await` return path with an explicit ThreadPool continuation plus `Dispatcher.BeginInvoke`, so successful network results are posted back to WPF deterministically instead of relying on a captured synchronization context.
- Moved all updater network resolution off the WPF UI thread and added a visible Cancel action plus `%LOCALAPPDATA%\ForeverGuiDesigner\updater.log` diagnostics with a one-click Log button.
- Added an outer update-check watchdog so the UI always leaves `Checking…` after at most 18 seconds even if Windows networking does not honor an inner HTTP cancellation promptly.
- Fixed self-update verification on Windows by disposing the staged executable download stream before reopening the file for SHA-256 verification.
- Added a GitHub release-manifest fallback for update checks.
- Added Active Processing preview and suppressed internal Forever panel metadata labels in machine previews.
