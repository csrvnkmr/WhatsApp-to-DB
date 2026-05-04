import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { useThemeStore } from '@/stores/theme'
import App from './App.vue'
import './main.css'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)
useThemeStore()
app.mount('#app')
