# Tables

Markdown tables are a great way to display structured data. GitHub Flavored Markdown (GFM) provides a simple syntax for creating tables.

## Basic Table Syntax

A table consists of a header row, a separator row (with dashes), and data rows. Use pipes (`|`) to separate columns.

```markdown
| Header 1 | Header 2 | Header 3 |
|----------|----------|----------|
| Row 1    | Data     | Data     |
| Row 2    | Data     | Data     |
```

## Column Alignment

You can align text in columns by adding colons (`:`) to the separator row.

| Syntax | Alignment |
|:--- |:--- |
| `:---` | Left-aligned (default) |
| `---:` | Right-aligned |
| `:---:` | Center-aligned |

### Alignment Example

```markdown
| Left | Center | Right |
|:-----|:------:|------:|
| data | data   | data  |
```

## Formatting Inside Tables

You can use common Markdown formatting such as links, inline code, and emphasis within table cells.

```markdown
| Feature | Description |
|:--- |:--- |
| **Bold** | `**text**` |
| *Italic* | `*text*` |
| [Link](https://example.com) | `[text](url)` |
| `Code` | `` `code` `` |
```

## Well formatted tables

Prefer well formatted tables with nice whitespace formatting

### DON'T
- Don't make ugly tables

```markdown
| Syntax | Alignment |
|:--- |:--- |
| `:---` | Left-aligned (default) |
| `---:` | Right-aligned |
| `:---:` | Center-aligned |
```

### DO
- Make tables well formatted
- Well formatted means pipe `|` characters are aligned

```markdown
| Syntax  | Alignment              |
| :---    | :---                   |
| `:---`  | Left-aligned (default) |
| `---:`  | Right-aligned          |
| `:---:` | Center-aligned         |

```

## Best Practices

### ✅ DO
- Use whitespace around pipes for better source code readability.
- Keep table headers concise.
- Use alignment to improve readability (e.g., right-align numbers).
- Use pipes on both ends of a row for consistency.
- Use whitespace to improve table readability

### ❌ DON'T
- Create excessively wide tables that require horizontal scrolling.
- Use complex formatting that makes the source Markdown hard to maintain.
- Use more than one header row (not supported in standard GFM).
