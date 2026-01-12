# Frontend Code & Test Quality Rules

Target stack:
Vite · React 18 · TypeScript · shadcn/ui · TanStack Query · Vitest

---

## 1. Type Safety
- [ ] `any` is forbidden (use `unknown` + narrowing)
- [ ] All exported APIs have explicit return types
- [ ] Zod schemas define all external contracts
- [ ] Types are inferred from schemas, never duplicated
- [ ] `strict`, `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes` enabled

---

## 2. Component Architecture
- [ ] Components are pure and deterministic
- [ ] No side effects during render
- [ ] Presentation and logic are separated
- [ ] Components >150 LOC are decomposed
- [ ] Business logic is never embedded in JSX
- [ ] shadcn/ui components are wrapped, not modified

---

## 3. Hooks & State
- [ ] Shared logic lives in custom hooks (`useX`)
- [ ] Hooks have no conditional execution
- [ ] TanStack Query:
  - [ ] Queries = read-only
  - [ ] Mutations = writes
  - [ ] Typed, centralized query keys
- [ ] No derived state in `useState`
- [ ] State priority: local → context → cache

---

## 4. Side Effects
- [ ] Side effects only in `useEffect` or query callbacks
- [ ] Exhaustive dependency arrays
- [ ] Effects are independent and explicit
- [ ] Date logic uses `date-fns` only

---

## 5. Styling & Accessibility
- [ ] Tailwind composed via `clsx` + `tailwind-merge`
- [ ] No inline styles except runtime-calculated values
- [ ] Variants use `class-variance-authority`
- [ ] Accessibility enforced:
  - [ ] Labels
  - [ ] Keyboard navigation
  - [ ] ARIA where needed

---

## 6. Testing (Vitest + Testing Library)

### Coverage
- [ ] Public components/hooks are tested
- [ ] ≥90% statements, ≥85% branches
- [ ] Coverage enforced in CI

### Design
- [ ] Test behavior, not implementation
- [ ] No internal state or hook-order assertions
- [ ] One test file per unit
- [ ] Only mock network, time, browser APIs

### Execution
- [ ] `user-event` only (no `fireEvent`)
- [ ] No logic snapshots
- [ ] Async tests await explicit UI changes
- [ ] Tests are deterministic on all machines

---

## 7. Error Handling
- [ ] Errors are typed
- [ ] All async flows handle loading/error/success
- [ ] User errors are normalized and readable
- [ ] Logging is structured and env-gated

---

## 8. Linting & Formatting
- [ ] ESLint warnings fail builds
- [ ] Hooks rules are never disabled
- [ ] No unused exports
- [ ] Imports are sorted and absolute
- [ ] Dead code removed immediately

---

## 9. Performance
- [ ] Memoization is justified
- [ ] Stable callbacks and query keys
- [ ] No deep prop trees
- [ ] Routes and heavy components are lazy-loaded

---

## 10. Repository Discipline
- [ ] Every PR includes tests
- [ ] No coverage regression
- [ ] Commits are small and single-purpose
- [ ] `main` is always green

---

## 11. Absolute Prohibitions
- [ ] No silent failures
- [ ] No commented-out code
- [ ] No magic numbers
- [ ] No UI logic in data layers
- [ ] No tests written solely for coverage
