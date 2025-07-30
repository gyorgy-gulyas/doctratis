import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import path from 'path';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        port: 57941,
    },
    resolve: {
        alias: {
            // FONTOS: app.ts-t aliasoljuk, ne a buildelt app.js-t
            'docratis.ts.api': path.resolve(__dirname, './../docratis.ts.api/app.ts')
        }
    }
})
