# Links and Images

Use links, images, and code blocks in a way that preserves readability in both
raw Markdown and rendered output.

## Mandatory Rules

- MUST use descriptive link text.
- MUST prefer relative links for repo-local documentation.
- MUST use a Markdown link when referencing a file in Markdown prose instead of
  plain quoted or backticked path text.
- MUST include alt text for images.
- MUST use fenced code blocks for multi-line code.
- MUST include a language info string on fenced code blocks when practical.
- MUST avoid indented code blocks unless there is a specific reason to use one.

## Links

Use inline links as the default style for normal repo docs.

Use bare autolinks only when the raw URL itself is the content.

Reference-style links are allowed, but inline links are usually easier to
maintain.

## Images

Use alt text that still makes sense if the image cannot be rendered.

## Code Blocks

Use fenced blocks with language tags for multi-line examples.

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
