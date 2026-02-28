import { NextResponse } from "next/server";

const agentUrl = process.env.AGENT_URL || "http://localhost:8000";

export async function GET() {
  const res = await fetch(`${agentUrl}/api/documents/history`);

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    return NextResponse.json({ error: text }, { status: res.status });
  }

  const data = await res.json().catch(() => []);
  return NextResponse.json(data);
}
