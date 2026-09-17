import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import Antd from 'ant-design-vue'

import App from './App.vue'
import router from './router/index.ts'
import { initializeGoogleAnalytics } from './services/analytics'
import 'ant-design-vue/dist/reset.css'

initializeGoogleAnalytics()

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.use(Antd).mount('#app')
