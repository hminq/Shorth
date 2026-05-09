import './style.css'

document.querySelector<HTMLDivElement>('#app')!.innerHTML = `
<main class="shell">
  <img class="brandmark" src="/shorth_logo.svg" alt="Shorth logo" />
  <p class="eyebrow">URL Shortener</p>
  <h1>Shorth</h1>
  <p class="lead">Fast link creation, clean redirects, and room for analytics when you need them.</p>
</main>
`
