import { vi } from 'vitest'

export function createMockTweetElement(options: {
  tweetId?: string
  username?: string
  displayName?: string
  text?: string
  timestamp?: string
  url?: string
}): HTMLElement {
  const {
    tweetId = '1234567890',
    username = 'testuser',
    displayName = 'Test User',
    text = 'This is a test tweet',
    timestamp = '2026-01-31T12:00:00.000Z',
    url = `/status/${tweetId}`,
  } = options

  const article = document.createElement('article')
  article.setAttribute('data-testid', 'tweet')
  article.setAttribute('data-tweet-id', tweetId)

  article.innerHTML = `
    <div>
      <a href="/${username}">
        <span>
          <span>${displayName}</span>
        </span>
      </a>
      <div data-testid="tweetText">
        <span>${text}</span>
      </div>
      <a href="${url}">
        <time datetime="${timestamp}">Jan 31, 2026</time>
      </a>
      <div role="group">
        <button data-testid="reply">Reply</button>
        <button data-testid="retweet">Retweet</button>
        <button data-testid="like">Like</button>
      </div>
    </div>
  `

  return article
}

export function createMockWebpageElement(options: {
  title?: string
  content?: string
  author?: string
  publishDate?: string
  description?: string
}): string {
  const {
    title = 'Test Article',
    content = 'This is the main content of the article. It should be long enough to pass the 200 character threshold for content extraction. We need to add more text here to ensure the content is properly extracted by the extraction logic.',
    author = 'John Doe',
    publishDate = '2026-01-31T00:00:00.000Z',
    description = 'A test article description',
  } = options

  return `
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>${title}</title>
  <meta name="author" content="${author}">
  <meta name="description" content="${description}">
  <meta property="article:published_time" content="${publishDate}">
  <meta property="og:site_name" content="Test Site">
</head>
<body>
  <main>
    <article>
      <h1>${title}</h1>
      <p>${content}</p>
    </article>
  </main>
</body>
</html>
`
}

export function mockFetchResponse(options: {
  ok?: boolean
  status?: number
  json?: unknown
  text?: string
}): void {
  const { ok = true, status = 200, json = {}, text } = options

  global.fetch = vi.fn().mockResolvedValue({
    ok,
    status,
    json: vi.fn().mockResolvedValue(json),
    text: vi.fn().mockResolvedValue(text || JSON.stringify(json)),
  })
}

export function resetChromeMocks(): void {
  vi.clearAllMocks()
}
