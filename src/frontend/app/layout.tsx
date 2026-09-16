import type { Metadata } from 'next'
import { Analytics } from '@vercel/analytics/next'
import 'leaflet/dist/leaflet.css'
import './globals.css'

export const metadata: Metadata = {
  title: 'Cabrynt - Porto Ride Duration Estimates',
  description: 'Porto ride quotes using route-aware machine learning duration estimates.',
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
