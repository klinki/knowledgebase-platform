import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { handleSaveTweet, handleSaveWebpage, extractWebpageData } from '../../src/background.js'
import { chromeMock, mockStorageLocal } from '../../src/test-utils/setup.js'
import { mockFetchResponse, resetChromeMocks } from '../../src/test-utils/helpers.js'

describe('handleSaveTweet', () => {
  const mockTweetData = {
    source: 'twitter',
    tweet_id: '1234567890',
    author: {
      username: '@testuser',
      display_name: 'Test User',
    },
    content: {
      text: 'Hello world!',
      timestamp: '2026-01-31T12:00:00.000Z',
      url: 'https://x.com/testuser/status/1234567890',
    },
    captured_at: '2026-01-31T12:30:00.000Z',
  }

  beforeEach(() => {
    resetChromeMocks()
    mockStorageLocal.data = {}
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('uses stored API key and URL from storage', async () => {
    mockStorageLocal.data = {
      apiKey: 'test-api-key',
      apiUrl: 'http://test-api-server:3000',
    }

    mockFetchResponse({ ok: true, json: { id: '123' } })

    const result = await handleSaveTweet(mockTweetData)

    expect(result).toEqual({ success: true })
    expect(global.fetch).toHaveBeenCalledWith(
      'http://test-api-server:3000/api/v1/capture',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({
          Authorization: 'Bearer test-api-key',
        }),
      })
    )
  })

  it('falls back to DEFAULT_API_URL when apiUrl not configured', async () => {
    mockStorageLocal.data = {
      apiKey: 'test-api-key',
    }

    mockFetchResponse({ ok: true, json: { id: '123' } })

    const result = await handleSaveTweet(mockTweetData)

    expect(result).toEqual({ success: true })
    expect(global.fetch).toHaveBeenCalledWith(
      'http://localhost:3000/api/v1/capture',
      expect.any(Object)
    )
  })

  it('returns error when API key not configured', async () => {
    mockStorageLocal.data = {}

    const result = await handleSaveTweet(mockTweetData)

    expect(result).toEqual({
      success: false,
      error: 'API key not configured. Please set it in the extension options.',
    })
    expect(global.fetch).not.toHaveBeenCalled()
  })

  it('handles network errors gracefully', async () => {
    mockStorageLocal.data = {
      apiKey: 'test-api-key',
    }

    global.fetch = vi.fn().mockRejectedValue(new Error('Network error'))

    const result = await handleSaveTweet(mockTweetData)

    expect(result).toEqual({
      success: false,
      error: 'Network error',
    })
  })

  it('handles API error responses', async () => {
    mockStorageLocal.data = {
      apiKey: 'test-api-key',
    }

    mockFetchResponse({
      ok: false,
      status: 401,
      text: 'Unauthorized',
    })

    const result = await handleSaveTweet(mockTweetData)

    expect(result.success).toBe(false)
    expect(result.error).toContain('API error: 401')
  })

  it('sends correct payload structure', async () => {
    mockStorageLocal.data = {
      apiKey: 'test-api-key',
    }

    mockFetchResponse({ ok: true, json: { id: '123' } })

    await handleSaveTweet(mockTweetData)

    const fetchCall = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[0]
    const body = JSON.parse(fetchCall[1].body)

    expect(body).toEqual(mockTweetData)
  })
})

describe('handleSaveWebpage', () => {
  const mockWebpageData = {
    source: 'webpage' as const,
    url: 'https://example.com/article',
    title: 'Test Article',
    author: 'John Doe',
    publish_date: '2026-01-31T00:00:00.000Z',
    description: 'A test article',
    content: {
      text: 'Article content here...',
      html: '<p>Article content here...</p>',
      excerpt: 'Article content...',
    },
    metadata: {
      site_name: 'Example Site',
      favicon: 'https://example.com/favicon.ico',
      language: 'en',
    },
    captured_at: '2026-01-31T12:30:00.000Z',
  }

  beforeEach(() => {
    resetChromeMocks()
    mockStorageLocal.data = {}
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('sends webpage data to correct endpoint', async () => {
    mockStorageLocal.data = {
      apiKey: 'test-api-key',
    }

    mockFetchResponse({ ok: true, json: { id: '123' } })

    const result = await handleSaveWebpage(mockWebpageData)

    expect(result).toEqual({ success: true })
    expect(global.fetch).toHaveBeenCalledWith(
      'http://localhost:3000/api/v1/capture/webpage',
      expect.objectContaining({
        method: 'POST',
      })
    )
  })

  it('returns error when API key not configured', async () => {
    mockStorageLocal.data = {}

    const result = await handleSaveWebpage(mockWebpageData)

    expect(result.success).toBe(false)
    expect(result.error).toContain('API key not configured')
  })

  it('handles API errors', async () => {
    mockStorageLocal.data = {
      apiKey: 'test-api-key',
    }

    mockFetchResponse({
      ok: false,
      status: 500,
      text: 'Internal Server Error',
    })

    const result = await handleSaveWebpage(mockWebpageData)

    expect(result.success).toBe(false)
    expect(result.error).toContain('API error: 500')
  })
})

describe('extractWebpageData', () => {
  beforeEach(() => {
    document.head.innerHTML = ''
    document.body.innerHTML = ''
  })

  it('extracts metadata from meta tags', () => {
    document.head.innerHTML = `
      <meta name="author" content="Jane Doe">
      <meta name="description" content="Test description">
      <meta property="article:published_time" content="2026-01-31T00:00:00.000Z">
      <meta property="og:site_name" content="Test Site">
    `

    const result = extractWebpageData('https://example.com/article', 'Test Article')

    expect(result).not.toBeNull()
    expect(result?.author).toBe('Jane Doe')
    expect(result?.description).toBe('Test description')
    expect(result?.publish_date).toBe('2026-01-31T00:00:00.000Z')
    expect(result?.metadata.site_name).toBe('Test Site')
  })

  it('extracts main content from article element', () => {
    document.body.innerHTML = `
      <article>
        <h1>Article Title</h1>
        <p>This is a long article content that should be extracted properly by the content extraction function. We need to make sure it is long enough to pass the 200 character threshold.</p>
        <p>Another paragraph with more content to ensure we have enough text for the extraction to work properly.</p>
      </article>
    `

    const result = extractWebpageData('https://example.com/article', 'Test Article')

    expect(result).not.toBeNull()
    expect(result?.content.text).toContain('long article content')
    expect(result?.content.excerpt.length).toBeLessThanOrEqual(203)
  })

  it('extracts content from paragraphs when no article element', () => {
    document.body.innerHTML = `
      <p>This is a paragraph that is long enough to be included in the extraction. It needs to be over 50 characters.</p>
      <p>Another paragraph with sufficient length to be included in the text extraction process for testing.</p>
    `

    const result = extractWebpageData('https://example.com/article', 'Test Article')

    expect(result).not.toBeNull()
    expect(result?.content.text).toContain('paragraph')
  })

  it('uses URL hostname as site name fallback', () => {
    const result = extractWebpageData('https://mysite.com/page', 'Test Page')

    expect(result).not.toBeNull()
    expect(result?.metadata.site_name).toBe('mysite.com')
  })

  it('includes favicon URL', () => {
    const result = extractWebpageData('https://example.com/page', 'Test Page')

    expect(result).not.toBeNull()
    expect(result?.metadata.favicon).toBe('https://example.com/favicon.ico')
  })

  it('captures current timestamp', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-01-31T15:00:00.000Z'))

    const result = extractWebpageData('https://example.com/page', 'Test Page')

    expect(result?.captured_at).toBe('2026-01-31T15:00:00.000Z')

    vi.useRealTimers()
  })
})
