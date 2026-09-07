import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    // CommissionQuote.Service's default HTTP dev port (Properties/launchSettings.json).
    proxy: {
      '/api': 'http://localhost:5172',
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/setupTests.js',
  },
})
