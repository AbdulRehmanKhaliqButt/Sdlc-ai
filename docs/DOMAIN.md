# Domain model

The platform models AI output as a proposal that moves through explicit human gates.

## Core aggregates

### Workspace
Security and tenancy boundary.

### Project
A software-delivery initiative containing source discussions and analyses.

### SourceArtifact
Transcript, notes, documents, screenshots, or future meeting media used as evidence.

### AnalysisRun
Immutable record of an AI execution: input reference, prompt version, model/provider metadata, output, timing, and status.

### RequirementProposal
Structured proposal containing summary, user stories, acceptance criteria, risks, dependencies, and open questions.

### Approval
Human decision over a proposal. Approval is separate from generation so AI cannot self-approve.

### DeliveryArtifact
Future Jira issue, test plan, implementation plan, pull request, E2E run, or release evidence generated from an approved proposal.

## State model

```text
Draft -> PendingReview -> Approved
                    \-> ChangesRequested
                    \-> Rejected

Approved -> downstream proposal generation
```

Downstream agents consume approved versions only.
