import {
  ChartBar,
  DeviceMobileCamera,
  HandPalm,
  Link,
  ShieldCheck,
  ThumbsUp
} from '@phosphor-icons/react'
import type { Icon } from '@phosphor-icons/react'

type Benefit = {
  icon: Icon
  title: string
  body: string
}

const benefits: Benefit[] = [
  {
    icon: ThumbsUp,
    title: 'Easy',
    body: 'Paste a long link and get a shorter one in seconds.'
  },
  {
    icon: Link,
    title: 'Shareable',
    body: 'Cleaner URLs for posts, bios, messages, documents, and QR codes.'
  },
  {
    icon: ShieldCheck,
    title: 'Controlled',
    body: 'Sign in when you want saved links and ownership over what you create.'
  },
  {
    icon: ChartBar,
    title: 'Measurable',
    body: 'Keep room for click counts and details without making creation slower.'
  },
  {
    icon: HandPalm,
    title: 'Low friction',
    body: 'No account wall for the first link. Use it once or keep going.'
  },
  {
    icon: DeviceMobileCamera,
    title: 'Device friendly',
    body: 'Works from desktop or mobile with the same simple flow.'
  }
]

export function Benefits() {
  return (
    <section className="benefits" aria-label="Shorth benefits">
      {benefits.map(({ icon: BenefitIcon, title, body }) => (
        <article className="benefit-card" key={title}>
          <BenefitIcon className="ph-icon" weight="regular" />
          <h2>{title}</h2>
          <p>{body}</p>
        </article>
      ))}
    </section>
  )
}
