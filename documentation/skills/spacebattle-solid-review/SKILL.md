# SpaceBattle SOLID Review

Use this skill when reviewing a SpaceBattle change set. Read the diff, the relevant ADRs, and the tests before commenting. Report only actionable findings with a file and line, priority, violated SOLID principle or dependency boundary, concrete impact, and a minimal fix.

Check these boundaries in particular:

- **SRP**. An endpoint validates and translates transport input. A scheduler selects ready games. A game or worker executes commands. Flag a class that owns more than one of these responsibilities.
- **LSP**. `Game` and `GameProxy` must both satisfy the command contract. A proxy may forward execution, but it must preserve the observable command lifecycle and error behavior.
- **DIP**. Workers and submission services depend on command receivers, factories, routers, and serializers through interfaces. Flag direct construction of domain commands, sockets, threads, or concrete transports in these components.
- **OCP**. New command names and exception policies enter through registrations or factories. Flag switch statements that require editing the endpoint or worker for every new command.
- **Determinism**. Command ordering, generation checks, and snapshot fields must be explicit and testable.

Do not treat an architecture proposal as evidence that an implementation exists. Distinguish missing tests from incorrect behavior, and avoid style-only comments when the contract is preserved.
