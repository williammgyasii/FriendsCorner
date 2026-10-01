import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Provider } from 'react-redux'
import type { HomeView } from '../homeLook.ts'
import type { AppStore } from '../store/index.ts'
import { HomeScreen } from './HomeScreen.tsx'

export function mountHome(host: HTMLElement, view: HomeView, email: string, store: AppStore) {
  const root = createRoot(host)
  root.render(
    <StrictMode>
      <Provider store={store}>
        <HomeScreen view={view} email={email} />
      </Provider>
    </StrictMode>,
  )
  return () => root.unmount()
}
