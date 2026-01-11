# Copilot Instructions
CRITICAL: Each generated C# file must be validated against the following coding standards and best practices. These rules ensure code quality, maintainability, and consistency across the codebase.

## Usage Guide
- Rules have severity: [C]ritical, [H]igh, [M]edium, [L]ow
- When rules conflict: Higher severity wins → Existing code patterns take precedence
- Process rules by severity (Critical first)

## Architecture & Structure [A]

- **[A1-C]** The existing solution and namespace structure must not be changed without a strong reason.
- **[A2-C]** Every bug must be reproduced by a unit test before being fixed.
- **[A3-C]** Every new feature must be covered by a unit test before it is implemented.
- **[A4-M]** Minor inconsistencies and typos in the existing code may be fixed.
- **[A5-H]** All CI workflows must pass before code changes may be reviewed.
- **[A6-H]** The DDD paradigm must be respected.
- **[A7-H]** Elegant Objects design principles must be respected.

## Code Style & Patterns [S]

- **[S1-H]** Method bodies may not contain blank lines.
- **[S2-H]** Method and function bodies may not contain comments.
- **[S3-M]** Variable names must be single nouns, never compound or composite.
- **[S4-M]** Method names must be single verbs, never compound or composite.
- **[S5-H]** The principle of “Paired Brackets” suggested by Yegor Bugayenko must be respected.
- **[S6-M]** Exception and log messages must not end with a period.
- **[S7-M]** Exception and log messages must be a single sentence with no periods inside.
- **[S8-H]** Favor “fail fast” over “fail safe”: throw exceptions as early as possible.
- **[S9-M]** Method names must respect CQRS: commands are verbs, queries are nouns.
- **[S10-H]** Classes must avoid public static readonly fields and const fields.

## Class Requirements [C]

- **[C1-C]** Every class must have an XML documentation comment preceding it.
- **[C2-H]** Class documentation must explain the purpose of the class and provide usage examples.
- **[C3-H]** Constructors may contain only assignment statements.
- **[C4-C]** Implementation inheritance must be avoided; prefer composition and interfaces.
- **[C5-H]** Property getters exposing internal state must be avoided.
- **[C6-M]** Class names may not end with the *-er* suffix.
- **[C7-H]** Setters must be avoided to preserve immutability.
- **[C8-H]** Immutable objects must be favored.
- **[C9-H]** Each class may have only one primary constructor; secondary constructors must delegate to it.
- **[C10-M]** A class may encapsulate no more than four fields.
- **[C11-H]** Every class must encapsulate at least one field.
- **[C12-C]** Utility classes are strictly prohibited.
- **[C13-C]** Static methods in classes are strictly prohibited.
- **[C14-C]** All classes must be declared `sealed`.

## Method Requirements [M]

- **[M1-C]** Every method must have an XML documentation comment.
- **[M2-H]** Methods must be declared in interfaces and implemented in classes.
- **[M3-H]** Public methods that do not implement an interface must be avoided.
- **[M4-C]** Methods must never return `null`; use `Option<T>`-like patterns or explicit objects.
- **[M5-M]** Methods should avoid checking incoming arguments for validity.
- **[M6-C]** `null` must not be passed as an argument.
- **[M7-C]** Type checks (`is`, `as`) and casting are strictly prohibited.
- **[M8-C]** Reflection on object internals is strictly prohibited.
- **[M9-H]** Exception messages must include maximum contextual information.

## Documentation [D]

- **[D1-H]** The README.md file must explain the purpose of the repository.
- **[D2-H]** The README.md file must be free of typos and broken English.
- **[D3-M]** The README.md file must be as short as possible and must not duplicate XML documentation.
- **[D4-H]** All XML documentation must be written in English using UTF-8.

## Testing Standards [T]

- **[T1-C]** Every change must be covered by a unit test.
- **[T2-H]** Each test may contain only one assertion.
- **[T3-H]** The assertion must be the last statement in the test.
- **[T4-M]** Tests must be as short as possible.
- **[T5-H]** Every test must assert at least once.
- **[T6-M]** Test files must map one-to-one with production files.
- **[T7-H]** Assertions must include a negatively worded failure message.
- **[T8-M]** Tests must use irregular inputs, including non-ASCII strings.
- **[T9-H]** Tests may not share state.
- **[T10-H]** Tests must not use setup or teardown fixtures.
- **[T11-H]** Tests must not use static literals or shared constants.
- **[T12-M]** Test names must be full English sentences describing behavior.
- **[T13-H]** Tests must verify only the behavior stated in their name.
- **[T14-H]** Tests must close all resources they open.
- **[T15-H]** Production code must not contain test-only functionality.
- **[T16-M]** Tests must not assert on side effects such as logging.
- **[T17-L]** Tests must not validate constructors or property accessors.
- **[T18-M]** Tests must prepare a clean state at the beginning.
- **[T19-H]** Mocks must be avoided; use fakes or stubs.
- **[T20-M]** The best tests consist of a single statement.
- **[T21-M]** Prefer fluent or matcher-based assertions when available.
- **[T22-H]** Each test must verify exactly one behavioral pattern.
- **[T23-M]** Tests must use random input values.
- **[T24-M]** Temporary files must be created in OS-designated temp directories.
- **[T25-H]** Tests must not produce log output.
- **[T26-H]** Logging must be disabled for objects under test.
- **[T27-H]** Tests must always use timeouts when waiting.
- **[T28-H]** Tests must validate behavior under concurrency.
- **[T29-H]** Tests must retry potentially flaky operations.
- **[T30-H]** Tests must assume no Internet connection.
- **[T31-M]** Tests must not assert on exception messages or error codes.
- **[T32-H]** Tests must not rely on default configurations.
- **[T33-H]** Tests must not mock file systems, sockets, or memory managers.
- **[T34-M]** Tests must use ephemeral TCP ports allocated at runtime.
- **[T35-M]** Small fixtures must be inlined.
- **[T36-M]** Large fixtures must be generated at runtime.
- **[T37-M]** Supplementary fixture objects may be created to reduce duplication.
- **[T38-L]** Test method names must spell “cannot” and “dont” without apostrophes.

## AI Code Generation Process [AI]

- **[AI1-H]** Analyze existing C# code patterns first.
- **[AI2-H]** Write tests before implementation.
- **[AI3-H]** Design interfaces before classes.
- **[AI4-H]** Implement with immutability as the default.
- **[AI5-H]** Error handling must validate early and throw specific exceptions.
