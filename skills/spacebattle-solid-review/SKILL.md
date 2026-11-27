---
name: spacebattle-solid-review
description: Review SpaceBattle diffs for SOLID boundaries, IoC usage, deterministic command flow, and testable architecture.
---

# SpaceBattle SOLID Review

Review the diff together with the relevant ADRs and tests. Report actionable findings with file, line, priority, violated principle, impact, and a minimal fix.

Check the endpoint, scheduler, worker, game, and proxy boundaries for SRP. Check that GameProxy preserves the ICommand contract for LSP. Check that workers and submission services depend on factories, receivers, routers, and serializers through abstractions for DIP. Check that new command names and exception policies are registered through IoC instead of requiring endpoint or worker edits for each command. Check deterministic ordering, generation validation, and snapshot metadata.

Treat architecture documents as proposals unless the implementation and tests confirm them. Distinguish missing tests from incorrect behavior. Avoid style-only findings.
