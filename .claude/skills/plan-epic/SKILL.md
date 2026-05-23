---
name: plan-epic
description: 'Invoke for epic planning requests: "plan epic N", "design the [name] epic", "break [epic] into tasks", "technical design doc for epic N", "tackle the [name] epic — where do we start?", or "think through the [name] epic before we commit". Epics may be referenced by number (epic 1, epic 06) or name (markdown viewer epic, distribution epic, infrastructure adapters epic, CLI presentation layer). Produces two gated artifacts: a technical design document, then an implementation plan. Skip when the user only wants to understand what an epic covers, explain existing architecture, or design a single isolated feature outside of epic planning context.'
---

# plan-epic

Turns a mked epic into a reviewed technical design and a checkable implementation plan, in two
gated phases. Each phase produces a file on disk and waits for explicit user approval before
proceeding.

---

## Step 0 — Identify the epic

The user will reference an epic by number, name, or both. Epic files live in `docs/epics/` and
follow the pattern `NN-slug.md` (e.g. `01-domain-core.md`, `04-markdown-viewer.md`).

If the reference is ambiguous, list the available epics and ask the user to pick one before
doing anything else.

Throughout the skill, derive the output filename prefix from the epic file: e.g. epic
`04-markdown-viewer.md` → prefix `04-markdown-viewer`.

---

## Phase 1 — Technical Design

### 1a. Gather context

Read these in parallel before writing anything:

- The epic file (`docs/epics/NN-slug.md`)
- All files in `docs/architecture/`
- Any existing designs in `docs/designs/` that may overlap with this epic
- Source files in the projects the epic touches — use the epic's features as a guide to which
  namespaces or `*.cs` files are most relevant

The goal is to understand what already exists so the design describes only what's genuinely new
or changed.

### 1b. Write the design document

Output path: `docs/designs/{prefix}-design.md`

Use the template in `assets/design-template.md`. Fill every section. If something is genuinely
unknown, write `TBD` and add an entry in the Open Questions table so it doesn't get lost.

### 1c. Request approval

Tell the user the design has been saved and ask them to review it. Do not start Phase 2 until
the user gives clear approval (e.g. "looks good", "approved", "proceed", "go ahead").

If the user requests changes, update the file and ask again. Repeat until approved.

---

## Phase 2 — Implementation Plan

### 2a. Write the implementation plan

Output path: `docs/plans/{prefix}-plan.md`

Use the template in `assets/plan-template.md`. Break the work into **feature-slice tasks** —
one task per logical feature or coherent sub-deliverable from the epic. A good slice is
independently testable and completable in a focused session.

Rules for the task list:

- Number tasks sequentially starting at 1
- Each task has a bold name, a 2–4 sentence description of what "done" looks like, and — when
  it has prerequisites — a `Depends on: Task N[, Task M]` line immediately after the description
- Omit the depends-on line for tasks with no prerequisites
- Order tasks so no task appears before all of its dependencies
- Reference the approved design document and the epic for traceability

### 2b. Request approval

Tell the user the plan has been saved and ask them to review it. The plan is complete when the
user explicitly approves it. Apply any requested changes and ask again until approved.

---

## Templates

Read `assets/design-template.md` when writing the design document.
Read `assets/plan-template.md` when writing the implementation plan.
