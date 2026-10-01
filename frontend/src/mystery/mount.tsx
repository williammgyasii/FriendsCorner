import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import type { AppStore } from '../store/index.ts'
import { MysteryScreen } from './MysteryScreen.tsx'

export function mountMystery(host: HTMLElement, store: AppStore) {
  const root = createRoot(host)
  root.render(
    <StrictMode>
      <Provider store={store}>
        <MysteryScreen />
      </Provider>
    </StrictMode>,
  )
  return () => root.unmount()
}
