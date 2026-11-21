import { useEffect, useState } from 'react'
import { api } from '../../shared/api'
import { Dialog } from '../../shared/Dialog'
import { localDateTime, money, number } from '../../shared/format'
import { ResourceStatus } from '../../shared/ResourceStatus'
import { Status, statusLabel } from '../../shared/Status'
import { Table } from '../../shared/Table'
import { useResource } from '../../shared/useResource'

const pageSize = 20
const stages = {
  cuts: ['Queued', 'Cutting', 'Cut'],
  sews: ['New', 'Sewing', 'Sewn'],
}

export function Orders({ tab, options, busy, query, run, edit }) {
  const [filters, setFilters] = useState({})
  const [page, setPage] = useState(1)
  const [history, setHistory] = useState(null)
  const filterQuery = Object.fromEntries(
    Object.entries(filters).filter(([, value]) => value !== ''),
  )
  const resource = useResource(
    '/' + tab + '?' + new URLSearchParams({ ...filterQuery, page }),
    query,
  )
  const { items = [], total = 0 } = resource.data ?? {}
  const lastPage = Math.max(1, Math.ceil(total / pageSize))
  const cutting = tab === 'cuts'
  const [first, second, last] = stages[tab]

  useEffect(() => {
    if (resource.data && page > lastPage) setPage(lastPage)
  }, [resource.data, page, lastPage])

  function filter(event) {
    event.preventDefault()
    setFilters(Object.fromEntries(new FormData(event.currentTarget)))
    setPage(1)
  }

  function advance(row) {
    if (cutting && row.status === second) {
      edit({
        kind: 'finishCut',
        resource: tab,
        row,
        initial: {
          actualQuantity: row.quantity,
          actualMeters: row.estimatedMeters,
        },
      })
      return
    }
    run(
      () =>
        api(`/${tab}/${row.id}/status`, {
          method: 'POST',
          body: {
            version: row.version,
            status: row.status === first ? second : last,
          },
        }),
      'Zmieniono status.',
    )
  }

  function remove(row) {
    if (!window.confirm('Usunąć zlecenie i zwolnić rezerwację?')) return
    run(
      () =>
        api(`/${tab}/${row.id}?version=${encodeURIComponent(row.version)}`, {
          method: 'DELETE',
        }),
      'Usunięto zlecenie.',
    )
  }

  return (
    <>
      <form className="mb-5 flex flex-wrap items-end gap-3" onSubmit={filter}>
        <label className="min-w-56 flex-1">
          Szukaj
          <input
            name="search"
            className="mt-1"
            placeholder="Produkt lub wykonawca"
          />
        </label>
        <label>
          Status
          <select name="status" className="mt-1">
            <option value="">Wszystkie</option>
            {stages[tab].map((status) => (
              <option key={status} value={status}>
                {statusLabel(status)}
              </option>
            ))}
          </select>
        </label>
        {!cutting && (
          <label>
            Chałupniczka
            <select name="seamstressId" className="mt-1">
              <option value="">Wszystkie</option>
              {options.seamstresses.map((person) => (
                <option key={person.id} value={person.id}>
                  {person.name}
                </option>
              ))}
            </select>
          </label>
        )}
        <button className="btn secondary" disabled={busy}>
          Filtruj
        </button>
      </form>
      <ResourceStatus resource={resource} />
      {resource.data && (
        <>
          <Table
            headings={[
              'Zlecenie / produkt',
              cutting ? 'Belka' : 'Chałupniczka',
              'Ilość / kalkulacja',
              'Termin',
              'Status',
              'Akcje',
            ]}
            emptyText="Brak zleceń spełniających kryteria."
          >
            {items.map((row) => (
              <tr key={row.id}>
                <td>
                  <div className="font-semibold">
                    #{row.id} · {row.product}
                  </div>
                  <div className="mt-1 text-xs text-slate-500">
                    {row.variant}
                  </div>
                </td>
                <td>{row.roll || row.seamstress}</td>
                <td>
                  <div className="font-semibold">
                    {row.quantity} kompletów · {money(row.estimatedCost)}
                  </div>
                  <div className="mt-1 text-xs text-slate-500">
                    {number(row.estimatedMinutes)} min{' '}
                    {cutting && <>· {number(row.estimatedMeters)} m</>}
                  </div>
                  {row.actualQuantity != null && (
                    <div className="mt-1 text-xs text-emerald-700">
                      Wykonano {row.actualQuantity} · zużyto{' '}
                      {number(row.actualMeters)} m
                    </div>
                  )}
                </td>
                <td className="whitespace-nowrap">{row.dueDate}</td>
                <td>
                  <Status value={row.status} />
                </td>
                <td>
                  <div className="flex min-w-44 flex-wrap gap-2">
                    {row.status !== last && (
                      <button
                        type="button"
                        className="btn !px-3 !py-2 !text-xs"
                        disabled={busy}
                        onClick={() => advance(row)}
                      >
                        {row.status === first ? 'Rozpocznij' : 'Zakończ'}
                      </button>
                    )}
                    {row.status === first && (
                      <>
                        <button
                          type="button"
                          className="btn secondary !px-3 !py-2 !text-xs"
                          disabled={busy}
                          onClick={() => edit({ resource: tab, row })}
                        >
                          Edytuj
                        </button>
                        <button
                          type="button"
                          className="btn danger !px-3 !py-2 !text-xs"
                          disabled={busy}
                          onClick={() => remove(row)}
                        >
                          Usuń
                        </button>
                      </>
                    )}
                    <button
                      type="button"
                      className="text-xs text-slate-600 underline"
                      disabled={busy}
                      onClick={() =>
                        run(async () =>
                          setHistory({
                            id: row.id,
                            items: await api(`/history/${tab}/${row.id}`),
                          }),
                        )
                      }
                    >
                      Historia
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </Table>
          <div className="mt-5 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-600">
            <span>
              {total} zleceń · strona {page} z {lastPage}
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                className="btn secondary"
                disabled={busy || page <= 1}
                onClick={() => setPage(page - 1)}
              >
                Wstecz
              </button>
              <button
                type="button"
                className="btn secondary"
                disabled={busy || page >= lastPage}
                onClick={() => setPage(page + 1)}
              >
                Dalej
              </button>
            </div>
          </div>
        </>
      )}
      {history && (
        <Dialog
          title={`Historia zlecenia #${history.id}`}
          onClose={() => setHistory(null)}
        >
          <ol className="space-y-4">
            {history.items.map((item, index) => (
              <li key={index} className="border-l-2 border-emerald-300 pl-4">
                <p className="font-semibold">{statusLabel(item.status)}</p>
                <p className="mt-1 text-xs text-slate-600">
                  {localDateTime(item.changedAt)} · {item.user}
                </p>
              </li>
            ))}
          </ol>
        </Dialog>
      )}
    </>
  )
}
