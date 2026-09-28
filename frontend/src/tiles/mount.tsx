import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import type { AppStore } from '../store/index.ts'
import { TilesScreen } from './TilesScreen.tsx'

export function mountTiles(host: HTMLElement, store: AppStore) {
  const root = createRoot(host)
  root.render(
    <StrictMode>
      <Provider store={store}>
        <TilesScreen />
      </Provider>
    </StrictMode>,
  )
  return () => root.unmount()
}
