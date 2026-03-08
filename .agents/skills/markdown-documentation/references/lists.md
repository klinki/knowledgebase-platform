# Lists

Use lists for inherently list-shaped content and keep marker style consistent.

## Mandatory Rules

- MUST use `-` for unordered lists unless an existing document clearly uses a
  different style.
- MUST use `1.` style markers for ordered lists.
- MUST keep indentation consistent within a list.
- MUST avoid deep nesting unless it improves clarity.

## Unordered Lists

```markdown
- Item 1
- Item 2
  - Nested item 2.1
  - Nested item 2.2
- Item 3
```

## Ordered Lists

```markdown
1. First item
2. Second item
   1. Nested item 2.1
   2. Nested item 2.2
3. Third item
```

## Task Lists

```markdown
- [x] Completed task
- [ ] Incomplete task
- [ ] Another task
```

Use task lists only when tracking actionable items.

## Do

- Keep list items parallel in style and grammar.
- Prefer flat lists when nested lists add noise.
- Split long prose into a short intro plus a list when it improves scanning.

## Do Not

- Mix unordered list markers within the same list.
- Use lists for content that reads better as a paragraph.
- Create deeply nested lists unless the structure is necessary.
