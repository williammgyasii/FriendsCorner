import type { HomeView } from '../homeLook.ts'
import { DashboardScreen } from './DashboardScreen.tsx'

type Props = {
  view: HomeView
  email: string
}

export function HomeScreen(props: Props) {
  return <DashboardScreen {...props} />
}
