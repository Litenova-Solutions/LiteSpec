import type { APIRoute } from "astro";
import { rootLlmsText } from "../lib/agent-text";
import { loadAgentProtocolRegion, renderAgentProtocol, siteDefaultValues } from "../lib/agent-protocol";

export const GET: APIRoute = () => {
  const agentProtocol = renderAgentProtocol(loadAgentProtocolRegion(), siteDefaultValues());
  return new Response(rootLlmsText(agentProtocol), {
    headers: { "Content-Type": "text/plain; charset=utf-8" },
  });
};
