import { test as base, chromium, type BrowserContext, type Page } from '@playwright/test'
import path from 'path'
import { fileURLToPath } from 'url'
import fs from 'fs'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

const pathToExtension = path.join(__dirname, '..', '..', '..', 'dist')
const pathToContentScript = path.join(pathToExtension, 'content.js')
const pathToContentCss = path.join(pathToExtension, 'content.css')

export const test = base.extend<{
  context: BrowserContext
}>({
  context: async ({}, use) => {
    const context = await chromium.launchPersistentContext('', {
      channel: 'chromium',
      headless: false,
      args: [
        `--disable-extensions-except=${pathToExtension}`,
        `--load-extension=${pathToExtension}`,
      ],
    })
    
    await use(context)
    await context.close()
  },
})

export async function injectContentScript(page: Page): Promise<void> {
  const contentScript = fs.readFileSync(pathToContentScript, 'utf-8')
  const contentCss = fs.readFileSync(pathToContentCss, 'utf-8')
  
  await page.addStyleTag({ content: contentCss })
  await page.evaluate(contentScript)
}

export const expect = test.expect
