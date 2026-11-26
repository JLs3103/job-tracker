import { createHash } from 'node:crypto'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

const cacheId = createHash('sha256')
  .update(process.cwd())
  .digest('hex')
  .slice(0, 12)

// https://vite.dev/config/
export default defineConfig({
  cacheDir: join(tmpdir(), 'job-tracker-vite', cacheId),
  plugins: [react(), tailwindcss()],
})
