# Alerts and Callouts

Use alerts and callouts sparingly for information that needs emphasis beyond
normal prose.

## Mandatory Rules

- MUST prefer GitHub-style alerts when the target renderer supports them.
- MUST keep alerts short and focused.
- MUST use plain prose when the content does not need elevated emphasis.

## GitHub-Style Alerts

```markdown
> [!NOTE]
> Useful information

> [!WARNING]
> Critical content
```

## Plain Blockquote Callouts

```markdown
> **Note**
> This is a note.
```

Use plain blockquote callouts only when GFM alerts are unavailable or the
document already uses that style.

## Do

- Use alerts for warnings, caveats, prerequisites, or key constraints.
- Keep the title and body concise.
- Match the alert severity to the content.

## Do Not

- Wrap large sections of normal documentation in alerts.
- Stack many alerts back to back.
- Use alerts as a replacement for clear structure.
