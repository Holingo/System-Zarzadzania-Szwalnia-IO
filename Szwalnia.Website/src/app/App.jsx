import { useRef, useState } from 'react'
import { useAuth } from '../_Auth/AuthProvider'
import { LoginPage } from '../_Auth/LoginPage'
import { permissions, roleNames } from '../_Auth/roles'
import { Dashboard } from '../modules/dashboard/Dashboard'
import { Orders } from '../modules/production/Orders'
import { Catalog } from '../modules/catalog/Catalog'
import { EditDialog } from '../modules/editor/EditDialog'
import { Reports } from '../modules/reports/Reports'
import { Users } from '../modules/users/Users'
import { useResource } from '../shared/useResource'
import { ResourceStatus } from '../shared/ResourceStatus'

const emptyOptions = {
  materials: [],
  products: [],
  rolls: [],
  seamstresses: [],
  batches: [],
}
const adminTabs = [
  ['reports', 'Raporty tygodniowe'],
  ['products', 'Produkty i normy'],
  ['materials', 'Materiały'],
  ['rolls', 'Belki materiału'],
  ['seamstresses', 'Chałupniczki'],
  ['users', 'Użytkownicy'],
]
const assign = (batch) => ({ resource: 'sews', initial: { batchId: batch.id } })

export function App() {
  const { user, checking } = useAuth()
  if (checking)
    return (
      <p role="status" className="grid min-h-screen place-items-center">
        Ładowanie aplikacji…
      </p>
    )
  return user ? <Workspace key={user.id} user={user} /> : <LoginPage />
}

function Workspace({ user }) {
  const auth = useAuth()
  const access = permissions(user.role)
  const [tab, setTab] = useState('dashboard')
  const [busy, setBusy] = useState(false)
  const [revision, setRevision] = useState(0)
  const [message, setMessage] = useState(null)
  const [editor, setEditor] = useState(null)
  const lock = useRef(false)
  const query = {
    revision,
    paused: busy || Boolean(editor),
    expire: auth.expire,
  }
  const catalog = useResource(
    '/catalog/options',
    query,
    tab === 'dashboard' && access.canSew,
  )
  const options = catalog.data ?? emptyOptions
  const tabs = [
    ['dashboard', 'Przegląd'],
    ...(access.canCut ? [['cuts', 'Krojenie']] : []),
    ...(access.canSew
      ? [
          ['sews', 'Szycie'],
          ['batches', 'Partie wykrojów'],
        ]
      : []),
    ...(access.admin ? adminTabs : []),
  ]
  const canCreate =
    tab === 'cuts' ||
    tab === 'sews' ||
    (access.admin &&
      adminTabs.some(([key]) => key === tab) &&
      tab !== 'reports')

  async function run(action, success, onError) {
    if (lock.current) return
    lock.current = true
    setBusy(true)
    setMessage(null)
    try {
      await action()
      if (success) {
        setMessage({ text: success })
        setRevision((value) => value + 1)
      }
    } catch (error) {
      if (error.status === 401) auth.expire(error.message)
      else if (onError) onError(error.message)
      else setMessage({ text: error.message, error: true })
    } finally {
      lock.current = false
      setBusy(false)
    }
  }

  function navigate(next) {
    setMessage(null)
    setTab(next)
  }

  const props = {
    tab,
    options,
    busy,
    query,
    run,
    edit: setEditor,
    onAssign: (batch) => setEditor(assign(batch)),
  }
  let page
  switch (tab) {
    case 'dashboard':
      page = <Dashboard {...props} {...access} navigate={navigate} />
      break
    case 'cuts':
    case 'sews':
      page = <Orders key={tab} {...props} />
      break
    case 'reports':
      page = <Reports {...props} />
      break
    case 'users':
      page = <Users {...props} />
      break
    default:
      page = <Catalog key={tab} {...props} />
  }

  return (
    <div className="min-h-screen lg:flex">
      <aside className="bg-forest p-5 text-white lg:sticky lg:top-0 lg:flex lg:h-screen lg:w-64 lg:shrink-0 lg:flex-col lg:overflow-y-auto">
        <div className="mb-7 px-3 pt-3 text-2xl font-bold">Szwalnia</div>
        <nav
          className="flex flex-wrap gap-1 lg:block lg:space-y-1"
          aria-label="Nawigacja główna"
        >
          {tabs.map(([key, label]) => (
            <button
              key={key}
              type="button"
              disabled={busy}
              aria-current={tab === key ? 'page' : undefined}
              className={
                'block rounded-lg px-3 py-2.5 text-left text-sm lg:w-full ' +
                (tab === key
                  ? 'bg-mint font-semibold text-forest'
                  : 'text-emerald-50/75 hover:bg-white/10')
              }
              onClick={() => navigate(key)}
            >
              {label}
            </button>
          ))}
        </nav>
        <div className="mt-6 border-t border-white/20 px-3 py-5 lg:mt-auto">
          <p className="text-sm font-semibold">{user.name}</p>
          <p className="mt-1 text-xs text-emerald-100/80">
            {roleNames[user.role]}
          </p>
          <button
            type="button"
            className="mt-4 text-sm underline"
            disabled={busy}
            onClick={() => run(auth.signOut)}
          >
            Wyloguj się
          </button>
        </div>
      </aside>
      <main className="min-w-0 flex-1 p-5 lg:p-10">
        <header className="mb-7 flex flex-wrap items-center justify-between gap-4">
          <h1 className="text-3xl font-bold text-forest">
            {tabs.find(([key]) => key === tab)?.[1]}
          </h1>
          {canCreate && (
            <button
              type="button"
              className="btn"
              disabled={busy || !catalog.data}
              onClick={() => setEditor({ resource: tab })}
            >
              {tab === 'users' ? 'Dodaj użytkownika' : 'Dodaj'}
            </button>
          )}
        </header>
        {message && (
          <p
            role={message.error ? 'alert' : 'status'}
            className={`mb-5 rounded-xl p-4 text-sm ${message.error ? 'bg-red-50 text-red-800' : 'bg-mint text-forest'}`}
          >
            {message.text}
          </p>
        )}
        <ResourceStatus resource={catalog} />
        {page}
      </main>
      {editor && (
        <EditDialog
          editor={editor}
          options={options}
          busy={busy}
          run={run}
          onClose={() => setEditor(null)}
        />
      )}
    </div>
  )
}
