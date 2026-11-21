import { allRoles, productionRoles } from '../../_Auth/roles'
import { number, today } from '../../shared/format'

const field = (key, label, type = 'text', extra = {}) => ({
  key,
  label,
  type,
  ...extra,
})
const numeric = (key, label, min = 0, step = '0.0001') =>
  field(key, label, 'number', { min, step })
const select = (key, label, items) => field(key, label, 'select', { items })

export function initialValues({ row, initial }) {
  const defaults = {
    widthCm: 160,
    remainingMeters: 0,
    metersPerSet: 1,
    cutSetupMinutes: 0,
    cutMinutesPerSet: 0,
    cutSetupCost: 0,
    cutCostPerSet: 0,
    sewMinutesPerSet: 0,
    sewCostPerSet: 0,
    quantity: 1,
    dueDate: today(),
    role: 'CuttingWorker',
  }
  return { ...defaults, ...row, ...initial }
}

export function compatibleRolls(options, productId) {
  const product = options.products.find((item) => item.id === Number(productId))
  return options.rolls.filter(
    (roll) =>
      !product ||
      (roll.materialId === product.materialId &&
        roll.widthCm === product.widthCm),
  )
}

export function fieldsFor({ kind, resource, row }, options, form) {
  if (kind === 'changeRole') return [select('role', 'Rola', productionRoles)]
  if (kind === 'finishCut')
    return [
      numeric('actualQuantity', 'Poprawne komplety (szt.)', 0, '1'),
      numeric('actualMeters', 'Rzeczywiste zużycie materiału (m)'),
    ]

  const orderFields = (stage) => [
    numeric('quantity', 'Liczba kompletów', 1, '1'),
    field('dueDate', 'Termin ' + stage, 'date'),
  ]
  switch (resource) {
    case 'materials':
      return [field('name', 'Nazwa materiału')]
    case 'seamstresses':
      return [
        field('name', 'Imię i nazwisko / nazwa'),
        field('phone', 'Telefon (opcjonalny)', 'text', {
          optional: true,
          maxLength: 100,
        }),
      ]
    case 'users':
      return [
        field('name', 'Login (minimum 3 znaki)', 'text', {
          minLength: 3,
          maxLength: 100,
        }),
        field('password', 'Hasło (minimum 12 znaków)', 'password', {
          minLength: 12,
          maxLength: 200,
          autoComplete: 'new-password',
        }),
        select('role', 'Rola', allRoles),
      ]
    case 'rolls':
      return [
        field('name', 'Numer / nazwa belki'),
        select('materialId', 'Materiał', options.materials),
        numeric('widthCm', 'Szerokość (cm)', 1),
        numeric(
          'remainingMeters',
          'Pozostała długość, łącznie z rezerwacją (m)',
        ),
      ]
    case 'products':
      return [
        field('name', 'Nazwa produktu'),
        field('variant', 'Wariant / rozmiar / kolor'),
        select('materialId', 'Materiał', options.materials),
        numeric('widthCm', 'Szerokość normy (cm)', 1),
        numeric('metersPerSet', 'Zużycie na komplet (m)', 0.0001),
        numeric('cutSetupMinutes', 'Przygotowanie krojenia (min)'),
        numeric('cutMinutesPerSet', 'Krojenie kompletu (min)'),
        numeric('cutSetupCost', 'Przygotowanie krojenia (PLN)'),
        numeric('cutCostPerSet', 'Krojenie kompletu (PLN)'),
        numeric('sewMinutesPerSet', 'Szycie kompletu (min)'),
        numeric('sewCostPerSet', 'Szycie kompletu (PLN)'),
      ]
    case 'cuts':
      return [
        ...(row
          ? []
          : [
              select(
                'productId',
                'Produkt i wariant',
                options.products.map((item) => ({
                  id: item.id,
                  name: `${item.name} · ${item.variant}`,
                })),
              ),
              select(
                'rollId',
                'Belka materiału',
                compatibleRolls(options, form.productId).map((item) => ({
                  id: item.id,
                  name: `${item.name} · ${number(item.availableMeters)} m dostępne`,
                })),
              ),
            ]),
        ...orderFields('krojenia'),
      ]
    case 'sews':
      return [
        ...(row
          ? []
          : [
              select(
                'batchId',
                'Partia wykrojów',
                options.batches
                  .filter((item) => item.available > 0)
                  .map((item) => ({
                    id: item.id,
                    name: `#${item.id} · ${item.product} · ${item.available} dostępnych`,
                  })),
              ),
            ]),
        select('seamstressId', 'Chałupniczka', options.seamstresses),
        ...orderFields('szycia'),
      ]
    default:
      return []
  }
}

export function estimateFor({ resource, kind, row }, options, form) {
  if (row || kind) return null
  const quantity = Number(form.quantity) || 0
  if (resource === 'cuts') {
    const product = options.products.find(
      (item) => item.id === Number(form.productId),
    )
    if (!product) return null
    const roll = options.rolls.find((item) => item.id === Number(form.rollId))
    return {
      cost: product.cutSetupCost + quantity * product.cutCostPerSet,
      minutes: product.cutSetupMinutes + quantity * product.cutMinutesPerSet,
      meters: quantity * product.metersPerSet,
      max: roll
        ? Math.floor(roll.availableMeters / product.metersPerSet)
        : null,
    }
  }
  if (resource === 'sews') {
    const batch = options.batches.find(
      (item) => item.id === Number(form.batchId),
    )
    if (batch)
      return {
        cost: quantity * batch.sewCostPerSet,
        minutes: quantity * batch.sewMinutesPerSet,
        max: batch.available,
      }
  }
  return null
}
