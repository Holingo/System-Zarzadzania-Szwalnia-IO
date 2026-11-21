import { useEffect, useState } from 'react'
import { api } from './api'

export function useResource(path, { revision, paused, expire }, poll = false) {
  const [result, setResult] = useState(null)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    if (paused) return
    let active = true
    let request = null
    async function load() {
      if (request) return
      request = new AbortController()
      try {
        const data = await api(path, { signal: request.signal })
        if (active) setResult({ path, data })
      } catch (error) {
        if (!active) return
        if (error.status === 401) expire(error.message)
        else
          setResult((previous) => ({
            path,
            error: error.message,
            data: previous?.path === path ? previous.data : undefined,
          }))
      } finally {
        request = null
      }
    }
    load()
    const timer =
      poll &&
      setInterval(() => {
        if (!document.hidden) load()
      }, 5000)
    return () => {
      active = false
      request?.abort()
      if (timer) clearInterval(timer)
    }
  }, [path, revision, paused, expire, poll, attempt])

  const current = result?.path === path ? result : null
  return {
    ...current,
    loading: !current,
    retry: () => setAttempt((value) => value + 1),
  }
}
