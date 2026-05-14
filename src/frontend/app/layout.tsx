import type { Metadata } from 'next'
import { Analytics } from '@vercel/analytics/next'
import './globals.css'

export const metadata: Metadata = {
  title: 'Cabrynt - Autonomous Mobility Platform',
  description: 'Scalable driverless taxi fleet system with real-time telemetry, smart pricing, and autonomous ride management',
  icons: {
    icon: '/cabrynt-favicon.svg',
    shortcut: '/cabrynt-favicon.svg',
  },
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" className="bg-background">
      <body className="font-sans antialiased">
        {children}
        {process.env.NODE_ENV === 'production' && <Analytics />}
      </body>
    </html>
  )
}
