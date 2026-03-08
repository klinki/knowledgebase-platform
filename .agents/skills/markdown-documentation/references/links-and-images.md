# Links and Images

Use links, images, and code blocks in a way that preserves readability in both
raw Markdown and rendered output.

## Mandatory Rules

- MUST use descriptive link text.
- MUST prefer relative links for repo-local documentation.
- MUST include alt text for images.
- MUST use fenced code blocks for multi-line code.
- MUST include a language info string on fenced code blocks when practical.
- MUST avoid indented code blocks unless there is a specific reason to use one.

## Links

```markdown
[Project docs](docs/ARCHITECTURE.md)
[OpenAI](https://openai.com)
[Link with title](https://example.com "Link title")
```

Use bare autolinks only when the raw URL itself is the content.

```markdown
<https://example.com>
```

Reference-style links are allowed, but inline links are usually easier to
maintain for normal repo docs.

```markdown
[Link text][reference]
[reference]: https://example.com
```

## Images

```markdown
![Architecture diagram](images/architecture.png)
![Architecture diagram](images/architecture.png "Diagram title")
```

## Code Blocks

````markdown
Inline code: `const x = 5;`

```typescript
function hello(name: string): void {
  console.log(`Hello, ${name}!`);
}
```

```python
def hello(name: str) -> None:
    print(f"Hello, {name}!")
```

```bash
npm install
npm start
```
````

## Do

- Keep link text meaningful out of context.
- Prefer relative paths for files in the same repository.
- Use inline code for filenames, commands, and identifiers in prose.
- Use fenced code blocks with language tags for examples.

## Do Not

- Use "click here" or similarly vague link text.
- Paste raw URLs when descriptive link text is clearer.
- Omit alt text from images.
- Use indented code blocks as the default style.
