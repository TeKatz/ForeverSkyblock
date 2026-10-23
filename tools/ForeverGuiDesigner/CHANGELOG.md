# Forever GUI Designer changelog

## Development

- Added an outer update-check watchdog so the UI always leaves `Checking…` after at most 18 seconds even if Windows networking does not honor an inner HTTP cancellation promptly.
- Fixed self-update verification on Windows by disposing the staged executable download stream before reopening the file for SHA-256 verification.
- Added a GitHub release-manifest fallback for update checks.
- Added Active Processing preview and suppressed internal Forever panel metadata labels in machine previews.
