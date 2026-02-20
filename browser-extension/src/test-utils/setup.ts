import { vi } from 'vitest'

type MockStorage = {
  data: Record<string, unknown>
}

function createMockStorage(): MockStorage & {
  get: ReturnType<typeof vi.fn>
  set: ReturnType<typeof vi.fn>
  remove: ReturnType<typeof vi.fn>
  clear: ReturnType<typeof vi.fn>
} {
  const storage: MockStorage & {
    get: ReturnType<typeof vi.fn>
    set: ReturnType<typeof vi.fn>
    remove: ReturnType<typeof vi.fn>
    clear: ReturnType<typeof vi.fn>
  } = {
    data: {},
    get: vi.fn((keys?: string | string[] | Record<string, unknown>) => {
      if (keys === undefined || keys === null) {
        return Promise.resolve({ ...storage.data })
      }
      if (typeof keys === 'string') {
        return Promise.resolve({ [keys]: storage.data[keys] })
      }
      if (Array.isArray(keys)) {
        const result: Record<string, unknown> = {}
        keys.forEach((key) => {
          if (storage.data[key] !== undefined) {
            result[key] = storage.data[key]
          }
        })
        return Promise.resolve(result)
      }
      const result: Record<string, unknown> = {}
      Object.keys(keys).forEach((key) => {
        if (storage.data[key] !== undefined) {
          result[key] = storage.data[key]
        } else {
          result[key] = keys[key]
        }
      })
      return Promise.resolve(result)
    }),
    set: vi.fn((items: Record<string, unknown>) => {
      Object.assign(storage.data, items)
      return Promise.resolve()
    }),
    remove: vi.fn((keys: string | string[]) => {
      const keysArray = Array.isArray(keys) ? keys : [keys]
      keysArray.forEach((key) => {
        delete storage.data[key]
      })
      return Promise.resolve()
    }),
    clear: vi.fn(() => {
      storage.data = {}
      return Promise.resolve()
    }),
  }
  return storage
}

type MockEvent<T extends (...args: unknown[]) => unknown = () => void> = {
  addListener: ReturnType<typeof vi.fn>
  removeListener: ReturnType<typeof vi.fn>
  hasListener: ReturnType<typeof vi.fn>
  hasListeners: ReturnType<typeof vi.fn>
  callListeners: (...args: Parameters<T>) => void
  clearListeners: () => void
  _listeners: Set<T>
}

function createMockEvent<T extends (...args: unknown[]) => unknown = () => void>(): MockEvent<T> {
  const listeners = new Set<T>()
  
  const event: MockEvent<T> = {
    _listeners: listeners,
    addListener: vi.fn((callback: T) => {
      listeners.add(callback)
    }),
    removeListener: vi.fn((callback: T) => {
      listeners.delete(callback)
    }),
    hasListener: vi.fn((callback: T) => listeners.has(callback)),
    hasListeners: vi.fn(() => listeners.size > 0),
    callListeners: (...args: Parameters<T>) => {
      listeners.forEach((listener) => listener(...args))
    },
    clearListeners: () => {
      listeners.clear()
    },
  }
  
  return event
}

const mockStorageLocal = createMockStorage()

const chromeMock = {
  storage: {
    local: mockStorageLocal,
  },
  runtime: {
    sendMessage: vi.fn(),
    onMessage: createMockEvent<(request: unknown, sender: unknown, sendResponse: (response: unknown) => void) => void | boolean>(),
    onInstalled: createMockEvent<(details: { reason: string }) => void>(),
    lastError: null as chrome.runtime.LastError | null,
  },
  scripting: {
    executeScript: vi.fn(),
  },
  tabs: {
    query: vi.fn(),
    create: vi.fn(),
  },
  notifications: {
    create: vi.fn(),
    onButtonClicked: createMockEvent<(notificationId: string, buttonIndex: number) => void>(),
  },
  bookmarks: {
    onCreated: createMockEvent<(id: string, bookmark: { url?: string; title?: string }) => void>(),
    create: vi.fn(),
  },
  contextMenus: {
    create: vi.fn(),
    removeAll: vi.fn(),
    onClicked: createMockEvent<(info: chrome.contextMenus.OnClickData, tab?: chrome.tabs.Tab) => void>(),
  },
}

vi.stubGlobal('chrome', chromeMock)

export { chromeMock, createMockStorage, createMockEvent, mockStorageLocal }
