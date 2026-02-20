import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { chromeMock, mockStorageLocal } from '../../src/test-utils/setup.js'
import { mockFetchResponse, resetChromeMocks } from '../../src/test-utils/helpers.js'
import { DEFAULT_API_URL } from '../../src/constants.js'

describe('Message Passing Integration', () => {
  let messageListeners: Array<(request: unknown, sender: unknown, sendResponse: (response: unknown) => void) => void | boolean>

  beforeEach(async () => {
    resetChromeMocks()
    mockStorageLocal.data = {}
    messageListeners = []
    vi.clearAllMocks()

    // Capture message listeners
    const originalAddListener = chromeMock.runtime.onMessage.addListener
    chromeMock.runtime.onMessage.addListener = vi.fn((callback) => {
      messageListeners.push(callback)
    })

    // Re-import background to register listeners
    vi.resetModules()
    await import('../../src/background.js')
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.resetModules()
  })

  describe('SAVE_TWEET message', () => {
    it('handles SAVE_TWEET message and returns success', async () => {
      mockStorageLocal.data = { apiKey: 'test-key' }
      mockFetchResponse({ ok: true, json: { id: '123' } })

      const tweetData = {
        source: 'twitter',
        tweet_id: '1234567890',
        author: { username: '@test', display_name: 'Test' },
        content: { text: 'Hello', timestamp: '2026-01-31T12:00:00Z', url: 'https://x.com/test/status/123' },
        captured_at: '2026-01-31T12:00:00Z',
      }

      const sendResponse = vi.fn()
      const request = { type: 'SAVE_TWEET', data: tweetData }
      const sender = {}

      const handler = messageListeners[0]
      const result = handler(request, sender, sendResponse)

      expect(result).toBe(true)

      await vi.waitFor(() => {
        expect(sendResponse).toHaveBeenCalled()
      })

      expect(sendResponse).toHaveBeenCalledWith({ success: true })
    })

    it('handles SAVE_TWEET message and returns error for missing API key', async () => {
      mockStorageLocal.data = {}

      const tweetData = {
        source: 'twitter',
        tweet_id: '1234567890',
        author: { username: '@test', display_name: 'Test' },
        content: { text: 'Hello', timestamp: null, url: 'https://x.com/test/status/123' },
        captured_at: '2026-01-31T12:00:00Z',
      }

      const sendResponse = vi.fn()
      const request = { type: 'SAVE_TWEET', data: tweetData }

      const handler = messageListeners[0]
      handler(request, {}, sendResponse)

      await vi.waitFor(() => {
        expect(sendResponse).toHaveBeenCalled()
      })

      expect(sendResponse).toHaveBeenCalledWith({
        success: false,
        error: expect.stringContaining('API key not configured'),
      })
    })

    it('makes API call with correct authorization header', async () => {
      mockStorageLocal.data = { apiKey: 'my-secret-key', apiUrl: 'http://custom-api' }
      mockFetchResponse({ ok: true, json: {} })

      const tweetData = {
        source: 'twitter',
        tweet_id: '123',
        author: { username: '@user', display_name: 'User' },
        content: { text: 'Test', timestamp: null, url: 'https://x.com/user/status/123' },
        captured_at: '2026-01-31T12:00:00Z',
      }

      const sendResponse = vi.fn()
      messageListeners[0]({ type: 'SAVE_TWEET', data: tweetData }, {}, sendResponse)

      await vi.waitFor(() => {
        expect(global.fetch).toHaveBeenCalled()
      })

      const fetchCall = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[0]
      expect(fetchCall[0]).toBe('http://custom-api/api/v1/capture')
      expect(fetchCall[1].headers.Authorization).toBe('Bearer my-secret-key')
    })
  })

  describe('SAVE_WEBPAGE message', () => {
    it('handles SAVE_WEBPAGE message and returns success', async () => {
      mockStorageLocal.data = { apiKey: 'test-key' }
      mockFetchResponse({ ok: true, json: { id: '123' } })

      const webpageData = {
        source: 'webpage' as const,
        url: 'https://example.com/article',
        title: 'Test Article',
        author: 'Author',
        publish_date: '2026-01-31T00:00:00Z',
        description: 'Description',
        content: { text: 'Content', html: '<p>Content</p>', excerpt: 'Content...' },
        metadata: { site_name: 'Example', favicon: 'https://example.com/favicon.ico', language: 'en' },
        captured_at: '2026-01-31T12:00:00Z',
      }

      const sendResponse = vi.fn()
      messageListeners[0]({ type: 'SAVE_WEBPAGE', data: webpageData }, {}, sendResponse)

      await vi.waitFor(() => {
        expect(sendResponse).toHaveBeenCalled()
      })

      expect(sendResponse).toHaveBeenCalledWith({ success: true })
      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:3000/api/v1/capture/webpage',
        expect.objectContaining({ method: 'POST' })
      )
    })

    it('returns error when API key missing', async () => {
      mockStorageLocal.data = {}

      const webpageData = {
        source: 'webpage' as const,
        url: 'https://example.com',
        title: 'Test',
        author: null,
        publish_date: null,
        description: null,
        content: { text: '', html: '', excerpt: '' },
        metadata: { site_name: 'Example', favicon: '', language: null },
        captured_at: '2026-01-31T12:00:00Z',
      }

      const sendResponse = vi.fn()
      messageListeners[0]({ type: 'SAVE_WEBPAGE', data: webpageData }, {}, sendResponse)

      await vi.waitFor(() => {
        expect(sendResponse).toHaveBeenCalled()
      })

      expect(sendResponse).toHaveBeenCalledWith({
        success: false,
        error: expect.stringContaining('API key not configured'),
      })
    })
  })

  describe('Unknown message type', () => {
    it('returns false for unknown message types', () => {
      const sendResponse = vi.fn()
      const result = messageListeners[0]({ type: 'UNKNOWN' }, {}, sendResponse)

      expect(result).toBe(false)
    })
  })
})
