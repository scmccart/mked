# Epic NN — [Epic Name]: Technical Design

> **Epic**: [`docs/epics/NN-slug.md`](../../docs/epics/NN-slug.md)
> **Status**: Draft
> **Date**: YYYY-MM-DD

---

## Goals

What this epic must deliver. Each goal should be concrete and verifiable.

-
-

## Non-Goals

What is explicitly out of scope for this epic.

-
-

---

## Architecture Overview

How this epic's work fits into the Clean Architecture layers. Describe which projects are
touched and what role each plays.

| Layer | Project | Role |
|-------|---------|------|
| Domain | `Mked.Domain` | |
| Application | `Mked.Application` | |
| Infrastructure | `Mked.Infrastructure` | |
| Presentation | `Mked.Console` | |

Note any cross-cutting concerns (e.g., AOT/trim constraints, new NuGet dependencies).

---

## Key Types and Interfaces

New or significantly modified public types this epic introduces.

### New Types

| Type | Kind | Project | Purpose |
|------|------|---------|---------|
| `TypeName` | record / class / interface | `Mked.Domain` | |

### Modified Types

| Type | Change | Reason |
|------|--------|--------|

---

## Data Flow / Sequence

How the primary use cases flow through the system. Use a numbered sequence or a Mermaid diagram
— whichever is clearest. One subsection per major use case.

### Use Case: [Name]

1.
2.
3.

---

## Error Handling Strategy

How failures are represented and propagated in this epic.

- **New `MkedError` variants** (if any):
- **Error production boundaries** — where are errors created vs. passed through?
- **User-visible failures** — which error cases surface to the terminal?

---

## Testing Approach

How the work will be verified.

- **Unit tests**: which use cases or types get unit tests, and with what fakes?
- **Integration tests** (if applicable):
- **Architecture tests** (if applicable): new ArchUnitNet rules to enforce layer constraints?

---

## Open Questions

Decisions that must be made before or during implementation.

| # | Question | Status |
|---|----------|--------|
| 1 | | Open |
