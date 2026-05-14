import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './style.css'
import { HomePage } from './components/HomePage'

const app = document.querySelector<HTMLDivElement>('#app')

if (!app) {
  throw new Error('App root is missing.')
}

createRoot(app).render(
  <StrictMode>
    <HomePage />
  </StrictMode>
)
