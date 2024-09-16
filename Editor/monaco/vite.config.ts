import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          editorWorker: ['monaco-editor/esm/vs/editor/editor.worker'],
        },
      },
    },
  },
})