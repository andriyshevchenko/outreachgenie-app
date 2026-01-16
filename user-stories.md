# Agentic Marketing & Sales Copilot

## Product & Requirements Specification (User Stories)

---

## 1. Purpose and Scope

The system is a **desktop-based agentic copilot** for marketing and sales workflows (e.g., LinkedIn outreach campaigns). It is designed to execute **long-running, multi-step campaigns** with strict procedural guarantees, durable state, and full auditability.

Large Language Models (LLMs) are used strictly as **reasoning and generation components**. They are **not** the system of record and are not trusted to remember obligations, enforce invariants, or maintain state.

The system must tolerate:

* Context truncation
* Conversation summarization
* Model hallucination
* Model non-determinism

Without violating campaign correctness.

---

## 2. Target Users

### 2.1 Marketing Operator

A user running outreach or lead-generation campaigns who expects:

* Predictable execution
* Visibility into agent actions
* Recoverability after interruptions

### 2.2 Power User / Growth Engineer

A technically proficient user who expects:

* Deterministic workflows
* Full observability
* Strict guarantees that no critical steps are skipped

### 2.3 Compliance-Oriented User

A user who requires:

* Complete audit trails
* Reconstructable system state
* Verifiable logs of all agent actions

---

## 3. Core Design Principles

1. **LLM is fallible**: The system assumes the LLM will forget, skip steps, or produce invalid output.
2. **State is externalized**: All progress, memory, and artifacts live outside the model.
3. **Procedural enforcement**: Critical steps cannot be skipped, even if the LLM attempts to.
4. **Observability-first**: Every meaningful action is logged.
5. **Recoverability**: Any campaign can be resumed from durable state.

---

## 4. High-Level Capabilities

### 4.1 Campaign Lifecycle Management

**User Story**

> As a user, I want to create and manage campaigns so that each campaign has isolated state, data, and history.

**Acceptance Criteria**

* Each campaign has a unique identifier
* Campaigns can be paused, resumed, and restored
* Campaign metadata persists across sessions

---

### 4.2 Task Planning and Enforcement

**User Story**

> As a user, I want the agent to generate TODO items and strictly follow them so that no required step is skipped.

**Acceptance Criteria**

* Tasks are generated as explicit, persistent artifacts
* Tasks have states (e.g., pending, completed, blocked)
* Progress is derived from task completion, not from model output
* The agent cannot advance without satisfying required tasks

---

### 4.3 Lead Discovery

**User Story**

> As a user, I want the agent to search for leads so that I can build targeted outreach lists.

**Acceptance Criteria**

* Lead discovery actions are explicitly logged
* Lead sources are recorded
* Duplicate leads are detected and handled

---

### 4.4 Lead Scoring and Prioritization

**User Story**

> As a user, I want the agent to apply heuristic scoring to leads so that higher-value leads are prioritized.

**Acceptance Criteria**

* Scoring criteria are explicit and reproducible
* Scores are stored with timestamped rationale
* Score changes are tracked over time

---

### 4.5 Persistent Data Storage

**User Story**

> As a user, I want all leads and campaign artifacts stored in a database so that no data is lost.

**Acceptance Criteria**

* Leads persist independently of the agent process
* Database reflects the current campaign state in near real time
* Data integrity is preserved across restarts

---

### 4.6 Artifact Management

**User Story**

> As a user, I want the agent to store generated artifacts (e.g., Excel files, context documents) so that I can review and reuse them.

**Acceptance Criteria**

* Artifacts are versioned
* Artifact creation and deletion are logged
* Temporary files are cleaned up deterministically

---

### 4.7 Event Logging

**User Story**

> As a user, I want every data mutation recorded so that the system can be restored to an earlier state.

**Acceptance Criteria**

* Inserts, updates, and deletes are logged
* Logs are append-only
* System state can be reconstructed from logs

---

### 4.8 Audit Logging

**User Story**

> As a user, I want a detailed audit log so that I can inspect what the agent actually did.

**Acceptance Criteria**

* Every tool call is logged
* Every browser interaction is logged
* Every file operation is logged
* Logs are timestamped and ordered

---

## 5. Agent Behavior Model

### 5.1 Algorithm Execution

**User Story**

> As a user, I want the agent to follow a predefined algorithm so that campaign execution is predictable.

**Acceptance Criteria**

* Algorithm steps are explicit and externally enforced
* The agent cannot skip mandatory steps
* Deviations are detected and blocked

---

### 5.2 Plan Mode vs Execution Mode

**User Story**

> As a user, I want the agent to separate planning from execution so that plans can be inspected before actions occur.

**Acceptance Criteria**

* Plans exist as durable documents
* Execution references the plan, not conversation history
* Plan changes are versioned

---

## 6. Memory and Context Management

### 6.1 Durable Memory

**User Story**

> As a user, I want campaign memory to persist independently of the conversation so that context loss does not break execution.

**Acceptance Criteria**

* Campaign context is stored outside the LLM
* Memory survives summarization or truncation
* State restoration does not depend on chat history

---

### 6.2 Conversational Memory

**User Story**

> As a user, I want conversational context to assist reasoning without becoming a source of truth.

**Acceptance Criteria**

* Conversation summaries are treated as advisory
* Authoritative state is always external

---

## 7. Failure Handling and Recovery

**User Story**

> As a user, I want the system to recover from crashes or interruptions without losing progress.

**Acceptance Criteria**

* Campaigns can resume mid-execution
* Incomplete steps are detectable
* No silent failure modes exist

---

## 8. Desktop Application Context

**User Story**

> As a user, I want this to run as a desktop application with a browser-based UI so that I have local control and visibility.

**Acceptance Criteria**

* UI reflects real campaign state
* Agent actions are observable in near real time
* Local execution does not reduce auditability

---

## 9. Problem Statement: GitHub Copilot Experience

### 9.1 Observed Failures

**User Story**

> As a developer, I tried to implement this system using prompt-based agents in GitHub Copilot but encountered systemic limitations.

**Documented Issues**

* The agent failed to consistently follow TODO lists
* Critical steps (logging, persistence) were skipped
* Conversation summarization caused state loss
* Token usage increased rapidly for long campaigns
* Model behavior was non-deterministic

---

### 9.2 Conclusion

**User Story**

> As a developer, I concluded that prompt-only agent design is insufficient for this class of system.

**Outcome**

* LLMs cannot be trusted as workflow engines
* Enforcement must live in deterministic system layers
* The agent must be treated as an unreliable collaborator, not an executor

---

## 10. Non-Goals

* No assumption that the LLM is obedient
* No reliance on chat history for correctness
* No implicit guarantees based on prompt wording

---

## 11. Success Criteria

The system is successful if:

* No critical step can be skipped
* All actions are auditable
* Campaigns are recoverable
* LLM failures do not corrupt state

---

**End of Specification**
