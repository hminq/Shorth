import { StrictMode } from 'react'
import type { ComponentType } from 'react'
import { createRoot } from 'react-dom/client'
import './style.css'
import { AuthCallbackPage } from './components/AuthCallbackPage'
import { HomePage } from './components/HomePage'
import { LoginPage } from './components/LoginPage'
import { LinkAnalyticsPage } from './components/LinkAnalyticsPage'
import { MyLinksPage } from './components/MyLinksPage'
import { ProfilePage } from './components/ProfilePage'
import { RegisterPage } from './components/RegisterPage'
import { RegisterVerifyPage } from './components/RegisterVerifyPage'

const app = document.querySelector<HTMLDivElement>('#app')

if (!app) {
  throw new Error('App root is missing.')
}

const routes: Record<string, ComponentType> = {
  '/': HomePage,
  '/login': LoginPage,
  '/register': RegisterPage,
  '/register/verify': RegisterVerifyPage,
  '/auth/callback': AuthCallbackPage,
  '/links': MyLinksPage,
  '/profile': ProfilePage
}

const Page = window.location.pathname.match(/^\/links\/[^/]+\/analytics$/)
  ? LinkAnalyticsPage
  : routes[window.location.pathname] ?? HomePage

createRoot(app).render(
  <StrictMode>
    <Page />
  </StrictMode>
)
