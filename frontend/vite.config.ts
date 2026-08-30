import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

const apiUrl = process.env.VITE_API_URL ?? 'http://localhost:5100'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: apiUrl,
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
  },
})
