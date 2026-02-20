import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { chromeMock, mockStorageLocal } from '../../src/test-utils/setup.js'
import { resetChromeMocks } from '../../src/test-utils/helpers.js'

describe('Storage Integration', () => {
  beforeEach(() => {
    resetChromeMocks()
    mockStorageLocal.data = {}
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('API Key Storage', () => {
    it('stores and retrieves API key', async () => {
      const apiKey = 'my-secret-api-key'

      await chrome.storage.local.set({ apiKey })

      expect(mockStorageLocal.data.apiKey).toBe(apiKey)

      const result = await chrome.storage.local.get('apiKey')
      expect(result.apiKey).toBe(apiKey)
    })

    it('returns undefined for missing API key', async () => {
      const result = await chrome.storage.local.get('apiKey')
      expect(result.apiKey).toBeUndefined()
    })

    it('overwrites existing API key', async () => {
      await chrome.storage.local.set({ apiKey: 'old-key' })
      await chrome.storage.local.set({ apiKey: 'new-key' })

      const result = await chrome.storage.local.get('apiKey')
      expect(result.apiKey).toBe('new-key')
    })

    it('removes API key', async () => {
      await chrome.storage.local.set({ apiKey: 'test-key' })
      await chrome.storage.local.remove('apiKey')

      const result = await chrome.storage.local.get('apiKey')
      expect(result.apiKey).toBeUndefined()
    })
  })

  describe('API URL Storage', () => {
    it('stores and retrieves custom API URL', async () => {
      const apiUrl = 'http://custom-server:3000'

      await chrome.storage.local.set({ apiUrl })

      const result = await chrome.storage.local.get('apiUrl')
      expect(result.apiUrl).toBe(apiUrl)
    })

    it('allows empty API URL (fallback to default)', async () => {
      await chrome.storage.local.set({ apiUrl: '' })

      const result = await chrome.storage.local.get('apiUrl')
      expect(result.apiUrl).toBe('')
    })
  })

  describe('Multiple Keys', () => {
    it('retrieves multiple keys at once', async () => {
      await chrome.storage.local.set({
        apiKey: 'test-key',
        apiUrl: 'http://test-api',
        bookmarkCaptureEnabled: true,
      })

      const result = await chrome.storage.local.get(['apiKey', 'apiUrl', 'bookmarkCaptureEnabled'])

      expect(result).toEqual({
        apiKey: 'test-key',
        apiUrl: 'http://test-api',
        bookmarkCaptureEnabled: true,
      })
    })

    it('retrieves all stored data when no keys specified', async () => {
      await chrome.storage.local.set({
        apiKey: 'key',
        otherSetting: 'value',
      })

      const result = await chrome.storage.local.get()

      expect(result).toEqual({
        apiKey: 'key',
        otherSetting: 'value',
      })
    })

    it('provides default values for missing keys', async () => {
      await chrome.storage.local.set({ apiKey: 'test-key' })

      const result = await chrome.storage.local.get({
        apiKey: '',
        apiUrl: 'http://default',
        newSetting: 'default-value',
      })

      expect(result).toEqual({
        apiKey: 'test-key',
        apiUrl: 'http://default',
        newSetting: 'default-value',
      })
    })
  })

  describe('Clear Storage', () => {
    it('clears all stored data', async () => {
      await chrome.storage.local.set({
        apiKey: 'key',
        apiUrl: 'url',
        otherData: 'data',
      })

      await chrome.storage.local.clear()

      const result = await chrome.storage.local.get()
      expect(result).toEqual({})
    })
  })

  describe('Settings Persistence', () => {
    it('persists bookmarkCaptureEnabled setting', async () => {
      await chrome.storage.local.set({ bookmarkCaptureEnabled: false })

      const result = await chrome.storage.local.get('bookmarkCaptureEnabled')
      expect(result.bookmarkCaptureEnabled).toBe(false)

      await chrome.storage.local.set({ bookmarkCaptureEnabled: true })

      const result2 = await chrome.storage.local.get('bookmarkCaptureEnabled')
      expect(result2.bookmarkCaptureEnabled).toBe(true)
    })

    it('persists autoConfirmWebpageCapture setting', async () => {
      await chrome.storage.local.set({ autoConfirmWebpageCapture: true })

      const result = await chrome.storage.local.get('autoConfirmWebpageCapture')
      expect(result.autoConfirmWebpageCapture).toBe(true)
    })

    it('persists captureBlacklist setting', async () => {
      const blacklist = ['twitter.com', 'facebook.com']
      await chrome.storage.local.set({ captureBlacklist: blacklist })

      const result = await chrome.storage.local.get('captureBlacklist')
      expect(result.captureBlacklist).toEqual(blacklist)
    })
  })

  describe('Storage Mock Behavior', () => {
    it('get is called with correct arguments', async () => {
      await chrome.storage.local.get('apiKey')

      expect(chromeMock.storage.local.get).toHaveBeenCalledWith('apiKey')
    })

    it('set is called with correct arguments', async () => {
      await chrome.storage.local.set({ apiKey: 'test' })

      expect(chromeMock.storage.local.set).toHaveBeenCalledWith({ apiKey: 'test' })
    })

    it('remove is called with correct arguments', async () => {
      await chrome.storage.local.remove('apiKey')

      expect(chromeMock.storage.local.remove).toHaveBeenCalledWith('apiKey')
    })

    it('clear is called', async () => {
      await chrome.storage.local.clear()

      expect(chromeMock.storage.local.clear).toHaveBeenCalled()
    })
  })
})
