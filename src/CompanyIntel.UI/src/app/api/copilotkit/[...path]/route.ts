import {
  CopilotRuntime,
  createCopilotEndpoint,
} from "@copilotkit/runtime/v2";
import { HttpAgent, Middleware } from "@ag-ui/client";
import { map } from "rxjs/operators";
import { handle } from "hono/vercel";

const agentUrl = process.env.AGENT_URL || "http://localhost:8000";

// @ag-ui/client@0.0.45 ThinkingToReasoningMiddleware bug: emits role="assistant"
// on REASONING_MESSAGE_START, but @ag-ui/core schema expects role="reasoning".
// Inserted at index 0 to run outermost (after the buggy middleware transforms events).
class FixReasoningRole extends Middleware {
  run(
    input: Parameters<Middleware["run"]>[0],
    next: Parameters<Middleware["run"]>[1],
  ) {
    return this.runNext(input, next).pipe(
      map((event) => {
        if (
          event.type === "REASONING_MESSAGE_START" &&
          "role" in event &&
          event.role !== "reasoning"
        ) {
          return { ...event, role: "reasoning" as const };
        }
        return event;
      }),
    );
  }
}

const agent = new HttpAgent({ url: agentUrl });
// eslint-disable-next-line @typescript-eslint/no-explicit-any -- accessing private array to control middleware ordering
(agent as any).middlewares.unshift(new FixReasoningRole());

const runtime = new CopilotRuntime({
  agents: {
    agentic_chat: agent,
  },
});

const app = createCopilotEndpoint({
  runtime,
  basePath: "/api/copilotkit",
});

export const POST = handle(app);
export const GET = handle(app);
