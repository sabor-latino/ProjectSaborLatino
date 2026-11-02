import { defineConfig } from 'vitest/config';
import plugin from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => ({
  plugins: [
    plugin(),
    {
      name: 'portal-module-manifest',
      generateBundle(_options, bundle) {
        if (mode !== 'public' && mode !== 'admin') return;
        const modules = [...new Set(Object.values(bundle).flatMap((output) =>
          output.type === 'chunk'
            ? Object.keys(output.modules).map((id) => {
                const normalized = id.replaceAll('\\', '/').split('?')[0];
                const offset = normalized.lastIndexOf('/frontendSL/src/');
                return offset < 0 ? null : normalized.slice(offset + '/frontendSL/'.length);
              }).filter((id): id is string => id !== null)
            : [],
        ))].sort();
        this.emitFile({
          type: 'asset', fileName: '.vite/portal-modules.json',
          source: JSON.stringify({ portal: mode === 'admin' ? 'admin' : 'publico', modules }, null, 2),
        });
      },
    },
  ],
  define: (mode === 'admin' || mode === 'public')
    ? { 'import.meta.env.VITE_PORTAL_MODE': JSON.stringify(mode) }
    : {},
  server: {
    port: mode === 'admin' ? 54527 : 54526,
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test-setup.ts',
  },
}));
