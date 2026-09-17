const measurementId = import.meta.env.VITE_GA_MEASUREMENT_ID?.trim()

type GtagArguments = [command: string, ...parameters: unknown[]]

declare global {
  interface Window {
    dataLayer: GtagArguments[]
    gtag: (...args: GtagArguments) => void
  }
}

export function initializeGoogleAnalytics() {
  if (!measurementId || document.querySelector('script[data-google-analytics]')) {
    return
  }

  window.dataLayer = window.dataLayer || []
  window.gtag = (...args: GtagArguments) => {
    window.dataLayer.push(args)
  }

  window.gtag('js', new Date())
  window.gtag('config', measurementId, {
    send_page_view: false,
  })

  const script = document.createElement('script')
  script.async = true
  script.dataset.googleAnalytics = measurementId
  script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(measurementId)}`
  document.head.append(script)
}

export function trackPageView() {
  if (!measurementId || !window.gtag) {
    return
  }

  window.gtag('event', 'page_view', {
    page_title: document.title,
    page_location: window.location.href,
  })
}
