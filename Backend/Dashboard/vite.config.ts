import { defineConfig } from 'vite'
import vue, { type Options as VuePluginOptions } from '@vitejs/plugin-vue'
import eslintPlugin, { type Options as EslintPluginOptions } from 'vite-plugin-eslint'

// https://vitejs.dev/config/
export default defineConfig({
  define: {
    __VUE_OPTIONS_API__: 'true',
    __VUE_PROD_DEVTOOLS__: 'false',
    __VUE_PROD_HYDRATION_MISMATCH_DETAILS__: 'false',
  },
  plugins: [
    vue({
      template: {
        compilerOptions: {
          compatConfig: {
            MODE: 2,
            COMPILER_V_BIND_OBJECT_ORDER: "suppress-warning", // New MetaUiNext inputs trigger this warning and are hand-tested to work -> silence.
          }
        }
      }
    } satisfies VuePluginOptions),
    eslintPlugin({
      exclude: [
        '**/node_modules/**',
        '**/Frontend/**', // Added our own monorepo packages to ESLint ignore list so they don't get linted twice.
      ]
    } satisfies EslintPluginOptions),
  ],
  server: {
    port: 5551,
    strictPort: true,
    watch: {
      ignored: [/\.#/] // Ignore EMACS files
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5550'
      }
    },
    open: true
  },
  preview: {
    port: 5551
  },
  resolve: {
    alias: {
      vue: '@vue/compat' // Vue 3 compatibility mode setup
    }
  },
  build: {
    emptyOutDir: true,
    cssCodeSplit: false, // Forces all CSS to be bundled into a single file -> better for our project since we have very very small snippets all over the place
    // TODO: consider if we could optimize some of the generated chuncks
    chunkSizeWarningLimit: 2048
  }
})
