# Extended Syntax (GitHub Flavored Markdown)

Use extended syntax only when the target renderer supports it and the feature
improves the document.

## Mandatory Rules

- MUST verify that the target renderer supports the syntax before using it.
- MUST prefer standard Markdown when extended syntax adds little value.
- MUST avoid GitHub-specific mention or issue syntax in docs unless it is
  intentionally needed.

## Footnotes

Use footnotes sparingly. Inline prose is usually easier to read.

## Task Lists

Task lists are allowed when they represent real tracking state.

## GitHub-Specific References

Use mentions and issue references only when the document intentionally
references GitHub users, teams, issues, or pull requests.

## Avoid by Default

Do not assume support for less-common extended constructs such as definition
lists unless you have verified renderer support for the current environment.

## Do

- Use extended syntax when it adds real utility.
- Keep GitHub-specific references intentional and minimal.
- Prefer simpler Markdown when it communicates just as well.

## Do Not

- Introduce renderer-dependent syntax casually.
- Create accidental mentions or issue links in general docs.
- Use extended syntax as decoration.
