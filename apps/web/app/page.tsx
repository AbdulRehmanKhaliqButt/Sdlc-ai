"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";

type Project = { id: string; name: string; description?: string | null };
type Story = { title: string; description: string; acceptanceCriteria: string[] };
type Analysis = {
  id: string;
  status: "PendingReview" | "Approved";
  transcript: string;
  result: { summary: string; userStories: Story[]; openQuestions: string[] };
};
type TestCase = {
  id: string;
  title: string;
  type: string;
  preconditions: string;
  steps: string[];
  expectedResult: string;
  acceptanceCriteria: string[];
};
type TestPlan = { id: string; status: string; testCases: TestCase[] };
type ImplementationTask = {
  id: string;
  title: string;
  description: string;
  filesLikelyAffected: string[];
  validation: string[];
};
type ImplementationPlan = {
  id: string;
  status: string;
  summary: string;
  tasks: ImplementationTask[];
};
type MemoryItem = {
  id: string;
  kind: string;
  content: string;
  tags: string[];
  createdAt: string;
};
type RepositoryFile = { path: string; sha?: string | null; content: string };
type RepositoryAnalysis = {
  id: string;
  repository: string;
  defaultBranch: string;
  signals: string[];
  relevantFiles: RepositoryFile[];
  analyzedAt: string;
};
type FileChange = { path: string; action: string; content: string; reason: string };
type SandboxCommandResult = {
  command: string;
  exitCode: number;
  durationMs: number;
  stdout: string;
  stderr: string;
  blocked: boolean;
};
type ValidationEvidence = {
  passed: boolean;
  repairCount: number;
  attempts: {
    attempt: number;
    proposalSummary: string;
    validation: {
      passed: boolean;
      repository: string;
      branch: string;
      changedFiles: string[];
      commands: SandboxCommandResult[];
    };
  }[];
  failureSummary?: string | null;
};
type DeliveryRun = {
  id: string;
  repository: string;
  defaultBranch: string;
  branchName?: string | null;
  status: string;
  proposal: {
    summary: string;
    changes: FileChange[];
    commands: string[];
    risks: string[];
  };
  validation?: ValidationEvidence | null;
  pullRequestNumber?: number | null;
  pullRequestUrl?: string | null;
};
type E2eProposal = {
  suggestedPath: string;
  playwrightSpec: string;
  traceability: string[];
};
type Workspace = {
  project: Project;
  latestAnalysis?: Analysis | null;
  latestTestPlan?: TestPlan | null;
  latestImplementationPlan?: ImplementationPlan | null;
  latestRepositoryAnalysis?: RepositoryAnalysis | null;
  latestDeliveryRun?: DeliveryRun | null;
  memory: MemoryItem[];
};

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API}${path}`, {
    ...options,
    headers: { "Content-Type": "application/json" },
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const body = await response.json();
      message = body.error ?? body.detail ?? message;
    } catch {}
    throw new Error(message);
  }

  return response.json();
}

function statusClass(done: boolean, active = false) {
  if (done) return "step done";
  if (active) return "step active";
  return "step";
}

export default function Home() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [workspace, setWorkspace] = useState<Workspace | null>(null);
  const [projectName, setProjectName] = useState("SDLC AI Demo");
  const [projectDescription, setProjectDescription] = useState("Browser-driven SDLC AI workflow");
  const [transcript, setTranscript] = useState(
    "We need administrators to deactivate users. Deactivated users must not be able to log in, existing sessions should be invalidated, and every deactivation must be audited. The UI must ask for confirmation."
  );
  const [memoryKind, setMemoryKind] = useState("architecture");
  const [memoryContent, setMemoryContent] = useState(
    "Preserve existing architecture boundaries and add tests for changed behavior."
  );
  const [memoryTags, setMemoryTags] = useState("architecture,tests");
  const [repository, setRepository] = useState("");
  const [repositoryQuery, setRepositoryQuery] = useState("Implement the approved requirements using existing repository conventions.");
  const [e2e, setE2e] = useState<E2eProposal | null>(null);
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [selectedChange, setSelectedChange] = useState(0);

  const analysis = workspace?.latestAnalysis ?? null;
  const qa = workspace?.latestTestPlan ?? null;
  const dev = workspace?.latestImplementationPlan ?? null;
  const repoAnalysis = workspace?.latestRepositoryAnalysis ?? null;
  const delivery = workspace?.latestDeliveryRun ?? null;

  const progress = useMemo(() => {
    const checks = [
      Boolean(analysis),
      analysis?.status === "Approved",
      Boolean(qa),
      qa?.status === "Approved",
      Boolean(dev),
      dev?.status === "Approved",
      Boolean(repoAnalysis),
      Boolean(delivery),
      delivery?.status === "PullRequestOpened",
    ];
    return Math.round((checks.filter(Boolean).length / checks.length) * 100);
  }, [analysis, qa, dev, repoAnalysis, delivery]);

  async function refreshProjects() {
    const list = await request<Project[]>("/api/projects");
    setProjects(list);
  }

  async function loadWorkspace(projectId: string) {
    setBusy("workspace");
    setError("");
    try {
      const data = await request<Workspace>(`/api/projects/${projectId}/workspace`);
      setWorkspace(data);
      if (data.latestAnalysis?.transcript) setTranscript(data.latestAnalysis.transcript);
      if (data.latestRepositoryAnalysis?.repository) setRepository(data.latestRepositoryAnalysis.repository);
      setSelectedChange(0);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not load project.");
    } finally {
      setBusy("");
    }
  }

  useEffect(() => {
    refreshProjects().catch((e) => setError(e instanceof Error ? e.message : "Could not load projects."));
  }, []);

  async function createProject(e: FormEvent) {
    e.preventDefault();
    setBusy("project");
    setError("");
    setNotice("");
    try {
      const project = await request<Project>("/api/projects", {
        method: "POST",
        body: JSON.stringify({ name: projectName, description: projectDescription }),
      });
      await refreshProjects();
      await loadWorkspace(project.id);
      setNotice("Project created. Start by analyzing a grooming transcript.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not create project.");
    } finally {
      setBusy("");
    }
  }

  async function runAndRefresh(label: string, action: () => Promise<unknown>, noticeText: string) {
    if (!workspace) return;
    setBusy(label);
    setError("");
    setNotice("");
    try {
      await action();
      await loadWorkspace(workspace.project.id);
      setNotice(noticeText);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Action failed.");
    } finally {
      setBusy("");
    }
  }

  async function analyzeRequirements() {
    if (!workspace) return;
    await runAndRefresh(
      "analysis",
      () =>
        request(`/api/projects/${workspace.project.id}/analyses`, {
          method: "POST",
          body: JSON.stringify({ transcript }),
        }),
      "Requirements generated. Review them before approval."
    );
  }

  async function approveAnalysis() {
    if (!workspace || !analysis) return;
    await runAndRefresh(
      "analysis-approve",
      () => request(`/api/projects/${workspace.project.id}/analyses/${analysis.id}/approve`, { method: "POST" }),
      "Requirements approved. QA planning is now unlocked."
    );
  }

  async function addMemory() {
    if (!workspace || !memoryContent.trim()) return;
    await runAndRefresh(
      "memory",
      () =>
        request(`/api/projects/${workspace.project.id}/memory`, {
          method: "POST",
          body: JSON.stringify({
            kind: memoryKind,
            content: memoryContent,
            tags: memoryTags.split(",").map((x) => x.trim()).filter(Boolean),
          }),
        }),
      "Project knowledge saved to the Project Brain."
    );
  }

  async function generateQa() {
    if (!workspace || !analysis) return;
    await runAndRefresh(
      "qa",
      () =>
        request(`/api/projects/${workspace.project.id}/qa/test-plans`, {
          method: "POST",
          body: JSON.stringify({ analysisId: analysis.id }),
        }),
      "QA plan generated. Review the tests before approval."
    );
  }

  async function approveQa() {
    if (!workspace || !qa) return;
    await runAndRefresh(
      "qa-approve",
      () => request(`/api/projects/${workspace.project.id}/qa/test-plans/${qa.id}/approve`, { method: "POST" }),
      "QA plan approved. Development planning is now unlocked."
    );
  }

  async function generateE2e() {
    if (!workspace || !qa) return;
    setBusy("e2e");
    setError("");
    try {
      const proposal = await request<E2eProposal>(
        `/api/projects/${workspace.project.id}/qa/test-plans/${qa.id}/e2e-proposal`,
        { method: "POST" }
      );
      setE2e(proposal);
      setNotice("Playwright proposal generated.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not generate Playwright proposal.");
    } finally {
      setBusy("");
    }
  }

  async function generateDev() {
    if (!workspace || !analysis || !qa) return;
    await runAndRefresh(
      "dev",
      () =>
        request(`/api/projects/${workspace.project.id}/development/plans`, {
          method: "POST",
          body: JSON.stringify({ analysisId: analysis.id, testPlanId: qa.id }),
        }),
      "Implementation plan generated. Review it before approval."
    );
  }

  async function approveDev() {
    if (!workspace || !dev) return;
    await runAndRefresh(
      "dev-approve",
      () => request(`/api/projects/${workspace.project.id}/development/plans/${dev.id}/approve`, { method: "POST" }),
      "Implementation plan approved. Repository delivery is now unlocked."
    );
  }

  async function analyzeRepository() {
    if (!workspace || !repository.trim()) return;
    await runAndRefresh(
      "repository",
      () =>
        request(`/api/projects/${workspace.project.id}/repositories/analyze`, {
          method: "POST",
          body: JSON.stringify({ repository: repository.trim(), query: repositoryQuery }),
        }),
      "Repository context captured for the developer agent."
    );
  }

  async function createDelivery() {
    if (!workspace || !dev || !repository.trim()) return;
    await runAndRefresh(
      "delivery",
      () =>
        request(`/api/projects/${workspace.project.id}/development/delivery-runs`, {
          method: "POST",
          body: JSON.stringify({ implementationPlanId: dev.id, repository: repository.trim(), branchName: null }),
        }),
      "Code proposal generated and validated in the isolated sandbox. No GitHub write has happened yet."
    );
  }

  async function retryValidation() {
    if (!workspace || !delivery) return;
    await runAndRefresh(
      "delivery-validate",
      () =>
        request(`/api/projects/${workspace.project.id}/development/delivery-runs/${delivery.id}/validate`, {
          method: "POST",
        }),
      "Sandbox validation completed. Review the latest evidence and repaired proposal."
    );
  }

  async function approveDelivery() {
    if (!workspace || !delivery) return;
    const ok = window.confirm(
      "This will create a GitHub branch, write the proposed files and open a pull request. Continue?"
    );
    if (!ok) return;

    await runAndRefresh(
      "delivery-approve",
      () =>
        request(`/api/projects/${workspace.project.id}/development/delivery-runs/${delivery.id}/approve`, {
          method: "POST",
        }),
      "Pull request created. Review GitHub CI and the diff before merging."
    );
  }

  const change = delivery?.proposal.changes[selectedChange];

  return (
    <main>
      <header className="topbar">
        <div>
          <span className="eyebrow">AI SOFTWARE DELIVERY</span>
          <h1>SDLC AI</h1>
          <p className="hero-copy">
            Turn grooming conversations into approved requirements, tests, implementation plans and reviewable pull requests.
          </p>
        </div>
        <div className="progress-card">
          <strong>{workspace ? `${progress}%` : "Ready"}</strong>
          <span>{workspace ? "workflow progress" : "create or select a project"}</span>
        </div>
      </header>

      {error && <div className="banner error-banner">{error}</div>}
      {notice && <div className="banner success-banner">{notice}</div>}

      <section className="project-bar panel">
        <div className="project-picker">
          <label htmlFor="existing-project">Existing project</label>
          <select
            id="existing-project"
            value={workspace?.project.id ?? ""}
            onChange={(e) => e.target.value && loadWorkspace(e.target.value)}
            disabled={busy === "workspace"}
          >
            <option value="">Select a project…</option>
            {projects.map((p) => (
              <option value={p.id} key={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </div>
        <form className="new-project" onSubmit={createProject}>
          <div>
            <label htmlFor="project-name">New project name</label>
            <input id="project-name" value={projectName} onChange={(e) => setProjectName(e.target.value)} />
          </div>
          <div>
            <label htmlFor="project-description">Description</label>
            <input id="project-description" value={projectDescription} onChange={(e) => setProjectDescription(e.target.value)} />
          </div>
          <button disabled={Boolean(busy) || !projectName.trim()}>
            {busy === "project" ? "Creating…" : "Create project"}
          </button>
        </form>
      </section>

      {!workspace ? (
        <section className="empty-state panel">
          <div className="empty-icon">01</div>
          <h2>Start with a project</h2>
          <p>Create a new project above, or select an existing one. The workspace will recover where you left off.</p>
        </section>
      ) : (
        <div className="workspace">
          <aside className="sidebar panel">
            <div className="project-title">
              <span className="eyebrow">PROJECT</span>
              <h2>{workspace.project.name}</h2>
              <p>{workspace.project.description}</p>
            </div>
            <nav>
              <div className={statusClass(Boolean(analysis), !analysis)}><span>1</span>Requirements</div>
              <div className={statusClass(analysis?.status === "Approved", Boolean(analysis) && analysis?.status !== "Approved")}><span>2</span>PO approval</div>
              <div className={statusClass(Boolean(qa), analysis?.status === "Approved" && !qa)}><span>3</span>QA plan</div>
              <div className={statusClass(qa?.status === "Approved", Boolean(qa) && qa?.status !== "Approved")}><span>4</span>QA approval</div>
              <div className={statusClass(Boolean(dev), qa?.status === "Approved" && !dev)}><span>5</span>Development</div>
              <div className={statusClass(dev?.status === "Approved", Boolean(dev) && dev?.status !== "Approved")}><span>6</span>Dev approval</div>
              <div className={statusClass(Boolean(repoAnalysis), dev?.status === "Approved" && !repoAnalysis)}><span>7</span>Repository</div>
              <div className={statusClass(Boolean(delivery), Boolean(repoAnalysis) && !delivery)}><span>8</span>Code proposal</div>
              <div className={statusClass(
                delivery?.status === "PullRequestOpened",
                delivery?.status === "ValidatedPendingApproval" || delivery?.status === "ValidationFailed"
              )}><span>9</span>Validate & PR</div>
            </nav>
          </aside>

          <section className="content">
            <section className="panel stage">
              <div className="stage-header">
                <div>
                  <span className="eyebrow">01 · REQUIREMENTS</span>
                  <h2>Grooming → structured requirements</h2>
                </div>
                {analysis && <span className="status">{analysis.status}</span>}
              </div>
              <label htmlFor="grooming-transcript">Grooming transcript</label>
              <textarea id="grooming-transcript" rows={10} value={transcript} onChange={(e) => setTranscript(e.target.value)} />
              <div className="actions">
                <button onClick={analyzeRequirements} disabled={Boolean(busy) || !transcript.trim()}>
                  {busy === "analysis" ? "Analyzing…" : analysis ? "Generate new analysis" : "Analyze requirements"}
                </button>
                {analysis && analysis.status !== "Approved" && (
                  <button className="secondary" onClick={approveAnalysis} disabled={Boolean(busy)}>
                    {busy === "analysis-approve" ? "Approving…" : "Approve requirements"}
                  </button>
                )}
              </div>
              {analysis && (
                <div className="result-block">
                  <p>{analysis.result.summary}</p>
                  {analysis.result.userStories.map((story, i) => (
                    <article key={i}>
                      <h3>{story.title}</h3>
                      <p>{story.description}</p>
                      <strong>Acceptance criteria</strong>
                      <ul>{story.acceptanceCriteria.map((item) => <li key={item}>{item}</li>)}</ul>
                    </article>
                  ))}
                  {analysis.result.openQuestions.length > 0 && (
                    <>
                      <h3>Open questions</h3>
                      <ul>{analysis.result.openQuestions.map((item) => <li key={item}>{item}</li>)}</ul>
                    </>
                  )}
                </div>
              )}
            </section>

            <section className="panel stage">
              <div className="stage-header">
                <div>
                  <span className="eyebrow">PROJECT BRAIN</span>
                  <h2>Persistent engineering context</h2>
                </div>
                <span className="status">{workspace.memory.length} memories</span>
              </div>
              <div className="three-col">
                <div>
                  <label htmlFor="memory-kind">Kind</label>
                  <select id="memory-kind" value={memoryKind} onChange={(e) => setMemoryKind(e.target.value)}>
                    <option value="architecture">Architecture</option>
                    <option value="coding-convention">Coding convention</option>
                    <option value="domain">Domain knowledge</option>
                    <option value="decision">Decision</option>
                    <option value="review-feedback">Review feedback</option>
                  </select>
                </div>
                <div className="wide">
                  <label htmlFor="memory-content">Knowledge</label>
                  <input id="memory-content" value={memoryContent} onChange={(e) => setMemoryContent(e.target.value)} />
                </div>
                <div>
                  <label htmlFor="memory-tags">Tags</label>
                  <input id="memory-tags" value={memoryTags} onChange={(e) => setMemoryTags(e.target.value)} />
                </div>
              </div>
              <button onClick={addMemory} disabled={Boolean(busy) || !memoryContent.trim()}>
                {busy === "memory" ? "Saving…" : "Add to Project Brain"}
              </button>
              {workspace.memory.length > 0 && (
                <div className="memory-list">
                  {workspace.memory.slice(0, 6).map((item) => (
                    <div className="memory" key={item.id}>
                      <strong>{item.kind}</strong>
                      <p>{item.content}</p>
                      <div className="tags">{item.tags.map((tag) => <span key={tag}>{tag}</span>)}</div>
                    </div>
                  ))}
                </div>
              )}
            </section>

            <section className="panel stage">
              <div className="stage-header">
                <div>
                  <span className="eyebrow">02 · QA</span>
                  <h2>Acceptance criteria → test plan</h2>
                </div>
                {qa && <span className="status">{qa.status}</span>}
              </div>
              {!qa ? (
                <p>Approve requirements first, then generate the QA plan.</p>
              ) : (
                <div className="cards">
                  {qa.testCases.map((test) => (
                    <article className="mini-card" key={test.id}>
                      <span className="tiny">{test.id} · {test.type}</span>
                      <h3>{test.title}</h3>
                      <p>{test.expectedResult}</p>
                    </article>
                  ))}
                </div>
              )}
              <div className="actions">
                <button onClick={generateQa} disabled={Boolean(busy) || analysis?.status !== "Approved"}>
                  {busy === "qa" ? "Generating…" : qa ? "Regenerate QA plan" : "Generate QA plan"}
                </button>
                {qa && qa.status !== "Approved" && (
                  <button className="secondary" onClick={approveQa} disabled={Boolean(busy)}>
                    {busy === "qa-approve" ? "Approving…" : "Approve QA plan"}
                  </button>
                )}
                <button className="ghost" onClick={generateE2e} disabled={Boolean(busy) || qa?.status !== "Approved"}>
                  {busy === "e2e" ? "Generating…" : "Generate Playwright proposal"}
                </button>
              </div>
              {e2e && (
                <details className="code-details" open>
                  <summary>{e2e.suggestedPath}</summary>
                  <pre>{e2e.playwrightSpec}</pre>
                </details>
              )}
            </section>

            <section className="panel stage">
              <div className="stage-header">
                <div>
                  <span className="eyebrow">03 · DEVELOPMENT</span>
                  <h2>Approved QA → implementation plan</h2>
                </div>
                {dev && <span className="status">{dev.status}</span>}
              </div>
              {dev ? (
                <>
                  <p>{dev.summary}</p>
                  <div className="cards">
                    {dev.tasks.map((task) => (
                      <article className="mini-card" key={task.id}>
                        <span className="tiny">{task.id}</span>
                        <h3>{task.title}</h3>
                        <p>{task.description}</p>
                        <strong>Validation</strong>
                        <ul>{task.validation.map((v) => <li key={v}>{v}</li>)}</ul>
                      </article>
                    ))}
                  </div>
                </>
              ) : (
                <p>Approve the QA plan to unlock implementation planning.</p>
              )}
              <div className="actions">
                <button onClick={generateDev} disabled={Boolean(busy) || qa?.status !== "Approved"}>
                  {busy === "dev" ? "Generating…" : dev ? "Regenerate implementation plan" : "Generate implementation plan"}
                </button>
                {dev && dev.status !== "Approved" && (
                  <button className="secondary" onClick={approveDev} disabled={Boolean(busy)}>
                    {busy === "dev-approve" ? "Approving…" : "Approve implementation plan"}
                  </button>
                )}
              </div>
            </section>

            <section className="panel stage">
              <div className="stage-header">
                <div>
                  <span className="eyebrow">04 · REPOSITORY INTELLIGENCE</span>
                  <h2>Connect the target GitHub repository</h2>
                </div>
                {repoAnalysis && <span className="status">{repoAnalysis.defaultBranch}</span>}
              </div>
              <div className="two-col">
                <div>
                  <label htmlFor="target-repository">Repository</label>
                  <input
                    id="target-repository"
                    placeholder="owner/repository"
                    value={repository}
                    onChange={(e) => setRepository(e.target.value)}
                  />
                </div>
                <div>
                  <label htmlFor="repository-query">Repository task/query</label>
                  <input id="repository-query" value={repositoryQuery} onChange={(e) => setRepositoryQuery(e.target.value)} />
                </div>
              </div>
              <button onClick={analyzeRepository} disabled={Boolean(busy) || dev?.status !== "Approved" || !repository.trim()}>
                {busy === "repository" ? "Inspecting repository…" : "Analyze repository"}
              </button>
              {repoAnalysis && (
                <div className="result-block">
                  <div className="tags">{repoAnalysis.signals.map((signal) => <span key={signal}>{signal}</span>)}</div>
                  <h3>Relevant files</h3>
                  <div className="file-list">
                    {repoAnalysis.relevantFiles.map((file) => <code key={file.path}>{file.path}</code>)}
                  </div>
                </div>
              )}
            </section>

            <section className="panel stage">
              <div className="stage-header">
                <div>
                  <span className="eyebrow">05 · AI DEVELOPER</span>
                  <h2>Repository context → reviewable code proposal</h2>
                </div>
                {delivery && <span className="status">{delivery.status}</span>}
              </div>
              <p>
                Generating a delivery run does <strong>not</strong> modify GitHub. The proposal is applied inside an isolated
                runner, validated with allowlisted build/test commands, and repaired automatically when validation fails.
              </p>
              <button onClick={createDelivery} disabled={Boolean(busy) || dev?.status !== "Approved" || !repository.trim()}>
                {busy === "delivery" ? "Generating, testing & repairing…" : "Generate + validate code proposal"}
              </button>

              {delivery && (
                <div className="delivery">
                  <div className="proposal-summary">
                    <h3>{delivery.proposal.summary}</h3>
                    {delivery.proposal.risks.length > 0 && (
                      <div className="risk-box">
                        <strong>Review risks</strong>
                        <ul>{delivery.proposal.risks.map((risk) => <li key={risk}>{risk}</li>)}</ul>
                      </div>
                    )}
                  </div>

                  {delivery.proposal.changes.length === 0 ? (
                    <div className="info-box">
                      No code changes were proposed. If the risk says deterministic mode is active, configure
                      <code> AI_PROVIDER=openai </code> and an API key, then restart the stack.
                    </div>
                  ) : (
                    <div className="diff-browser">
                      <div className="change-tabs">
                        {delivery.proposal.changes.map((item, i) => (
                          <button
                            className={selectedChange === i ? "change-tab selected" : "change-tab"}
                            onClick={() => setSelectedChange(i)}
                            key={item.path}
                          >
                            <span>{item.action}</span>{item.path}
                          </button>
                        ))}
                      </div>
                      {change && (
                        <div className="change-view">
                          <div className="change-heading">
                            <strong>{change.path}</strong>
                            <span>{change.reason}</span>
                          </div>
                          <pre>{change.content}</pre>
                        </div>
                      )}
                    </div>
                  )}

                  {delivery.proposal.commands.length > 0 && (
                    <>
                      <h3>Validation commands</h3>
                      <div className="file-list">{delivery.proposal.commands.map((cmd) => <code key={cmd}>{cmd}</code>)}</div>
                    </>
                  )}

                  {delivery.validation && (
                    <div className={delivery.validation.passed ? "validation-box validation-pass" : "validation-box validation-fail"}>
                      <div className="validation-title">
                        <strong>{delivery.validation.passed ? "Sandbox validation passed" : "Sandbox validation failed"}</strong>
                        <span>{delivery.validation.repairCount} AI repair{delivery.validation.repairCount === 1 ? "" : "s"}</span>
                      </div>
                      {delivery.validation.failureSummary && <p>{delivery.validation.failureSummary}</p>}
                      {delivery.validation.attempts.map((attempt) => (
                        <details className="validation-attempt" key={attempt.attempt} open={attempt.attempt === delivery.validation?.attempts.length}>
                          <summary>
                            Attempt {attempt.attempt} · {attempt.validation.passed ? "passed" : "failed"} · {attempt.proposalSummary}
                          </summary>
                          {attempt.validation.commands.map((result) => (
                            <div className="command-result" key={result.command}>
                              <div>
                                <code>{result.command}</code>
                                <span className={result.exitCode === 0 ? "exit-ok" : "exit-bad"}>
                                  exit {result.exitCode} · {result.durationMs} ms{result.blocked ? " · blocked" : ""}
                                </span>
                              </div>
                              {(result.stdout || result.stderr) && (
                                <details>
                                  <summary>Logs</summary>
                                  <pre>{[result.stdout, result.stderr].filter(Boolean).join("\n")}</pre>
                                </details>
                              )}
                            </div>
                          ))}
                        </details>
                      ))}
                    </div>
                  )}

                  {delivery.status === "ValidationFailed" && (
                    <button className="secondary" onClick={retryValidation} disabled={Boolean(busy)}>
                      {busy === "delivery-validate" ? "Revalidating…" : "Retry validation & repair"}
                    </button>
                  )}

                  {delivery.status === "ValidatedPendingApproval" && (
                    <button className="danger-action" onClick={approveDelivery} disabled={Boolean(busy)}>
                      {busy === "delivery-approve" ? "Creating branch and PR…" : "Approve validated changes & create pull request"}
                    </button>
                  )}

                  {delivery.status === "PullRequestOpened" && delivery.pullRequestUrl && (
                    <div className="pr-success">
                      <strong>Pull request #{delivery.pullRequestNumber} created</strong>
                      <a href={delivery.pullRequestUrl} target="_blank" rel="noreferrer">Open pull request ↗</a>
                    </div>
                  )}
                </div>
              )}
            </section>

            <section className="panel stage setup">
              <span className="eyebrow">LOCAL SETUP</span>
              <h2>What the two modes mean</h2>
              <div className="mode-grid">
                <div>
                  <strong>Safe demo mode</strong>
                  <code>AI_PROVIDER=deterministic</code>
                  <p>Requirements and planning work. The developer agent deliberately produces zero code mutations.</p>
                </div>
                <div>
                  <strong>Real AI delivery</strong>
                  <code>AI_PROVIDER=openai</code>
                  <p>Requires an OpenAI API key. GitHub writes additionally require a fine-grained GITHUB_TOKEN.</p>
                </div>
              </div>
            </section>
          </section>
        </div>
      )}
    </main>
  );
}
