---
name: markdown-documentation
description: >
  Write and edit Markdown documentation using GitHub Flavored Markdown. Use for
  READMEs, docs pages, wiki content, and any task that creates or changes
  Markdown structure such as tables, lists, links, images, Mermaid diagrams, or
  code blocks.
metadata:
  version: "1.1"
---

# Markdown Documentation

## Reference Loading Rules

Before editing a Markdown construct, MUST load the matching reference file.

- Tables: [references/tables.md](/.agents/skills/markdown-documentation/references/tables.md)
- Lists: [references/lists.md](/.agents/skills/markdown-documentation/references/lists.md)
- Links and images: [references/links-and-images.md](/.agents/skills/markdown-documentation/references/links-and-images.md)
- Mermaid diagrams: [references/mermaid-diagrams.md](/.agents/skills/markdown-documentation/references/mermaid-diagrams.md)
- Extended syntax: [references/extended-syntax-github-flavored-markdown.md](/.agents/skills/markdown-documentation/references/extended-syntax-github-flavored-markdown.md)
- Collapsible sections and badges: [references/collapsible-sections.md](/.agents/skills/markdown-documentation/references/collapsible-sections.md)
- Alerts and callouts: [references/alerts-and-callouts.md](/.agents/skills/markdown-documentation/references/alerts-and-callouts.md)

Load only the references relevant to the current edit.

## Mandatory Rules

- Optimize for raw-source readability as well as rendered output.
- Split wide tables or move detail into bullets when needed.
- When referencing a file in Markdown prose, use a Markdown link instead of
  plain quoted or backticked path text.
- Do not rely on memory when a relevant reference file exists.

## Final Check

Before finishing:

- Re-open edited constructs in raw Markdown.
- Verify they follow the relevant reference rules.
- For tables, verify pipe columns are visibly aligned.
- Verify links and Markdown syntax still work.
