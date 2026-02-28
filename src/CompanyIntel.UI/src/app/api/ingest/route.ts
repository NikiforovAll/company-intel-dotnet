import { NextRequest, NextResponse } from "next/server";

const agentUrl = process.env.AGENT_URL || "http://localhost:8000";

export async function POST(req: NextRequest) {
  const formData = await req.formData();

  const res = await fetch(`${agentUrl}/api/ingest`, {
    method: "POST",
    body: formData,
  });

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    return NextResponse.json({ error: text }, { status: res.status });
  }

  const data = await res.json().catch(() => ({}));
  return NextResponse.json(data);
}
