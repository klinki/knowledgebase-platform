import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import {
  extractTweetId,
  extractAuthor,
  extractText,
  extractTimestamp,
  extractUrl,
  extractTweetData,
} from '../../src/content.js'
import { createMockTweetElement } from '../../src/test-utils/helpers.js'

describe('extractTweetId', () => {
  it('extracts tweet ID from data-tweet-id attribute', () => {
    const element = createMockTweetElement({ tweetId: '1234567890' })
    const result = extractTweetId(element)
    expect(result).toBe('1234567890')
  })

  it('extracts tweet ID from time link /status/ path', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <a href="/testuser/status/9876543210">
        <time datetime="2026-01-31T12:00:00.000Z">Jan 31</time>
      </a>
    `
    const result = extractTweetId(element)
    expect(result).toBe('9876543210')
  })

  it('extracts tweet ID from any status link in the tweet', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <div>
        <a href="/someotheruser/status/1112223334">Link</a>
      </div>
    `
    const result = extractTweetId(element)
    expect(result).toBe('1112223334')
  })

  it('returns null when no tweet ID can be found', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = '<div>No tweet ID here</div>'
    const result = extractTweetId(element)
    expect(result).toBeNull()
  })

  it('prioritizes data-tweet-id attribute over links', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.setAttribute('data-tweet-id', '1111111111')
    element.innerHTML = `
      <a href="/user/status/2222222222">
        <time datetime="2026-01-31T12:00:00.000Z">Jan 31</time>
      </a>
    `
    const result = extractTweetId(element)
    expect(result).toBe('1111111111')
  })
})

describe('extractAuthor', () => {
  it('extracts author username and display name from user link', () => {
    const element = createMockTweetElement({
      username: 'testuser',
      displayName: 'Test User',
    })
    const result = extractAuthor(element)
    expect(result).toEqual({
      username: '@testuser',
      display_name: 'Test User',
    })
  })

  it('falls back to username as display name when not found', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <a href="/onlyusername">
        <span>Some other content</span>
      </a>
    `
    const result = extractAuthor(element)
    expect(result).toEqual({
      username: '@onlyusername',
      display_name: 'onlyusername',
    })
  })

  it('returns null when no valid user link found', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <a href="/status/123456">Status link</a>
      <a href="https://external.com">External link</a>
    `
    const result = extractAuthor(element)
    expect(result).toBeNull()
  })

  it('ignores links that are not user profile links', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <a href="/status/123456">Status</a>
      <a href="/explore">Explore</a>
      <a href="/i/notifications">Notifications</a>
    `
    const result = extractAuthor(element)
    expect(result?.username).toBe('@explore')
  })

  it('handles usernames with underscores', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <a href="/test_user_123">
        <span><span>Test User</span></span>
      </a>
    `
    const result = extractAuthor(element)
    expect(result).toEqual({
      username: '@test_user_123',
      display_name: 'Test User',
    })
  })
})

describe('extractText', () => {
  it('extracts text from tweetText data-testid element', () => {
    const element = createMockTweetElement({
      text: 'Hello, world! This is a test tweet.',
    })
    const result = extractText(element)
    expect(result.trim()).toBe('Hello, world! This is a test tweet.')
  })

  it('falls back to [lang] element when tweetText not found', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <div lang="en">Fallback text content</div>
    `
    const result = extractText(element)
    expect(result).toBe('Fallback text content')
  })

  it('returns empty string when no text found', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = '<div>No text elements</div>'
    const result = extractText(element)
    expect(result).toBe('')
  })

  it('returns first tweetText element content when multiple exist', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <div data-testid="tweetText">First tweet text</div>
      <div data-testid="tweetText">Second tweet text (reply)</div>
    `
    const result = extractText(element)
    expect(result).toBe('First tweet text')
  })
})

describe('extractTimestamp', () => {
  it('extracts datetime from time element', () => {
    const element = createMockTweetElement({
      timestamp: '2026-01-31T12:00:00.000Z',
    })
    const result = extractTimestamp(element)
    expect(result).toBe('2026-01-31T12:00:00.000Z')
  })

  it('returns null when no time element found', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = '<div>No time element</div>'
    const result = extractTimestamp(element)
    expect(result).toBeNull()
  })

  it('returns null when time element has no datetime attribute', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = '<time>Jan 31, 2026</time>'
    const result = extractTimestamp(element)
    expect(result).toBeNull()
  })
})

describe('extractUrl', () => {
  it('extracts full URL from existing status link', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <a href="https://x.com/testuser/status/1234567890">
        <time datetime="2026-01-31T12:00:00.000Z">Jan 31</time>
      </a>
    `
    const result = extractUrl(element, '1234567890', '@testuser')
    expect(result).toBe('https://x.com/testuser/status/1234567890')
  })

  it('converts relative URL to full URL', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = `
      <a href="/testuser/status/1234567890">
        <time datetime="2026-01-31T12:00:00.000Z">Jan 31</time>
      </a>
    `
    const result = extractUrl(element, '1234567890', '@testuser')
    expect(result).toBe('https://x.com/testuser/status/1234567890')
  })

  it('constructs URL from username and tweet ID when no link found', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = '<div>No status link</div>'
    const result = extractUrl(element, '1234567890', '@testuser')
    expect(result).toBe('https://x.com/testuser/status/1234567890')
  })

  it('strips @ from username when constructing URL', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = '<div>No status link</div>'
    const result = extractUrl(element, '1234567890', '@another_user')
    expect(result).toBe('https://x.com/another_user/status/1234567890')
  })
})

describe('extractTweetData', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-31T12:30:00.000Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('extracts complete tweet data from valid element', () => {
    const element = createMockTweetElement({
      tweetId: '1234567890',
      username: 'testuser',
      displayName: 'Test User',
      text: 'Hello world!',
      timestamp: '2026-01-31T12:00:00.000Z',
      url: '/testuser/status/1234567890',
    })

    const result = extractTweetData(element)

    expect(result).toEqual({
      source: 'twitter',
      tweet_id: '1234567890',
      author: {
        username: '@testuser',
        display_name: 'Test User',
      },
      content: {
        text: expect.stringContaining('Hello world!'),
        timestamp: '2026-01-31T12:00:00.000Z',
        url: 'https://x.com/testuser/status/1234567890',
      },
      captured_at: '2026-01-31T12:30:00.000Z',
    })
  })

  it('returns null when tweet ID cannot be extracted', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.innerHTML = '<div>No tweet ID</div>'

    const result = extractTweetData(element)
    expect(result).toBeNull()
  })

  it('returns null when author cannot be extracted', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.setAttribute('data-tweet-id', '1234567890')
    element.innerHTML = '<div>No author link</div>'

    const result = extractTweetData(element)
    expect(result).toBeNull()
  })

  it('handles missing text gracefully', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.setAttribute('data-tweet-id', '1234567890')
    element.innerHTML = `
      <a href="/testuser">
        <span><span>Test User</span></span>
      </a>
    `

    const result = extractTweetData(element)

    expect(result).not.toBeNull()
    expect(result?.content.text).toBe('')
  })

  it('handles missing timestamp gracefully', () => {
    const element = document.createElement('article')
    element.setAttribute('data-testid', 'tweet')
    element.setAttribute('data-tweet-id', '1234567890')
    element.innerHTML = `
      <a href="/testuser">
        <span><span>Test User</span></span>
      </a>
    `

    const result = extractTweetData(element)

    expect(result).not.toBeNull()
    expect(result?.content.timestamp).toBeNull()
  })
})
