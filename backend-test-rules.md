# Backend Test Quality Rules

## Usage Guide
- Rules have severity: [C]ritical, [H]igh, [M]edium, [L]ow
- When rules conflict: Higher severity wins → Existing code patterns take precedence
- Process rules by severity (Critical first)

## Testing Standards [T]

- **[T1-C]** Every change or new feature must be covered by a unit and integration tests.
- **[T2-H]** Each test may contain only one assertion.
- **[T3-H]** The assertion must be the last statement in the test.
- **[T4-M]** Tests must be as short as possible.
- **[T5-H]** Every test must assert at least once.
- **[T6-M]** Test files must map one-to-one with production files.
- **[T7-H]** Assertions must include a negatively worded failure message.
- **[T8-M]** Tests must use irregular inputs, including non-ASCII strings.
- **[T9-H]** Tests may not share state.
- **[T12-M]** Test names must be full English sentences describing behavior.
- **[T13-H]** Tests must verify only the behavior stated in their name.
- **[T14-H]** Tests must close all resources they open.
- **[T16-M]** Tests must not assert on side effects such as logging.
- **[T17-L]** Tests must not validate constructors or property accessors.
- **[T18-M]** Tests must prepare a clean state at the beginning.
- **[T19-H]** Mocks must be avoided; use fakes or stubs.
- **[T22-H]** Each test must verify exactly one behavioral pattern.
- **[T23-M]** Tests must use random input values.
- **[T24-M]** Temporary files must be created in OS-designated temp directories.
- **[T27-H]** Tests must always use timeouts when waiting.
- **[T28-H]** Tests must validate behavior under concurrency.
- **[T29-H]** Tests must retry potentially flaky operations.
- **[T32-H]** Tests must not rely on default configurations.
- **[T33-H]** Tests must not mock file systems, sockets, databases.
- **[T38-L]** Test method names must spell “cannot” and “dont” without apostrophes.
