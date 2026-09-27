import { defineConfig } from 'vite'

export default defineConfig({
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: {
      '/rooms': 'http://localhost:5250',
      '/ws': {
        target: 'http://localhost:5250',
        ws: true,
      },
    },
  },
})
