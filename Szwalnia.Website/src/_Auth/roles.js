export const productionRoles = [
  { id: 'CuttingWorker', name: 'Pracownik krojenia' },
  { id: 'SewingWorker', name: 'Pracownik szycia' },
]
export const allRoles = [
  ...productionRoles,
  { id: 'Admin', name: 'Administrator' },
]
export const roleNames = Object.fromEntries(
  allRoles.map(({ id, name }) => [id, name]),
)

export function permissions(role) {
  const admin = role === 'Admin'
  return {
    admin,
    canCut: admin || role === 'CuttingWorker',
    canSew: admin || role === 'SewingWorker',
  }
}
