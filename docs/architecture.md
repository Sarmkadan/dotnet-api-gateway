# Architecture

Moved: see [ARCHITECTURE.md](ARCHITECTURE.md) for the current architecture document.

The previous version of this file described a pipeline (JWT middleware, coalescing in the default path, a `RateLimitRepository`) that did not match the shipped code; the new document is grounded in what `Program.cs` and `ServiceCollectionExtensions` actually wire up.
