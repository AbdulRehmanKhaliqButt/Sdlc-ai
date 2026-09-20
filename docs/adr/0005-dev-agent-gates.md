# ADR 0005: Development agent requires approved requirements and QA plan

Status: Accepted

The development agent may generate an implementation plan only when both the source requirement analysis and its QA test plan are human-approved.

Code-changing capabilities will consume approved implementation plans only. Repository writes will occur on isolated branches, never directly on the default branch. CI and human review remain merge gates.
