import { useState } from 'react'
import { api } from '../../shared/api'
import { monday, money, number } from '../../shared/format'
import { ResourceStatus } from '../../shared/ResourceStatus'
import { Status } from '../../shared/Status'
import { Table } from '../../shared/Table'
import { useResource } from '../../shared/useResource'

export function Reports({ query, run, busy }) {
  const reports = useResource('/reports', query, true)
  const [result, setResult] = useState(null)

  function create(event) {
    event.preventDefault()
    const weekStart = new FormData(event.currentTarget).get('weekStart')
    run(
      () => api('/reports', { method: 'POST', body: { weekStart } }),
      'Raport dodany do kolejki.',
    )
  }

  return (
    <>
      <form
        className="panel mb-6 flex flex-wrap items-end gap-4 p-5"
        onSubmit={create}
      >
        <label>
          Tydzień od poniedziałku
          <input
            name="weekStart"
            className="mt-2"
            type="date"
            defaultValue={monday()}
            required
          />
        </label>
        <button className="btn" disabled={busy}>
          Generuj raport
        </button>
        <p className="pb-2 text-xs text-slate-600">
          Raport obejmuje zakończenia w tygodniu według czasu Europe/Warsaw.
        </p>
      </form>
      <ResourceStatus resource={reports} />
      {reports.data && (
        <Table
          headings={['Tydzień od', 'Status', 'Wynik']}
          emptyText="Nie wygenerowano jeszcze raportów."
        >
          {reports.data.map((report) => (
            <tr key={report.id}>
              <td>{report.weekStart}</td>
              <td>
                <Status value={report.status} />
                {report.error && (
                  <p className="mt-2 text-red-700">{report.error}</p>
                )}
              </td>
              <td>
                {report.status === 'Completed' && (
                  <button
                    type="button"
                    className="btn secondary"
                    disabled={busy}
                    onClick={() =>
                      run(async () =>
                        setResult((await api('/reports/' + report.id)).result),
                      )
                    }
                  >
                    Otwórz raport
                  </button>
                )}
              </td>
            </tr>
          ))}
        </Table>
      )}
      {result && <ReportResult result={result} />}
    </>
  )
}

function ReportResult({ result }) {
  const rows = [
    ...result.cuts.map((row) => ({
      ...row,
      stage: 'Krojenie',
      detail: row.variant,
    })),
    ...result.sews.map((row) => ({
      ...row,
      stage: 'Szycie',
      detail: row.seamstress,
    })),
  ]
  const filename = `raport-${result.weekStart}.json`
  const download =
    'data:application/json;charset=utf-8,' +
    encodeURIComponent(JSON.stringify(result, null, 2))

  return (
    <section className="panel mt-6 p-6">
      <div className="flex flex-wrap justify-between gap-3">
        <h2 className="text-lg font-semibold">
          {result.weekStart} – {result.weekEnd}
        </h2>
        <a className="btn secondary" href={download} download={filename}>
          Pobierz JSON
        </a>
      </div>
      <div className="my-5 grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl bg-mint p-5">
          <p>Skrojone komplety</p>
          <p className="mt-2 text-3xl font-bold">{result.cutQuantity}</p>
          <p>{money(result.cuttingCost)}</p>
        </div>
        <div className="rounded-xl bg-slate-100 p-5">
          <p>Uszyte komplety</p>
          <p className="mt-2 text-3xl font-bold">{result.sewnQuantity}</p>
          <p>{money(result.sewingCost)}</p>
        </div>
        <div className="rounded-xl bg-slate-100 p-5">
          <p>Zużyty materiał</p>
          <p className="mt-2 text-3xl font-bold">
            {number(result.materialMeters)} m
          </p>
        </div>
      </div>
      <Table
        headings={['Etap', 'Zlecenie', 'Produkt / wykonawca', 'Ilość', 'Koszt']}
      >
        {rows.map((row) => (
          <tr key={row.stage + row.id}>
            <td>{row.stage}</td>
            <td>#{row.id}</td>
            <td>
              {row.product} · {row.detail}
            </td>
            <td>{row.quantity}</td>
            <td>{money(row.cost)}</td>
          </tr>
        ))}
      </Table>
    </section>
  )
}
