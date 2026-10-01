export type DashboardSection = 'home' | 'friends' | 'settings' | 'profile' | 'billing'

export type DashboardNavItem = {
  id: DashboardSection
  label: string
  tab: string
}

export const dashboardNav: DashboardNavItem[] = [
  { id: 'home', label: 'Home', tab: 'Home' },
  { id: 'friends', label: 'Friends', tab: 'Friends' },
  { id: 'settings', label: 'Settings', tab: 'Prefs' },
  { id: 'profile', label: 'Profile', tab: 'Profile' },
  { id: 'billing', label: 'Billing', tab: 'Plan' },
]

// Room for icons, labels, and the home indicator on phones.
export const mobileTabBarHeight = '4.5rem'

export function sectionTitle(section: DashboardSection): string {
  return dashboardNav.find((item) => item.id === section)?.label ?? 'Home'
}
