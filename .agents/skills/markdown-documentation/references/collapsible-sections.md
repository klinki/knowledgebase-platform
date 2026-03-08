# Collapsible Sections, Syntax Highlighting, and Badges

Use these constructs only when they improve navigation or presentation without
making the source harder to maintain.

## Collapsible Sections

## Mandatory Rules

- MUST use valid `<details>` and `<summary>` markup.
- MUST keep the summary text short and descriptive.
- MUST avoid hiding core content by default.

````markdown
<details>
<summary>Click to expand</summary>

Hidden content goes here.

- Can include lists
- Can include code blocks

```javascript
const code = "works too";
```

</details>
````

Use collapsible sections for optional detail, not primary documentation.

## Syntax Highlighting

- MUST prefer fenced code blocks with a language tag.
- MUST choose the most specific practical language tag.

````markdown
```typescript
const value = 1;
```
````

## Badges

- MUST use badges only when they add useful status metadata.
- MUST avoid cluttering documentation with low-signal badges.

Example badge set:

```markdown
![Build Status](https://img.shields.io/github/workflow/status/user/repo/CI)
![Coverage](https://img.shields.io/codecov/c/github/user/repo)
```

## Do

- Use collapsible sections for optional detail or long examples.
- Use syntax highlighting on fenced code blocks.
- Keep badge sets small and useful.

## Do Not

- Hide essential instructions in `<details>`.
- Use unlabeled fenced code blocks when a language is obvious.
- Add decorative badges with little informational value.
