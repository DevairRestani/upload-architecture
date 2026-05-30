import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    proxy: {
      "/files": {
        target: "http://localhost:8080",
        changeOrigin: true
      },
      "/blob": {
        target: "http://localhost:10000",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/blob/, "")
      }
    }
  }
});
