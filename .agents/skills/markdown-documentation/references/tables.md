# Tables

Use GitHub Flavored Markdown table syntax and format tables for raw-source
readability, not just rendered correctness.

## Mandatory Rules

- MUST align pipe `|` characters vertically across all rows in a table.
- MUST pad cells with spaces so columns line up in raw Markdown.
- MUST use pipes on both ends of every row.
- MUST keep headers concise.
- MUST split wide tables or move detail into bullets when a table becomes hard
  to read or align cleanly.
- MUST re-read the final table in raw Markdown before finishing.

## Basic Table Syntax

A table consists of a header row, a separator row, and data rows.

```markdown
| Header 1 | Header 2 | Header 3 |
| :---     | :---     | :---     |
| Row 1    | Data     | Data     |
| Row 2    | Data     | Data     |
```

## Column Alignment

Use colons in the separator row to control alignment.

| Syntax  | Alignment              |
| :---    | :---                   |
| `:---`  | Left-aligned (default) |
| `---:`  | Right-aligned          |
| `:---:` | Center-aligned         |

### Alignment Example

```markdown
| Left | Center | Right |
| :--- | :----: | ----: |
| data |  data  |  data |
```

## Formatting Inside Tables

You can use common Markdown formatting inside table cells.

```markdown
| Feature | Description     |
| :---    | :---            |
| **Bold** | `**text**`     |
| *Italic* | `*text*`       |
| [Link](https://example.com) | `[text](url)` |
| `Code` | `` `code` ``     |
```

## Formatting Standard

### Avoid

```markdown
| Syntax | Alignment |
|:--- |:--- |
| `:---` | Left-aligned (default) |
| `---:` | Right-aligned |
| `:---:` | Center-aligned |
```

Problems:

- Pipes are not aligned.
- Spacing is inconsistent.
- Raw-source readability is poor.

### Use

```markdown
| Syntax  | Alignment              |
| :---    | :---                   |
| `:---`  | Left-aligned (default) |
| `---:`  | Right-aligned          |
| `:---:` | Center-aligned         |
```

## Best Practices

### Do

- Use whitespace around pipes for readability.
- Keep tables narrow when possible.
- Move dense detail into bullets below the table.
- Right-align numeric columns when it improves scanning.

### Do Not

- Leave pipe columns visually unaligned.
- Keep a table wide when splitting it would improve readability.
- Use more than one header row.
