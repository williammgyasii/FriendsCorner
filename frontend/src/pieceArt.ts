const drawings = import.meta.glob<string>('./assets/pieces/*.svg', { eager: true, query: '?url', import: 'default' })

export function pieceImage(piece: string): string {
  const shade = piece === piece.toUpperCase() ? 'l' : 'd'
  return drawings[`./assets/pieces/${piece.toLowerCase()}${shade}.svg`]
}
