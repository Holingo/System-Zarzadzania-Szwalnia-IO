<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { api, money, number } from './api'

const user = ref(null),
  checking = ref(true),
  busy = ref(false),
  error = ref(''),
  notice = ref('')
const login = ref({ name: '', password: '' }),
  tab = ref('dashboard'),
  options = ref({ materials: [], products: [], rolls: [], seamstresses: [], batches: [] })
const dashboard = ref({}),
  rows = ref([]),
  total = ref(0),
  page = ref(1),
  search = ref(''),
  status = ref(''),
  seamstressFilter = ref('')
const reports = ref([]),
  report = ref(null),
  users = ref([]),
  modal = ref(null),
  form = ref({}),
  modalError = ref(''),
  history = ref(null)
let timer
let polling = false
const today = () => {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Warsaw',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date())
  const part = (type) => parts.find((item) => item.type === type).value
  return `${part('year')}-${part('month')}-${part('day')}`
}
const monday = () => {
  const date = new Date(today() + 'T12:00:00Z')
  date.setUTCDate(date.getUTCDate() - ((date.getUTCDay() + 6) % 7))
  return date.toISOString().slice(0, 10)
}
const weekStart = ref(monday())
const productionRoles = [
  { id: 'CuttingWorker', name: 'Pracownik krojenia' },
  { id: 'SewingWorker', name: 'Pracownik szycia' },
]
const allRoles = [...productionRoles, { id: 'Admin', name: 'Administrator' }]
const roleNames = Object.fromEntries(allRoles.map(({ id, name }) => [id, name]))
const admin = computed(() => user.value?.role === 'Admin')
const canCut = computed(() => admin.value || user.value?.role === 'CuttingWorker')
const canSew = computed(() => admin.value || user.value?.role === 'SewingWorker')
const tabs = computed(() => [
  ['dashboard', 'Przegląd'],
  ...(canCut.value ? [['cuts', 'Krojenie']] : []),
  ...(canSew.value ? [['sews', 'Szycie'], ['batches', 'Partie wykrojów']] : []),
  ...(admin.value
    ? [
        ['reports', 'Raporty tygodniowe'],
        ['products', 'Produkty i normy'],
        ['materials', 'Materiały'],
        ['rolls', 'Belki materiału'],
        ['seamstresses', 'Chałupniczki'],
        ['users', 'Użytkownicy'],
      ]
    : []),
])
const title = computed(() => tabs.value.find((x) => x[0] === tab.value)?.[1] || '')
const availableBatches = computed(() => options.value.batches.filter((x) => x.available > 0))
const dashboardMetrics = computed(() => [
  ...(canCut.value
    ? [['queued', 'W kolejce do krojenia'], ['cutting', 'W trakcie krojenia']]
    : []),
  ...(canSew.value
    ? [
        ['readyBatches', 'Partie gotowe do przydziału'],
        ['availableSets', 'Komplety gotowe do szycia'],
        ['incomingSets', 'Komplety przed zakończeniem krojenia'],
        ['sewing', 'W trakcie szycia'],
        ['overdue', 'Opóźnione zlecenia szycia'],
      ]
    : []),
])
const labels = {
  Queued: 'W kolejce',
  Cutting: 'W trakcie krojenia',
  Cut: 'Skrojone',
  New: 'Nowe',
  Sewing: 'W trakcie szycia',
  Sewn: 'Uszyte',
  Pending: 'Oczekuje',
  Running: 'Generowanie',
  Completed: 'Gotowy',
  Failed: 'Błąd',
}
const isOrder = computed(() => ['cuts', 'sews'].includes(tab.value))
const catalogRows = computed(() =>
  (options.value[tab.value] || []).filter((x) =>
    `${x.name || x.product || ''} ${x.variant || ''}`
      .toLowerCase()
      .includes(search.value.toLowerCase()),
  ),
)
const canCreate = computed(
  () =>
    (tab.value === 'cuts' && canCut.value) ||
    (tab.value === 'sews' && canSew.value) ||
    (admin.value && ['materials', 'products', 'rolls', 'seamstresses', 'users'].includes(tab.value)),
)
function clearSession() {
  user.value = null
  tab.value = 'dashboard'
  modal.value = null
  history.value = null
  report.value = null
  dashboard.value = {}
  options.value = { materials: [], products: [], rolls: [], seamstresses: [], batches: [] }
  rows.value = []
  total.value = 0
  reports.value = []
  users.value = []
}
function handleError(e) {
  error.value = e.message
  if (e.status === 401) clearSession()
}
async function run(action) {
  if (busy.value) return
  busy.value = true
  error.value = ''
  notice.value = ''
  try {
    await action()
  } catch (e) {
    handleError(e)
  } finally {
    busy.value = false
  }
}
async function refresh() {
  options.value = await api('/catalog/options')
  if (tab.value === 'dashboard') dashboard.value = await api('/dashboard')
  if (isOrder.value) {
    const result = await api(
      `/${tab.value}?` +
        new URLSearchParams({
          page: page.value,
          search: search.value,
          ...(status.value ? { status: status.value } : {}),
          ...(tab.value === 'sews' && seamstressFilter.value
            ? { seamstressId: seamstressFilter.value }
            : {}),
        }),
    )
    rows.value = result.items
    total.value = result.total
  }
  if (tab.value === 'reports') reports.value = await api('/reports')
  if (tab.value === 'users') users.value = await api('/auth/users')
}
function applyFilters() {
  page.value = 1
  run(refresh)
}
function changePage(delta) {
  page.value += delta
  run(refresh)
}
function assignBatch(row) {
  open('sews')
  form.value.batchId = row.id
}
function changeUserRole(row) {
  modalError.value = ''
  modal.value = { kind: 'changeRole', id: row.id, version: row.version, name: row.name }
  form.value = { role: row.role }
}
async function signIn() {
  await run(async () => {
    user.value = await api('/auth/login', { method: 'POST', body: login.value })
    login.value.password = ''
    await refresh()
  })
}
async function logout() {
  await run(async () => {
    await api('/auth/logout', { method: 'POST' })
    clearSession()
  })
}
watch(tab, () => {
  page.value = 1
  search.value = ''
  status.value = ''
  seamstressFilter.value = ''
  report.value = null
  if (user.value) run(refresh)
})
function switchTab(value) {
  if (!busy.value && tabs.value.some(([key]) => key === value)) tab.value = value
}
const fields = computed(() => {
  const text = (key, label, type = 'text', min = 0, step = '0.0001') => ({
    key,
    label,
    type,
    min,
    step,
  })
  const select = (key, label, items) => ({ key, label, type: 'select', items })
  const materials = options.value.materials.map((x) => ({ id: x.id, name: x.name }))
  if (!modal.value) return []
  if (modal.value.kind === 'changeRole') return [select('role', 'Rola', productionRoles)]
  if (modal.value.kind === 'finishCut')
    return [
      text('actualQuantity', 'Poprawne komplety (szt.)', 'number', 0, '1'),
      text('actualMeters', 'Rzeczywiste zużycie materiału (m)', 'number'),
    ]
  const kind = modal.value.resource
  if (kind === 'materials') return [text('name', 'Nazwa materiału')]
  if (kind === 'seamstresses')
    return [
      text('name', 'Imię i nazwisko / nazwa'),
      { ...text('phone', 'Telefon (opcjonalny)'), optional: true },
    ]
  if (kind === 'users')
    return [
      text('name', 'Login (minimum 3 znaki)'),
      text('password', 'Hasło (minimum 12 znaków)', 'password'),
      select('role', 'Rola', allRoles),
    ]
  if (kind === 'rolls')
    return [
      text('name', 'Numer / nazwa belki'),
      select('materialId', 'Materiał', materials),
      text('widthCm', 'Szerokość (cm)', 'number', 1),
      text('remainingMeters', 'Pozostała długość, łącznie z rezerwacją (m)', 'number'),
    ]
  if (kind === 'products')
    return [
      text('name', 'Nazwa produktu'),
      text('variant', 'Wariant / rozmiar / kolor'),
      select('materialId', 'Materiał', materials),
      text('widthCm', 'Szerokość normy (cm)', 'number', 1),
      text('metersPerSet', 'Zużycie na komplet (m)', 'number', 0.0001),
      text('cutSetupMinutes', 'Przygotowanie krojenia (min)', 'number'),
      text('cutMinutesPerSet', 'Krojenie kompletu (min)', 'number'),
      text('cutSetupCost', 'Przygotowanie krojenia (PLN)', 'number'),
      text('cutCostPerSet', 'Krojenie kompletu (PLN)', 'number'),
      text('sewMinutesPerSet', 'Szycie kompletu (min)', 'number'),
      text('sewCostPerSet', 'Szycie kompletu (PLN)', 'number'),
    ]
  if (kind === 'cuts')
    return [
      ...(modal.value.id
        ? []
        : [
            select(
              'productId',
              'Produkt i wariant',
              options.value.products.map((x) => ({ id: x.id, name: `${x.name} · ${x.variant}` })),
            ),
            select(
              'rollId',
              'Belka materiału',
              compatibleRolls.value.map((x) => ({
                id: x.id,
                name: `${x.name} · ${number(x.availableMeters)} m dostępne`,
              })),
            ),
          ]),
      text('quantity', 'Liczba kompletów', 'number', 1, '1'),
      text('dueDate', 'Termin krojenia', 'date'),
    ]
  if (kind === 'sews')
    return [
      ...(modal.value.id
        ? []
        : [
            select(
              'batchId',
              'Partia wykrojów',
              availableBatches.value.map((x) => ({
                  id: x.id,
                  name: `#${x.id} · ${x.product} · ${x.available} dostępnych`,
                })),
            ),
          ]),
      select('seamstressId', 'Chałupniczka', options.value.seamstresses),
      text('quantity', 'Liczba kompletów', 'number', 1, '1'),
      text('dueDate', 'Termin szycia', 'date'),
    ]
  return []
})
const product = computed(() =>
  options.value.products.find((x) => x.id === Number(form.value.productId)),
)
const compatibleRolls = computed(() =>
  options.value.rolls.filter(
    (x) =>
      !product.value ||
      (x.materialId === product.value.materialId && x.widthCm === product.value.widthCm),
  ),
)
const estimate = computed(() => {
  if (modal.value?.id || modal.value?.kind !== 'edit') return null
  const q = Number(form.value.quantity) || 0
  if (modal.value.resource === 'cuts' && product.value) {
    const p = product.value,
      r = options.value.rolls.find((x) => x.id === Number(form.value.rollId))
    return {
      cost: p.cutSetupCost + q * p.cutCostPerSet,
      minutes: p.cutSetupMinutes + q * p.cutMinutesPerSet,
      meters: q * p.metersPerSet,
      max: r ? Math.floor(r.availableMeters / p.metersPerSet) : 0,
    }
  }
  if (modal.value.resource === 'sews') {
    const b = options.value.batches.find((x) => x.id === Number(form.value.batchId))
    if (b) return { cost: q * b.sewCostPerSet, minutes: q * b.sewMinutesPerSet, max: b.available }
  }
  return null
})
function open(resource = tab.value, row = null) {
  modalError.value = ''
  modal.value = { kind: 'edit', resource, id: row?.id, version: row?.version }
  form.value = row
    ? { ...row }
    : {
        name: '',
        variant: '',
        phone: '',
        role: 'CuttingWorker',
        materialId: '',
        widthCm: 160,
        remainingMeters: 0,
        metersPerSet: 1,
        cutSetupMinutes: 0,
        cutMinutesPerSet: 0,
        cutSetupCost: 0,
        cutCostPerSet: 0,
        sewMinutesPerSet: 0,
        sewCostPerSet: 0,
        productId: '',
        rollId: '',
        batchId: '',
        seamstressId: '',
        quantity: 1,
        dueDate: today(),
      }
}
async function save() {
  if (busy.value) return
  busy.value = true
  modalError.value = ''
  try {
    const m = modal.value
    if (m.kind === 'changeRole')
      await api(`/auth/users/${m.id}/role`, {
        method: 'PUT',
        body: { role: form.value.role, version: m.version },
      })
    else if (m.kind === 'finishCut')
      await api(`/cuts/${m.id}/status`, {
        method: 'POST',
        body: { ...form.value, version: m.version, status: 'Cut' },
      })
    else {
      const body = Object.fromEntries(
        fields.value.map((f) => [
          f.key,
          f.type === 'number' || (f.type === 'select' && f.key !== 'role')
            ? Number(form.value[f.key])
            : form.value[f.key],
        ]),
      )
      const path =
        m.resource === 'users'
          ? '/auth/users'
          : ['cuts', 'sews'].includes(m.resource)
            ? '/' + m.resource
            : '/catalog/' + m.resource
      await api(path + (m.id ? `/${m.id}?version=${encodeURIComponent(m.version)}` : ''), {
        method: m.id ? 'PUT' : 'POST',
        body: { ...body, ...(m.id ? { version: m.version } : {}) },
      })
    }
    modal.value = null
    await refresh()
    notice.value = 'Zapisano zmiany.'
  } catch (e) {
    if (modal.value) modalError.value = e.message
    else handleError(e)
    if (e.status === 401) handleError(e)
  } finally {
    busy.value = false
  }
}
async function advance(row) {
  if (tab.value === 'cuts' && row.status === 'Cutting') {
    modalError.value = ''
    modal.value = { kind: 'finishCut', id: row.id, version: row.version }
    form.value = { actualQuantity: row.quantity, actualMeters: row.estimatedMeters }
    return
  }
  await run(async () => {
    await api(`/${tab.value}/${row.id}/status`, {
      method: 'POST',
      body: {
        version: row.version,
        status: tab.value === 'cuts' ? 'Cutting' : row.status === 'New' ? 'Sewing' : 'Sewn',
      },
    })
    await refresh()
    notice.value = 'Zmieniono status.'
  })
}
async function remove(row) {
  if (!confirm('Usunąć rekord? Usunięcie nowego zlecenia zwalnia rezerwację.')) return
  await run(async () => {
    const path = isOrder.value ? '/' + tab.value : '/catalog/' + tab.value
    await api(`${path}/${row.id}?version=${encodeURIComponent(row.version)}`, { method: 'DELETE' })
    await refresh()
    notice.value = 'Usunięto rekord.'
  })
}
async function showHistory(row) {
  await run(async () => {
    history.value = { id: row.id, items: await api(`/history/${tab.value}/${row.id}`) }
  })
}
async function createReport() {
  await run(async () => {
    await api('/reports', { method: 'POST', body: { weekStart: weekStart.value } })
    await refresh()
    notice.value = 'Raport dodany do kolejki.'
  })
}
async function showReport(id) {
  await run(async () => {
    report.value = await api('/reports/' + id)
  })
}
function downloadReport() {
  const blob = new Blob([JSON.stringify(report.value.result, null, 2)], {
    type: 'application/json',
  })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `raport-${report.value.result.weekStart}.json`
  a.click()
  URL.revokeObjectURL(url)
}
onMounted(async () => {
  try {
    user.value = await api('/auth/me')
    await refresh()
  } catch (e) {
    if (e.status !== 401) handleError(e)
  } finally {
    checking.value = false
  }
  timer = setInterval(pollActiveTab, 5000)
})
async function pollActiveTab() {
  if (!user.value || busy.value || modal.value || polling || document.hidden) return
  const currentUser = user.value
  const currentTab = tab.value
  if (!['dashboard', 'reports'].includes(currentTab)) return
  const stillActive = () => user.value === currentUser && tab.value === currentTab
  polling = true
  try {
    if (currentTab === 'reports') {
      const result = await api('/reports')
      if (stillActive() && !busy.value) reports.value = result
    } else {
      const [summary, catalog] = await Promise.all([
        api('/dashboard'),
        canSew.value ? api('/catalog/options') : null,
      ])
      if (stillActive() && !busy.value && !modal.value) {
        dashboard.value = summary
        if (catalog) options.value = catalog
      }
    }
  } catch (e) {
    if (stillActive()) handleError(e)
  } finally {
    polling = false
  }
}
onUnmounted(() => clearInterval(timer))
</script>

<template>
  <div v-if="checking" class="grid min-h-screen place-items-center">Ładowanie aplikacji…</div>
  <main v-else-if="!user" class="grid min-h-screen place-items-center p-6">
    <form class="panel w-full max-w-md p-9" @submit.prevent="signIn">
      <span class="badge bg-mint text-forest">ZARZĄDZANIE PRODUKCJĄ</span>
      <h1 class="mb-2 mt-6 text-3xl font-bold tracking-tight text-forest">
        Szwalnia
      </h1>
      <p class="mb-8 text-sm text-slate-500">Od belki materiału do gotowego produktu.</p>
      <label class="mb-5"
        >Login<input
          v-model="login.name"
          class="mt-2"
          autocomplete="username"
          required
          maxlength="100"
      /></label>
      <label class="mb-6"
        >Hasło<input
          v-model="login.password"
          class="mt-2"
          type="password"
          autocomplete="current-password"
          required
          maxlength="200"
      /></label>
      <p v-if="error" role="alert" class="mb-4 text-sm text-red-700">{{ error }}</p>
      <button class="btn w-full" :disabled="busy">{{ busy ? 'Logowanie…' : 'Zaloguj się' }}</button>
    </form>
  </main>
  <div v-else class="min-h-screen lg:flex">
    <aside class="bg-forest p-5 text-white lg:fixed lg:inset-y-0 lg:w-64">
      <div class="mb-2 px-3 pt-3 text-2xl font-bold tracking-tight">
        Szwalnia
      </div>
      <p class="mb-9 px-3 text-xs text-emerald-100/60">KROJENIE · SZYCIE · REALIZACJA</p>
      <nav class="flex flex-wrap gap-1 lg:block lg:space-y-1" aria-label="Nawigacja główna">
        <button
          v-for="[key, label] in tabs"
          :key="key"
          :disabled="busy"
          :aria-current="tab === key ? 'page' : undefined"
          class="block rounded-lg px-3 py-2.5 text-left text-sm transition lg:w-full"
          :class="
            tab === key
              ? 'bg-mint font-semibold text-forest'
              : 'text-emerald-50/75 hover:bg-white/10'
          "
          @click="switchTab(key)"
        >
          {{ label }}
        </button>
      </nav>
      <div
        class="mt-8 border-t border-white/15 px-3 pt-5 lg:absolute lg:bottom-6 lg:left-5 lg:right-5"
      >
        <div class="text-sm font-semibold">{{ user.name }}</div>
        <div class="mt-1 text-xs text-emerald-100/60">
          {{ roleNames[user.role] }}
        </div>
        <button class="mt-4 text-xs underline" :disabled="busy" @click="logout">Wyloguj się</button>
      </div>
    </aside>
    <main class="min-w-0 flex-1 p-5 lg:ml-64 lg:p-10">
      <header class="mb-8 flex flex-wrap items-center justify-between gap-4">
        <div>
          <p class="mb-2 text-xs font-semibold uppercase tracking-widest text-emerald-700">
            Panel produkcji
          </p>
          <h1 class="text-3xl font-bold tracking-tight text-forest">{{ title }}</h1>
        </div>
        <button v-if="canCreate" class="btn" :disabled="busy" @click="open()">
          + {{ tab === 'users' ? 'Dodaj użytkownika' : 'Dodaj' }}
        </button>
      </header>
      <div
        v-if="error"
        role="alert"
        class="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
      >
        {{ error }} <button class="ml-3 underline" @click="run(refresh)">Odśwież dane</button>
      </div>
      <div v-if="notice" role="status" class="mb-5 rounded-xl bg-mint p-4 text-sm text-forest">
        {{ notice }}
      </div>
      <template v-if="tab === 'dashboard'">
        <div class="grid gap-4 sm:grid-cols-2" :class="canSew ? 'xl:grid-cols-4' : ''">
          <article
            v-for="[key, label] in dashboardMetrics"
            :key="key"
            class="panel p-5"
            :class="key === 'availableSets' ? 'bg-mint' : ''"
          >
            <p class="text-xs text-slate-500">{{ label }}</p>
            <p class="mt-4 text-4xl font-semibold text-forest">{{ number(dashboard[key]) }}</p>
          </article>
        </div>
        <section v-if="canSew" class="panel mt-7 p-6">
          <header class="flex flex-wrap items-center justify-between gap-3">
            <h2 class="text-xl font-semibold text-forest">Partie do przekazania do szycia</h2>
            <button class="btn secondary" :disabled="busy" @click="switchTab('batches')">
              Wszystkie partie
            </button>
          </header>
          <ul v-if="availableBatches.length" class="mt-4 divide-y divide-slate-200">
            <li
              v-for="batch in availableBatches.slice(0, 5)"
              :key="batch.id"
              class="flex flex-wrap items-center justify-between gap-3 py-4"
            >
              <div>
                <p class="font-semibold text-forest">#{{ batch.id }} · {{ batch.product }} · {{ batch.variant }}</p>
                <p class="mt-1 text-sm text-slate-500">Dostępne komplety: {{ number(batch.available) }}</p>
              </div>
              <button class="btn" :disabled="busy" @click="assignBatch(batch)">Przydziel chałupniczce</button>
            </li>
          </ul>
          <p v-else class="mt-4 text-sm text-slate-600">Brak gotowych kompletów do przydziału.</p>
        </section>
        <div v-if="canCut" class="mt-6 flex flex-wrap gap-3">
          <button class="btn" :disabled="busy" @click="open('cuts')">Dodaj zlecenie krojenia</button>
          <button class="btn secondary" :disabled="busy" @click="switchTab('cuts')">Lista zleceń krojenia</button>
        </div>
      </template>
      <template v-else-if="isOrder">
        <form
          class="mb-5 flex flex-wrap items-end gap-3"
          @submit.prevent="applyFilters"
        >
          <label class="min-w-56 flex-1"
            >Szukaj<input
              v-model="search"
              class="mt-1"
              placeholder="Produkt lub wykonawca" /></label
          ><label
            >Status<select v-model="status" class="mt-1">
              <option value="">Wszystkie</option>
              <option
                v-for="s in tab === 'cuts'
                  ? ['Queued', 'Cutting', 'Cut']
                  : ['New', 'Sewing', 'Sewn']"
                :key="s"
                :value="s"
              >
                {{ labels[s] }}
              </option>
            </select></label
          ><label v-if="tab === 'sews'"
            >Chałupniczka<select v-model="seamstressFilter" class="mt-1">
              <option value="">Wszystkie</option>
              <option v-for="s in options.seamstresses" :key="s.id" :value="s.id">
                {{ s.name }}
              </option>
            </select></label
          ><button class="btn secondary" :disabled="busy">Filtruj</button>
        </form>
        <div class="panel overflow-x-auto">
          <table>
            <thead>
              <tr>
                <th>Zlecenie / produkt</th>
                <th>{{ tab === 'cuts' ? 'Belka' : 'Chałupniczka' }}</th>
                <th>Ilość / kalkulacja</th>
                <th>Termin</th>
                <th>Status</th>
                <th>Akcje</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in rows" :key="row.id">
                <td>
                  <div class="font-semibold">#{{ row.id }} · {{ row.product }}</div>
                  <div class="mt-1 text-xs text-slate-500">{{ row.variant }}</div>
                </td>
                <td>{{ row.roll || row.seamstress }}</td>
                <td>
                  <div class="font-semibold">
                    {{ row.quantity }} kompletów · {{ money(row.estimatedCost) }}
                  </div>
                  <div class="mt-1 text-xs text-slate-500">
                    {{ number(row.estimatedMinutes) }} min
                    <span v-if="tab === 'cuts'">· {{ number(row.estimatedMeters) }} m</span>
                  </div>
                  <div
                    v-if="row.actualQuantity !== undefined && row.actualQuantity !== null"
                    class="mt-1 text-xs text-emerald-700"
                  >
                    Wykonano {{ row.actualQuantity }} · zużyto {{ number(row.actualMeters) }} m
                  </div>
                </td>
                <td class="whitespace-nowrap">{{ row.dueDate }}</td>
                <td>
                  <span
                    class="badge"
                    :class="['Cut', 'Sewn'].includes(row.status) ? 'bg-mint text-forest' : ''"
                    >{{ labels[row.status] }}</span
                  >
                </td>
                <td>
                  <div class="flex min-w-44 flex-wrap gap-2">
                    <button
                      v-if="!['Cut', 'Sewn'].includes(row.status)"
                      class="btn !px-3 !py-2 !text-xs"
                      :disabled="busy"
                      @click="advance(row)"
                    >
                      {{
                        ['Queued', 'New'].includes(row.status) ? 'Rozpocznij' : 'Zakończ'
                      }}</button
                    ><button
                      v-if="['Queued', 'New'].includes(row.status)"
                      class="btn secondary !px-3 !py-2 !text-xs"
                      :disabled="busy"
                      @click="open(tab, row)"
                    >
                      Edytuj</button
                    ><button
                      v-if="['Queued', 'New'].includes(row.status)"
                      class="btn danger !px-3 !py-2 !text-xs"
                      :disabled="busy"
                      @click="remove(row)"
                    >
                      Usuń</button
                    ><button
                      class="text-xs text-slate-500 underline"
                      :disabled="busy"
                      @click="showHistory(row)"
                    >
                      Historia
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="!rows.length">
                <td colspan="6" class="py-12 text-center text-slate-500">
                  Brak zleceń spełniających kryteria.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="mt-5 flex items-center justify-between text-sm text-slate-500">
          <span
            >{{ total }} zleceń · strona {{ page }} z {{ Math.max(1, Math.ceil(total / 20)) }}</span
          >
          <div class="flex gap-2">
            <button
              class="btn secondary"
              :disabled="busy || page === 1"
              @click="changePage(-1)"
            >
              ← Wstecz</button
            ><button
              class="btn secondary"
              :disabled="busy || page * 20 >= total"
              @click="changePage(1)"
            >
              Dalej →
            </button>
          </div>
        </div>
      </template>
      <template v-else-if="tab === 'reports'">
        <form class="panel mb-6 flex flex-wrap items-end gap-4 p-5" @submit.prevent="createReport">
          <label
            >Tydzień od poniedziałku<input
              v-model="weekStart"
              class="mt-2"
              type="date"
              required /></label
          ><button class="btn" :disabled="busy">Generuj raport</button>
          <p class="pb-2 text-xs text-slate-500">
            Raport obejmuje zakończenia w tygodniu według czasu Europe/Warsaw.
          </p>
        </form>
        <div class="panel overflow-x-auto">
          <table>
            <thead>
              <tr>
                <th>Tydzień od</th>
                <th>Status</th>
                <th>Wynik</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in reports" :key="r.id">
                <td>{{ r.weekStart }}</td>
                <td>
                  <span class="badge">{{ labels[r.status] }}</span>
                  <p v-if="r.error" class="mt-2 text-red-700">{{ r.error }}</p>
                </td>
                <td>
                  <button
                    v-if="r.status === 'Completed'"
                    class="btn secondary"
                    :disabled="busy"
                    @click="showReport(r.id)"
                  >
                    Otwórz raport
                  </button>
                </td>
              </tr>
              <tr v-if="!reports.length">
                <td colspan="3" class="py-10 text-center text-slate-500">
                  Nie wygenerowano jeszcze raportów.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <section v-if="report?.result" class="panel mt-6 p-6">
          <div class="flex flex-wrap justify-between gap-3">
            <h2 class="text-lg font-semibold">
              {{ report.result.weekStart }} — {{ report.result.weekEnd }}
            </h2>
            <button class="btn secondary" @click="downloadReport">Pobierz JSON</button>
          </div>
          <div class="mt-5 grid gap-4 sm:grid-cols-3">
            <div class="rounded-xl bg-mint p-5">
              <p class="text-sm">Skrojone komplety</p>
              <p class="mt-2 text-3xl font-bold">{{ report.result.cutQuantity }}</p>
              <p class="mt-2 text-sm">{{ money(report.result.cuttingCost) }}</p>
            </div>
            <div class="rounded-xl bg-slate-100 p-5">
              <p class="text-sm">Uszyte komplety</p>
              <p class="mt-2 text-3xl font-bold">{{ report.result.sewnQuantity }}</p>
              <p class="mt-2 text-sm">{{ money(report.result.sewingCost) }}</p>
            </div>
            <div class="rounded-xl bg-slate-100 p-5">
              <p class="text-sm">Zużyty materiał</p>
              <p class="mt-2 text-3xl font-bold">{{ number(report.result.materialMeters) }} m</p>
            </div>
          </div>
          <div class="mt-5 overflow-x-auto">
            <table>
              <thead>
                <tr>
                  <th>Etap</th>
                  <th>Zlecenie</th>
                  <th>Produkt / wykonawca</th>
                  <th>Ilość</th>
                  <th>Koszt</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="c in report.result.cuts" :key="'c' + c.id">
                  <td>Krojenie</td>
                  <td>#{{ c.id }}</td>
                  <td>{{ c.product }} · {{ c.variant }}</td>
                  <td>{{ c.quantity }}</td>
                  <td>{{ money(c.cost) }}</td>
                </tr>
                <tr v-for="s in report.result.sews" :key="'s' + s.id">
                  <td>Szycie</td>
                  <td>#{{ s.id }}</td>
                  <td>{{ s.product }} · {{ s.seamstress }}</td>
                  <td>{{ s.quantity }}</td>
                  <td>{{ money(s.cost) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </template>
      <template v-else-if="tab === 'users'"
        ><div class="panel overflow-x-auto">
          <table>
            <thead>
              <tr>
                <th>Login</th>
                <th>Rola</th>
                <th>Akcje</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="u in users" :key="u.id">
                <td>{{ u.name }}</td>
                <td>{{ roleNames[u.role] }}</td>
                <td>
                  <button
                    v-if="u.role !== 'Admin'"
                    class="btn secondary"
                    :disabled="busy"
                    @click="changeUserRole(u)"
                  >
                    Zmień rolę
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div></template
      >
      <template v-else>
        <label class="mb-5 max-w-md"
          >Szukaj<input v-model="search" class="mt-1" placeholder="Wpisz nazwę lub wariant"
        /></label>
        <p v-if="tab === 'products'" class="mb-4 text-sm text-slate-500">
          Normy dotyczą kompletów na jeden produkt. Stawki są kopiowane do nowych zleceń.
        </p>
        <div class="panel overflow-x-auto">
          <table>
            <thead>
              <tr>
                <th>{{ tab === 'batches' ? 'Partia / produkt' : 'Nazwa' }}</th>
                <th>Szczegóły</th>
                <th>Akcje</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in catalogRows" :key="row.id">
                <td>
                  <div class="font-semibold">
                    {{ row.name || '#' + row.id + ' · ' + row.product }}
                  </div>
                  <div class="mt-1 text-xs text-slate-500">{{ row.variant }}</div>
                </td>
                <td>
                  <template v-if="tab === 'products'"
                    ><p>
                      {{ number(row.metersPerSet) }} m / komplet · szerokość {{ row.widthCm }} cm
                    </p>
                    <p class="mt-1 text-xs text-slate-500">
                      Krojenie: {{ money(row.cutCostPerSet) }}/komplet +
                      {{ money(row.cutSetupCost) }} · szycie: {{ money(row.sewCostPerSet) }}/komplet
                    </p></template
                  ><template v-if="tab === 'rolls'"
                    ><p>
                      {{ number(row.availableMeters) }} m dostępne ·
                      {{ number(row.reservedMeters) }} m zarezerwowane
                    </p>
                    <p class="mt-1 text-xs text-slate-500">
                      Szerokość {{ row.widthCm }} cm ·
                      {{ options.materials.find((m) => m.id === row.materialId)?.name }}
                    </p></template
                  ><template v-if="tab === 'batches'"
                    ><p>{{ row.available }} dostępnych / {{ row.quantity }} skrojonych kompletów</p>
                    <p class="mt-1 text-xs text-slate-500">
                      Zlecenie krojenia #{{ row.cutOrderId }} · przydzielono
                      {{ row.allocatedQuantity }}
                    </p></template
                  ><template v-if="tab === 'seamstresses'">{{ row.phone || '—' }}</template
                  ><template v-if="tab === 'materials'">Materiał produkcyjny</template>
                </td>
                <td>
                  <div class="flex gap-2">
                    <button
                      v-if="tab === 'batches' && row.available > 0"
                      class="btn secondary"
                      :disabled="busy"
                      @click="assignBatch(row)"
                    >
                      Przekaż do szycia</button
                    ><template v-else-if="canCreate"
                      ><button class="btn secondary" :disabled="busy" @click="open(tab, row)">
                        Edytuj</button
                      ><button class="btn danger" :disabled="busy" @click="remove(row)">
                        Usuń
                      </button></template
                    >
                  </div>
                </td>
              </tr>
              <tr v-if="!catalogRows.length">
                <td colspan="3" class="py-12 text-center text-slate-500">
                  Brak danych do wyświetlenia.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </main>
    <div
      v-if="modal"
      class="fixed inset-0 z-20 flex items-center justify-center overflow-y-auto bg-slate-950/50 p-4"
      @click.self="!busy && (modal = null)"
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        class="panel max-h-[90vh] w-full max-w-2xl overflow-y-auto p-6"
        @keydown.esc="!busy && (modal = null)"
      >
        <div class="mb-6 flex items-center justify-between">
          <h2 id="modal-title" class="text-xl font-bold">
            {{
              modal.kind === 'changeRole'
                ? `Zmień rolę konta ${modal.name}`
                : modal.kind === 'finishCut'
                ? 'Zakończ krojenie'
                : modal.id
                  ? 'Edytuj dane'
                  : 'Dodaj nowe dane'
            }}
          </h2>
          <button class="btn secondary" :disabled="busy" aria-label="Zamknij" @click="modal = null">
            ×
          </button>
        </div>
        <form @submit.prevent="save">
          <div class="grid gap-4 sm:grid-cols-2">
            <label v-for="field in fields" :key="field.key"
              >{{ field.label
              }}<select
                v-if="field.type === 'select'"
                v-model="form[field.key]"
                class="mt-2"
                required
              >
                <option value="" disabled>Wybierz…</option>
                <option v-for="item in field.items" :key="item.id" :value="item.id">
                  {{ item.name }}
                </option></select
              ><input
                v-else
                v-model="form[field.key]"
                class="mt-2"
                :type="field.type"
                :required="!field.optional"
                :min="field.type === 'number' ? field.min : undefined"
                :step="field.type === 'number' ? field.step : undefined"
                :maxlength="field.type === 'password' ? 200 : 150"
                :minlength="field.type === 'password' ? 12 : undefined"
                :autocomplete="field.type === 'password' ? 'new-password' : undefined"
            /></label>
          </div>
          <div v-if="estimate" class="mt-5 rounded-xl bg-mint p-4 text-sm">
            <p class="font-semibold">
              {{ money(estimate.cost) }} · {{ number(estimate.minutes) }} min
            </p>
            <p class="mt-1">
              Maksymalnie {{ estimate.max }} kompletów
              <span v-if="estimate.meters !== undefined"
                >· zapotrzebowanie {{ number(estimate.meters) }} m</span
              >
            </p>
          </div>
          <p
            v-if="modalError"
            role="alert"
            class="mt-5 rounded-lg bg-red-50 p-3 text-sm text-red-800"
          >
            {{ modalError }}
          </p>
          <div class="mt-6 flex justify-end gap-3">
            <button type="button" class="btn secondary" :disabled="busy" @click="modal = null">
              Anuluj</button
            ><button class="btn" :disabled="busy">{{ busy ? 'Zapisywanie…' : 'Zapisz' }}</button>
          </div>
        </form>
      </section>
    </div>
    <div
      v-if="history"
      class="fixed inset-0 z-20 grid place-items-center bg-slate-950/50 p-4"
      @click.self="history = null"
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-label="Historia statusów"
        class="panel max-h-[85vh] w-full max-w-lg overflow-y-auto p-6"
      >
        <h2 class="text-xl font-bold">Historia zlecenia #{{ history.id }}</h2>
        <ol class="my-6 space-y-4">
          <li v-for="(h, i) in history.items" :key="i" class="border-l-2 border-emerald-300 pl-4">
            <p class="font-semibold">{{ labels[h.status] }}</p>
            <p class="mt-1 text-xs text-slate-500">
              {{ new Date(h.changedAt).toLocaleString('pl-PL', { timeZone: 'Europe/Warsaw' }) }} ·
              {{ h.user }}
            </p>
          </li>
        </ol>
        <button class="btn secondary" @click="history = null">Zamknij</button>
      </section>
    </div>
  </div>
</template>
