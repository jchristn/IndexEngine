# Change Log

## Current Version

v2.1.3

- .NET 5 support
- Migrate to ORM

## Previous Versions

v2.1.0

- Move to .NET Standard 2.1 and migrate to Microsoft.Data.Sqlite
- Bugfixes

v2.0.0

- Breaking changes, major refactor
- Retarget from .NET 4.5.2 to .NET 4.6.1 (in addition to .NET Core)
- Migrate from unmanaged database layer to SqliteHelper
- Added support for IDisposable
- Database backup API
- Added index start and max results to search
- External logging via Logger
- Sync and async APIs for adding documents


v1.0.12

- Fix (more) to allow empty documents to be added (thanks @teub!)

v1.0.11

- Fix to allow empty documents to be added (thanks @teub!)

v1.0.10

- Fixes to database INSERT/SELECT and string case (thanks @teub!)
- Fix for divide by zero problem (thanks @teub!)
