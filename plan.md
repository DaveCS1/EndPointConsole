## Full build plan (CI/CD last)

### 0) Constraints and non-negotiables (tell the AI up front)

* This is **WPF on .NET 8**.
* Use **MVVM with CommunityToolkit.Mvvm**.
* **No Windows API calls in ViewModels**. All system access goes through interfaces in Core and implementations in System.
* All XAML must be **responsive** using the **WPF Grid system** (`Auto`/`*`, `SharedSizeGroup`, `DynamicResource` styles).

  * **No hard-coded sizes/fonts** (no fixed widths/heights; no hardcoded FontSize).
* Use **structured logging**.
* Include **unit tests**.
* Include at least one **Win32 P/Invoke** feature (visible in UI).
* Include a measurable **.NET 8 performance improvement**.
* Add **PerfView** workflow + “performance tests” (repeatable scenarios).
* **Reminder: don’t forget value converters and declare them in XAML** (ResourceDictionary merged in `App.xaml`, used via `{StaticResource ...}`).

---

## 1) Solution structure (projects)

```
EndpointConsole.sln
├─ EndpointConsole.Wpf            // WPF UI: Views, ViewModels, Resources (styles), XAML converters declared in XAML
├─ EndpointConsole.Core           // Models, interfaces, use-cases (pure; no Registry/ServiceController/WMI)
├─ EndpointConsole.System         // Windows implementations + P/Invoke wrappers
├─ EndpointConsole.Perf           // Console perf harness (repeatable scenarios for PerfView)
└─ EndpointConsole.Tests          // Unit tests (Core + ViewModels with fakes/mocks)
```

---

## 2) Milestones

### Milestone 1 — WPF shell + MVVM + DI + navigation

**Goal:** running app with a real structure.

* Add CommunityToolkit.Mvvm
* Create `MainWindow` shell with navigation
* Pages:

  * Dashboard
  * Services
  * Diagnostics
* Use `Grid` layout everywhere (`Auto` header row + `*` content row)
* Add app-wide `Styles.xaml` (typography, spacing, button styles) using resources (no hardcoded fonts)

**Deliverables**

* App runs and resizes cleanly
* ViewModels are DI-created, no code-behind logic

---

### Milestone 2 — Converters (explicit “don’t forget” requirement)

**Goal:** ensure converters exist and are wired correctly in XAML.

* Create `Resources/Converters.xaml` ResourceDictionary
* Merge in `App.xaml` via `MergedDictionaries`
* Implement + use converters (at least 2–4):

  * `BoolToVisibility`
  * `NullToBool` (for button enablement)
  * `ServiceStatusToText` or `ServiceStatusToIcon`
  * `SeverityToVisibility` (event log filtering)

**Deliverables**

* Converters **declared in XAML** and used in views via `{StaticResource ...}`

---

### Milestone 3 — Logging + supportability baseline

**Goal:** production-style diagnostics.

* Add `Microsoft.Extensions.Logging`
* Choose a file sink (Serilog recommended) writing under:

  * `%LocalAppData%\EndpointConsole\Logs`
* Add correlation IDs per user operation (e.g., “Collect Bundle”)
* Add global exception handling:

  * user-friendly error message
  * detailed logs with context

**Deliverables**

* Logs written and include operation durations + errors

---

### Milestone 4 — System features (job-description alignment)

All system work is behind interfaces in Core.

**Modules**

1. **OS / machine snapshot**

   * OS version/build, uptime, domain join, disk usage
2. **Windows Services**

   * list services + start/stop/restart (`ServiceController`)
3. **Event Logs**

   * show recent errors/warnings with filtering
4. **Registry**

   * read/write app config keys (with 64-bit view support)
5. **Permissions**

   * inspect folder ACLs (ProgramData, install folder) and report issues

**UI**

* Dashboard shows snapshot cards + issues list
* Services page manages services
* Diagnostics page shows event log issues and supports “collect bundle”

**Deliverables**

* End-to-end UI flows for each module

---

### Milestone 5 — Win32 P/Invoke feature (must be visible)

**Goal:** demonstrate low-level Windows interop.

**Recommended feature:** **Enumerate interactive user sessions**

* P/Invoke:

  * `WTSEnumerateSessions`
  * `WTSQuerySessionInformation`
  * `WTSFreeMemory`
* Show on Dashboard:

  * session id, state, username, domain (where available)

**Architecture**

* Core: `ISessionEnumerator`
* System: `WtsSessionEnumerator` (P/Invoke wrapper)
* VM: `DashboardViewModel` calls interface only

**Deliverables**

* Sessions table appears in UI and refreshes

---

### Milestone 6 — Support bundle builder (enterprise workflow)

**Goal:** “ticketing / production support” realism.

* “Collect Diagnostics” button:

  * app logs
  * selected event log entries
  * service status snapshot
  * registry export of app keys
  * machine snapshot JSON
* Writes:

  * `manifest.json`
  * `bundle.zip` output folder
* Progress UI + cancellation

**Deliverables**

* Generates a zip reliably with manifest + files
* Logs include correlation ID for that run

---

### Milestone 7 — .NET 8 performance improvement (measurable)

**Goal:** implement baseline + optimized path, measurable with PerfView.

Pick ONE hot path to optimize (recommended: bundle creation + JSON + zipping).

* Baseline: straightforward JSON serialization + file IO
* Optimized (.NET 8):

  * `System.Text.Json` **source-generated** serialization for manifest/DTOs **and/or**
  * pooled buffers (`ArrayPool<byte>`) for hashing/zipping **and/or**
  * Span-based parsing for event log transforms

**Deliverables**

* Two code paths: `Baseline` and `Optimized`
* Same output, faster/less allocation in optimized mode

---

### Milestone 8 — Performance tests + PerfView workflow

**Goal:** repeatable profiling and evidence.

**Approach**

* Create `EndpointConsole.Perf` console app:

  * `Scenario_CollectBundle_Baseline(iterations)`
  * `Scenario_CollectBundle_Optimized(iterations)`
  * optional: `Scenario_ParseEvents_*`
* Warmup + N iterations
* Output timings to console + log file

**PerfView usage**

* Document running PerfView against:

  1. **Console perf harness** (repeatable, low noise)
  2. **WPF exe** (startup + “Collect Diagnostics” click path; higher noise but real UX)

**Deliverables**

* `docs/perf/perfview.md` with step-by-step commands
* Example trace naming convention and what to inspect (CPU stacks, GC, allocations)

---

### Milestone 9 — Unit tests (quality + design patterns)

**Goal:** show “unit testing and best practices” explicitly.

**Tests** we will use nunit

* Core rules engine:

  * health evaluation logic
  * bundle manifest composition
* ViewModels:

  * command enablement (selection/null)
  * cancellation behavior
  * error handling paths
* System layer:

  * minimal “safe” tests with fakes; avoid admin-dependent tests unless optional

**Deliverables**

* Solid unit test suite (Core-heavy)
* Tests run fast and deterministically

---

### Milestone 10 — Packaging (WiX MSI) + upgrade path

**Goal:** show Windows packaging & deployment competence.

**Installer**

* `EndpointConsole.Installer` using WiX
* Installs app to Program Files
* Start Menu shortcut
* Writes install/version registry keys
* **MajorUpgrade** configured (upgrade older; block downgrades)

**Deliverables**

* MSI installs/uninstalls cleanly
* Upgrade v1.0 → v1.1 works

---

### Milestone 11 — CI/CD (last)

**Goal:** automate once stable.

* Build + test + package MSI
* Publish artifacts

(Optionally add signing later.)

**Deliverables**

* Pipeline definition (GitHub Actions or Azure DevOps)
* Produces MSI artifact on main/tag builds

---

## 3) UI/XAML implementation rules (to keep it consistent)

* Use `Grid` with `Auto` headers + `*` content
* Avoid fixed sizes; prefer:

  * `MinWidth` only when absolutely necessary
  * `TextWrapping="Wrap"`
  * `SharedSizeGroup` for aligned forms
* Put styles in `Resources/Styles.xaml`
* Put converters in `Resources/Converters.xaml` and **merge** in `App.xaml`
* Use `DynamicResource` for spacing/typography keys (lets theme + scaling work)

---

## 4) “Definition of Done” checklist

* App resizes cleanly; no clipped layouts at common DPI scales
* No hard-coded fonts/sizes
* Converters exist and are declared in XAML; used in bindings
* P/Invoke feature is visible and works
* Logs exist and include correlation IDs
* Baseline vs optimized performance scenario exists and is measurable
* PerfView docs exist for console + WPF
* Unit tests pass
* MSI upgrade path works (later milestone)

If you want, I can also produce a **repo-ready “AI Build Spec.md”** version of this (short, strict, copy-pasteable) to keep the implementation consistent.
