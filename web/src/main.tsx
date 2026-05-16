import { StrictMode } from 'react'
import type { ComponentType } from 'react'
import { createRoot } from 'react-dom/client'
import './style.css'
import { AuthCallbackPage } from './components/AuthCallbackPage'
import { HomePage } from './components/HomePage'
import { LoginPage } from './components/LoginPage'
import { MyLinksPage } from './components/MyLinksPage'
import { ProfilePage } from './components/ProfilePage'

const app = document.querySelector<HTMLDivElement>('#app')

if (!app) {
  throw new Error('App root is missing.')
}

const routes: Record<string, ComponentType> = {
  '/': HomePage,
  '/login': LoginPage,
  '/auth/callback': AuthCallbackPage,
  '/links': MyLinksPage,
  '/profile': ProfilePage
}

const Page = routes[window.location.pathname] ?? HomePage

createRoot(app).render(
  <StrictMode>
    <Page />
  </StrictMode>
)
