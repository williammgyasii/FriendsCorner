export type Layout = 'portrait' | 'sideways' | 'desktop'

export function pickLayout(width: number, height: number): Layout {
  if (width > height && height < 500) {
    return 'sideways'
  }

  if (width < 720) {
    return 'portrait'
  }

  return 'desktop'
}
