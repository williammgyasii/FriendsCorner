// @vitest-environment jsdom
import { render, screen } from '@testing-library/react'
import { expect, test } from 'vitest'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

test('a shadcn button renders as a real button with its label', () => {
  render(<Button>Open a lobby</Button>)

  expect(screen.getByRole('button', { name: 'Open a lobby' })).toBeInTheDocument()
})

test('when two classes set the same thing, the later one wins', () => {
  expect(cn('rounded-md px-2', 'rounded-2xl')).toBe('px-2 rounded-2xl')
})
