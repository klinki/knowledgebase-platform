import { describe, it, expect } from 'vitest'
import {
  DEFAULT_API_URL,
  MAX_PROCESSED_TWEETS,
  EXTENSION_NAME,
  EXTENSION_VERSION,
} from '../../src/constants.js'

describe('constants', () => {
  describe('DEFAULT_API_URL', () => {
    it('is defined and is a valid URL', () => {
      expect(DEFAULT_API_URL).toBeDefined()
      expect(DEFAULT_API_URL).toBe('http://localhost:3000')
    })

    it('is a valid URL format', () => {
      expect(() => new URL(DEFAULT_API_URL)).not.toThrow()
    })
  })

  describe('MAX_PROCESSED_TWEETS', () => {
    it('is defined and is a positive number', () => {
      expect(MAX_PROCESSED_TWEETS).toBeDefined()
      expect(MAX_PROCESSED_TWEETS).toBe(1000)
      expect(MAX_PROCESSED_TWEETS).toBeGreaterThan(0)
    })

    it('is a reasonable size to prevent memory leaks', () => {
      expect(MAX_PROCESSED_TWEETS).toBeLessThanOrEqual(10000)
    })
  })

  describe('EXTENSION_NAME', () => {
    it('is defined and is a non-empty string', () => {
      expect(EXTENSION_NAME).toBeDefined()
      expect(EXTENSION_NAME).toBe('Sentinel Knowledge Collector')
      expect(EXTENSION_NAME.length).toBeGreaterThan(0)
    })
  })

  describe('EXTENSION_VERSION', () => {
    it('is defined and follows semver format', () => {
      expect(EXTENSION_VERSION).toBeDefined()
      expect(EXTENSION_VERSION).toBe('1.0.0')
      expect(EXTENSION_VERSION).toMatch(/^\d+\.\d+\.\d+$/)
    })
  })
})
