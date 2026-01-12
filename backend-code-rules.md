# Backend Code Quality Rules

## Usage Guide
- Rules have severity: [C]ritical, [H]igh, [M]edium, [L]ow
- When rules conflict: Higher severity wins → Existing code patterns take precedence
- Process rules by severity (Critical first)

## Class Requirements [C]

- **[C1-C]** Elegant Objects by Yegor Bugayenko design principles must be respected.
- **[C2-C]** Classes must implement an interface.
- **[C3-C]** Interface names must be a single noun and reflect a real-world entities.
- **[C4-C]** Class names must consist only of adjective(s) + noun.
- **[C5-C]** Implementation inheritance must be avoided; prefer composition and interfaces.
- **[C6-C]** Utility classes are strictly prohibited.
- **[C7-C]** Static methods in classes are strictly prohibited.
- **[C8-C]** All classes must be declared `sealed`.
- **[C9-H]** Constructors may contain only assignment statements.
- **[C10-H]** Property getters exposing internal state must be avoided.
- **[C11-H]** Setters must be avoided to preserve immutability.
- **[C12-H]** Immutable objects must be favored.
- **[C13-H]** Every class must encapsulate at least one field.
- **[C14-H]** Classes must avoid public static readonly fields and const fields.
- **[C15-H]** Each private method is a candidate for a new class.
- **[C16-M]** Class and interface names may not end with the *-er* *-ir* suffixes.
- **[C17-M]** A class may encapsulate no more than four fields.
- **[C18-M]** Variable names must be single nouns, never compound or composite.

## Architecture & Structure [A]

- **[A1-H]** The DDD paradigm must be respected.
- **[A2-H]** Favor "fail fast" over "fail safe": throw exceptions as early as possible.

## Method Requirements [M]

- **[M1-C]** Methods must never return `null`; use `Option<T>`-like patterns or explicit objects.
- **[M2-C]** `null` must not be passed as an argument.
- **[M3-C]** Type checks (`is`, `as`) and casting are strictly prohibited.
- **[M4-C]** Reflection on object internals is strictly prohibited.
- **[M5-H]** Methods must be declared in interfaces and implemented in classes.
- **[M6-H]** Public methods that do not implement an interface must be avoided.
- **[M7-H]** Exception messages must include maximum contextual information.
- **[M8-M]** Method names must respect CQRS: commands are verbs, queries are nouns.
- **[M9-M]** Method names must be single verbs, never compound or composite.
- **[M10-M]** Methods should avoid checking incoming arguments for validity.