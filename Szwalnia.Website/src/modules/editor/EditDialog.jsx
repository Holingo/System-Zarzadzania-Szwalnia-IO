import { useState } from 'react'
import { api } from '../../shared/api'
import { Dialog } from '../../shared/Dialog'
import { money, number } from '../../shared/format'
import {
  compatibleRolls,
  estimateFor,
  fieldsFor,
  initialValues,
} from './fields'

function save({ kind, resource, row }, data) {
  if (kind === 'changeRole')
    return api(`/auth/users/${row.id}/role`, {
      method: 'PUT',
      body: { ...data, version: row.version },
    })
  if (kind === 'finishCut')
    return api(`/cuts/${row.id}/status`, {
      method: 'POST',
      body: { ...data, status: 'Cut', version: row.version },
    })
  const production = resource === 'cuts' || resource === 'sews'
  const path =
    resource === 'users'
      ? '/auth/users'
      : production
        ? '/' + resource
        : '/catalog/' + resource
  if (!row) return api(path, { method: 'POST', body: data })
  return api(
    path +
      '/' +
      row.id +
      (production ? '' : '?version=' + encodeURIComponent(row.version)),
    {
      method: 'PUT',
      body: production ? { ...data, version: row.version } : data,
    },
  )
}

export function EditDialog({ editor, options, busy, run, onClose }) {
  const [form, setForm] = useState(() => initialValues(editor))
  const [error, setError] = useState('')
  const fields = fieldsFor(editor, options, form)
  const estimate = estimateFor(editor, options, form)
  const title =
    editor.kind === 'changeRole'
      ? `Zmień rolę konta ${editor.row.name}`
      : editor.kind === 'finishCut'
        ? 'Zakończ krojenie'
        : editor.row
          ? 'Edytuj dane'
          : 'Dodaj nowe dane'

  function change(key, value) {
    setForm((previous) => {
      const next = { ...previous, [key]: value }
      if (
        key === 'productId' &&
        !compatibleRolls(options, value).some(
          (roll) => roll.id === Number(previous.rollId),
        )
      )
        next.rollId = ''
      return next
    })
  }

  function submit(event) {
    event.preventDefault()
    const data = Object.fromEntries(
      fields.map(({ key, type }) => [
        key,
        type === 'number' || (type === 'select' && key !== 'role')
          ? Number(form[key])
          : (form[key] ?? ''),
      ]),
    )
    setError('')
    run(
      async () => {
        await save(editor, data)
        onClose()
      },
      'Zapisano zmiany.',
      setError,
    )
  }

  return (
    <Dialog title={title} busy={busy} onClose={onClose}>
      <form onSubmit={submit}>
        <fieldset disabled={busy} className="grid gap-4 sm:grid-cols-2">
          {fields.map(
            ({ key, label, type, items, optional, ...constraints }) => (
              <label key={key}>
                {label}
                {type === 'select' ? (
                  <select
                    className="mt-2"
                    value={form[key] ?? ''}
                    required
                    onChange={(event) => change(key, event.target.value)}
                  >
                    <option value="" disabled>
                      Wybierz…
                    </option>
                    {items.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.name}
                      </option>
                    ))}
                  </select>
                ) : (
                  <input
                    className="mt-2"
                    type={type}
                    value={form[key] ?? ''}
                    required={!optional}
                    maxLength={150}
                    {...constraints}
                    onChange={(event) => change(key, event.target.value)}
                  />
                )}
              </label>
            ),
          )}
        </fieldset>
        {estimate && (
          <div className="mt-5 rounded-xl bg-mint p-4 text-sm">
            <p className="font-semibold">
              {money(estimate.cost)} · {number(estimate.minutes)} min
            </p>
            <p className="mt-1">
              {estimate.max != null && (
                <>Maksymalnie {estimate.max} kompletów. </>
              )}
              {estimate.meters != null && (
                <>Zapotrzebowanie {number(estimate.meters)} m.</>
              )}
            </p>
          </div>
        )}
        {error && (
          <p
            role="alert"
            className="mt-5 rounded-lg bg-red-50 p-3 text-sm text-red-800"
          >
            {error}
          </p>
        )}
        <div className="mt-6 flex justify-end gap-3">
          <button
            type="button"
            className="btn secondary"
            disabled={busy}
            onClick={onClose}
          >
            Anuluj
          </button>
          <button className="btn" disabled={busy}>
            {busy ? 'Zapisywanie…' : 'Zapisz'}
          </button>
        </div>
      </form>
    </Dialog>
  )
}
