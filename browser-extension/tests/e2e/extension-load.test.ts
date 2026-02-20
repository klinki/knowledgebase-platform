import path from 'path'
import { fileURLToPath } from 'url'
import fs from 'fs'
import { test, expect, injectContentScript } from './fixtures/extension.js'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)
const browserExtensionRoot = path.join(__dirname, '..', '..')

test.describe('Extension Build Verification', () => {
  test('dist folder contains required files', () => {
    const distPath = path.join(browserExtensionRoot, 'dist')
    
    expect(fs.existsSync(path.join(distPath, 'background.js'))).toBe(true)
    expect(fs.existsSync(path.join(distPath, 'content.js'))).toBe(true)
    expect(fs.existsSync(path.join(distPath, 'content.css'))).toBe(true)
    expect(fs.existsSync(path.join(distPath, 'options.js'))).toBe(true)
    expect(fs.existsSync(path.join(distPath, 'constants.js'))).toBe(true)
  })

  test('manifest.json is valid', () => {
    const manifestPath = path.join(browserExtensionRoot, 'manifest.json')
    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf-8'))
    
    expect(manifest.manifest_version).toBe(3)
    expect(manifest.name).toBe('Sentinel Knowledge Collector')
    expect(manifest.permissions).toContain('storage')
    expect(manifest.content_scripts).toBeDefined()
    expect(manifest.background).toBeDefined()
  })
})

test.describe('Options Page', () => {
  test('options.html contains form elements', () => {
    const optionsPath = path.join(browserExtensionRoot, 'options.html')
    const optionsHtml = fs.readFileSync(optionsPath, 'utf-8')
    
    expect(optionsHtml).toContain('id="apiKey"')
    expect(optionsHtml).toContain('id="apiUrl"')
    expect(optionsHtml).toContain('id="saveBtn"')
  })
})

test.describe('Background Script', () => {
  test('background.js contains required message handlers', () => {
    const backgroundJsPath = path.join(browserExtensionRoot, 'dist', 'background.js')
    const backgroundJs = fs.readFileSync(backgroundJsPath, 'utf-8')
    
    expect(backgroundJs).toContain('SAVE_TWEET')
    expect(backgroundJs).toContain('SAVE_WEBPAGE')
  })
})

test.describe('Content Script', () => {
  test('content.js contains tweet extraction logic', () => {
    const contentJsPath = path.join(browserExtensionRoot, 'dist', 'content.js')
    const contentJs = fs.readFileSync(contentJsPath, 'utf-8')
    
    expect(contentJs).toContain('extractTweetId')
    expect(contentJs).toContain('extractAuthor')
    expect(contentJs).toContain('sentinel-save-btn')
  })
})

test.describe('Mock Fixtures', () => {
  test('tweet-mock.html exists and has tweet elements', () => {
    const fixturePath = path.join(browserExtensionRoot, 'tests', 'e2e', 'fixtures', 'tweet-mock.html')
    const fixtureHtml = fs.readFileSync(fixturePath, 'utf-8')
    
    expect(fixtureHtml).toContain('data-testid="tweet"')
    expect(fixtureHtml).toContain('data-tweet-id')
  })

  test('webpage-mock.html exists and has article element', () => {
    const fixturePath = path.join(browserExtensionRoot, 'tests', 'e2e', 'fixtures', 'webpage-mock.html')
    const fixtureHtml = fs.readFileSync(fixturePath, 'utf-8')
    
    expect(fixtureHtml).toContain('<article>')
    expect(fixtureHtml).toContain('name="author"')
  })
})
