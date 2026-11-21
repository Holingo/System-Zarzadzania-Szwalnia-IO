import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from 'react'
import { api } from '../shared/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [checking, setChecking] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    api('/auth/me')
      .then((session) => {
        if (active) setUser(session)
      })
      .catch((failure) => {
        if (active && failure.status !== 401) setError(failure.message)
      })
      .finally(() => {
        if (active) setChecking(false)
      })
    return () => {
      active = false
    }
  }, [])

  const expire = useCallback((message) => {
    setError(message)
    setUser(null)
  }, [])

  async function signIn(credentials) {
    setError('')
    setUser(await api('/auth/login', { method: 'POST', body: credentials }))
  }

  async function signOut() {
    await api('/auth/logout', { method: 'POST' })
    expire('')
  }

  return (
    <AuthContext.Provider
      value={{ user, checking, error, expire, signIn, signOut }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  return useContext(AuthContext)
}
