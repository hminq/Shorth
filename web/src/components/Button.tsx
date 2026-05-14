import type { ButtonHTMLAttributes, PropsWithChildren } from 'react'

type ButtonProps = PropsWithChildren<ButtonHTMLAttributes<HTMLButtonElement>> & {
  variant?: 'primary' | 'secondary' | 'text'
}

export function Button({
  children,
  variant = 'primary',
  className = '',
  ...props
}: ButtonProps) {
  return (
    <button className={`button button-${variant} ${className}`.trim()} {...props}>
      {children}
    </button>
  )
}
