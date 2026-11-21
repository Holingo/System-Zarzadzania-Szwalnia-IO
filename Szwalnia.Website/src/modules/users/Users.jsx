import { roleNames } from '../../_Auth/roles'
import { ResourceStatus } from '../../shared/ResourceStatus'
import { Table } from '../../shared/Table'
import { useResource } from '../../shared/useResource'

export function Users({ query, busy, edit }) {
  const users = useResource('/auth/users', query)
  return (
    <>
      <ResourceStatus resource={users} />
      {users.data && (
        <Table headings={['Login', 'Rola', 'Akcje']}>
          {users.data.map((row) => (
            <tr key={row.id}>
              <td>{row.name}</td>
              <td>{roleNames[row.role]}</td>
              <td>
                {row.role !== 'Admin' && (
                  <button
                    type="button"
                    className="btn secondary"
                    disabled={busy}
                    onClick={() =>
                      edit({ resource: 'users', kind: 'changeRole', row })
                    }
                  >
                    Zmień rolę
                  </button>
                )}
              </td>
            </tr>
          ))}
        </Table>
      )}
    </>
  )
}
