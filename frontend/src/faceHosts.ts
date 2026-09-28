// Where a game world holds the two face videos: yours first, then your call
// partner's. Null keeps them in the face dock.
export function faceHosts(world: string | null, root: ParentNode): [HTMLElement, HTMLElement] | null {
  const pair = (you: string, them: string) => {
    const mine = root.querySelector<HTMLElement>(you)
    const theirs = root.querySelector<HTMLElement>(them)
    return mine && theirs ? ([mine, theirs] as [HTMLElement, HTMLElement]) : null
  }

  switch (world) {
    case 'tictactoe':
      return pair('#card-left .player-face', '#card-right .player-face')
    case 'chess':
      return pair('#chess-card-left .player-face', '#chess-card-right .player-face')
    case 'tiles':
      return pair('#tiles-world [data-face="you"]', '#tiles-world [data-face="partner"]')
    default:
      return null
  }
}
