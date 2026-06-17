import { MutationCache, QueryCache, QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import { createPinia } from 'pinia'
import { createApp } from 'vue'

import App from './App.vue'
import router from './router'
import { notifyApiError } from './services/http'
import './styles/app.scss'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
  queryCache: new QueryCache({
    onError: (error, query) => {
      if ((query.meta as { suppressGlobalError?: boolean } | undefined)?.suppressGlobalError) {
        return
      }

      notifyApiError(error, 'Khong tai duoc du lieu.')
    },
  }),
  mutationCache: new MutationCache({
    onError: (error) => notifyApiError(error, 'Thao tac that bai.'),
  }),
})

createApp(App)
  .use(createPinia())
  .use(router)
  .use(ElementPlus)
  .use(VueQueryPlugin, { queryClient })
  .mount('#app')
