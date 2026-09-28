import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import type { Faces } from '../faces.ts'
import type { AppStore } from '../store/index.ts'
import { LobbyScreen } from './LobbyScreen.tsx'

export function mountLobby(host: HTMLElement, store: AppStore, faces: Faces) {
  const root = createRoot(host)
  root.render(
    <StrictMode>
      <Provider store={store}>
        <LobbyScreen faces={faces} />
      </Provider>
    </StrictMode>,
  )
  return () => root.unmount()
}
