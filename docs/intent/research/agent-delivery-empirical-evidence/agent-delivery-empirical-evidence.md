# Agent Delivery Empirical Evidence

Five studies and reports from 2025 to 2026 back the way LiteSpec v7 reaches agents. Long unfocused context harms output, and small scoped context with guardrails restores it.

## Findings

Structured task context with independent verification raises agent success rates [ETH Zurich study][eth-zurich-agent-delivery]. Recall and answer quality fall as input tokens grow, even inside large windows [IFScale study][ifscale-context-study].

Longer context windows do not mean better attention across the window [Chroma report][chroma-context-rot]. Sending less, focused context beats sending everything available [Chroma report][chroma-context-rot] [Anthropic post][anthropic-context-engineering].

Harness structure around agents matters more than model choice alone [OpenAI post][openai-harness-engineering]. Small scoped context pieces with clear guardrails compound into reliable runs [Anthropic post][anthropic-context-engineering] [OpenAI post][openai-harness-engineering].

LiteSpec v7 delivery follows these findings: thin protocols, path-scoped rules, a 4-tool MCP surface, deterministic hooks, and independent verification [ETH Zurich study][eth-zurich-agent-delivery] [IFScale study][ifscale-context-study] [Chroma report][chroma-context-rot] [OpenAI post][openai-harness-engineering] [Anthropic post][anthropic-context-engineering].

## Source Notes

The ETH Zurich study [ETH Zurich study][eth-zurich-agent-delivery] was read from its arXiv abstract page on 20 November 2026. It covers measured gains from structured task context and verification.

The IFScale study [IFScale study][ifscale-context-study] was read from its arXiv abstract page on 20 November 2026. It covers measured recall loss as input tokens grow.

The Chroma report [Chroma report][chroma-context-rot] was read from the report page on 20 November 2026. It covers measured performance loss as context grows and what to send instead.

The OpenAI post [OpenAI post][openai-harness-engineering] was read from the post page on 20 November 2026. It covers structuring context and guardrails around coding agents.

The Anthropic post [Anthropic post][anthropic-context-engineering] was read from the post page on 20 November 2026. It covers small focused context and compounding small wins.
