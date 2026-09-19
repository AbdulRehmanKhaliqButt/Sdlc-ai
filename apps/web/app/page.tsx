"use client";
import {FormEvent,useState} from "react";

type Story={title:string;description:string;acceptanceCriteria:string[]};
type Analysis={id:string;status:string;result:{summary:string;userStories:Story[];openQuestions:string[]}};

const API=process.env.NEXT_PUBLIC_API_URL??"http://localhost:8080";

export default function Home(){
 const [projectId,setProjectId]=useState("");
 const [name,setName]=useState("SDLC AI Demo");
 const [transcript,setTranscript]=useState("We need an AI assistant that converts grooming discussions into reviewable user stories. The PO must approve requirements before downstream work starts.");
 const [analysis,setAnalysis]=useState<Analysis|null>(null);
 const [busy,setBusy]=useState(false);
 const [error,setError]=useState("");

 async function ensureProject(){
  if(projectId)return projectId;
  const r=await fetch(`${API}/api/projects`,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({name,description:"Created from SDLC AI workspace"})});
  if(!r.ok)throw new Error("Could not create project");
  const p=await r.json(); setProjectId(p.id); return p.id;
 }
 async function analyze(e:FormEvent){e.preventDefault();setBusy(true);setError("");
  try{const id=await ensureProject();const r=await fetch(`${API}/api/projects/${id}/analyses`,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({transcript})});if(!r.ok)throw new Error("Analysis failed");setAnalysis(await r.json());}
  catch(x){setError(x instanceof Error?x.message:"Unexpected error");}finally{setBusy(false)}
 }
 async function approve(){if(!analysis)return;setBusy(true);const r=await fetch(`${API}/api/projects/${projectId}/analyses/${analysis.id}/approve`,{method:"POST"});setAnalysis(await r.json());setBusy(false)}
 return <main><header><div><span className="eyebrow">AI SOFTWARE DELIVERY</span><h1>SDLC AI</h1><p>Turn grooming conversations into structured engineering work while keeping humans in control.</p></div><span className="badge">Phase 1 · Human in the loop</span></header>
 <section className="grid"><form className="panel" onSubmit={analyze}><h2>Grooming workspace</h2><label>Project</label><input value={name} onChange={e=>setName(e.target.value)} disabled={!!projectId}/><label>Transcript</label><textarea value={transcript} onChange={e=>setTranscript(e.target.value)} rows={15}/><button disabled={busy}>{busy?"Working…":"Analyze requirements"}</button>{error&&<p className="error">{error}</p>}</form>
 <section className="panel"><h2>Review</h2>{!analysis?<div className="empty">Run an analysis to generate reviewable requirements.</div>:<><div className="status">{analysis.status}</div><p>{analysis.result.summary}</p>{analysis.result.userStories.map((s,i)=><article key={i}><h3>{s.title}</h3><p>{s.description}</p><strong>Acceptance criteria</strong><ul>{s.acceptanceCriteria.map(x=><li key={x}>{x}</li>)}</ul></article>)}<h3>Open questions</h3><ul>{analysis.result.openQuestions.map(x=><li key={x}>{x}</li>)}</ul>{analysis.status!=="Approved"&&<button onClick={approve} disabled={busy}>Approve analysis</button>}</>}</section></section></main>
}