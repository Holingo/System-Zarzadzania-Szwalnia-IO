import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const proxy = Object.fromEntries(
  ['/api', '/swagger', '/health'].map((path) => [
    path,
    'http://localhost:5080',
  ]),
)

export default defineConfig({
  plugins: [react()],
  server: { proxy },
  preview: { proxy },
})
