import { useState } from 'react'
import { useAuth } from './AuthProvider'

export function LoginPage() {
  const { error, signIn } = useAuth()
  const [failure, setFailure] = useState(null)
  const [busy, setBusy] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setBusy(true)
    setFailure('')
    try {
      await signIn(Object.fromEntries(new FormData(event.currentTarget)))
    } catch (error) {
      setFailure(error.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="grid min-h-screen place-items-center p-5">
      <form className="panel w-full max-w-md p-8 sm:p-10" onSubmit={submit}>
        <h1 className="mb-7 text-3xl font-bold text-forest">Szwalnia</h1>
        <label className="mb-5">
          Login
          <input
            className="mt-2"
            name="name"
            autoComplete="username"
            required
            maxLength={100}
          />
        </label>
        <label className="mb-6">
          Hasło
          <input
            className="mt-2"
            name="password"
            type="password"
            autoComplete="current-password"
            required
            maxLength={200}
          />
        </label>
        {(failure ?? error) && (
          <p role="alert" className="mb-4 text-sm text-red-700">
            {failure ?? error}
          </p>
        )}
        <button className="btn w-full" disabled={busy}>
          {busy ? 'Logowanie…' : 'Zaloguj się'}
        </button>
      </form>
    </main>
  )
}
