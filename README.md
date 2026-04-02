# Serilog Showcase

## Worum geht es?

Dieses Projekt demonstriert den Migrationspfad:

```
Custom Logger  →  ILogger<T> Facade  →  Serilog als Provider
```

**Entscheidender Punkt:** Der Service-Code ändert sich beim Wechsel zu Serilog **nicht**.
Alle Services nutzen `ILogger<T>` – ob dahinter der Legacy-Logger oder Serilog steckt,
wird ausschließlich in der DI-Konfiguration entschieden.

---

## Schnellstart

```bash
dotnet restore
dotnet run
```

Das Programm führt nacheinander alle Phasen durch und zeigt den direkten
Vergleich zwischen Legacy- und Serilog-Output.

---

## Projektstruktur

```
SerilogShowcase/
├── Configuration/
│   └── SerilogSetup.cs           ← Zentrales Serilog-Setup (DI, Enrichers, Sinks)
├── CustomSinks/
│   └── CustomSinks.cs            ← InMemorySink, BatchingSink + AsyncDatabaseSink + Extension Methods
├── Logging/
│   ├── Legacy/
│   │   └── LegacyLogger.cs       ← Simuliert bestehenden Custom-Logger
│   ├── Providers/
│   │   └── LegacyLoggerProvider.cs ← Bridge: Legacy-Logger → ILoggerProvider
│   └── CustomEnrichers.cs        ← CorrelationId + AssemblyVersion Enricher
├── Models/
│   └── DemoModels.cs             ← Order, UserProfile DTOs
├── Redaction/
│   └── SensitiveDataDestructuringPolicy.cs ← Maskierung sensibler Daten
├── Services/
│   ├── OrderService.cs           ← Demo: Structured Logging, Scopes, Exceptions
│   ├── UserService.cs            ← Demo: Redaction, CorrelationId
│   └── NoisyService.cs           ← Demo: Level Overrides
├── Program.cs                    ← Hauptprogramm mit allen Demo-Phasen
├── appsettings.json              ← Serilog-Konfiguration (Sinks, Level, Filter)
└── README.md
```

---

## Was wird demonstriert?

### 1. Migrations-Brücke (LegacyLoggerProvider)

Die Klasse `LegacyLoggerProvider` implementiert `ILoggerProvider` und leitet
alle `ILogger<T>`-Aufrufe an den bestehenden `ILegacyLogger` weiter.

**Migration in 3 Schritten:**
1. Services auf `ILogger<T>` umstellen
2. `LegacyLoggerProvider` registrieren → bestehender Logger arbeitet weiter
3. `LegacyLoggerProvider` durch `UseSerilog()` ersetzen → fertig!

### 2. Structured Logging (Message Templates)

```csharp
// ❌ String-Interpolation (flacher Text, nicht durchsuchbar)
logger.LogInformation($"Order {order.OrderId} processed");

// ✅ Message Template (Properties als Schlüssel-Wert-Paare)
logger.LogInformation("Order {OrderId} processed", order.OrderId);
```

Serilog speichert `OrderId=4711` als eigenständiges Property.
In Seq/Elasticsearch kann man danach filtern!

### 3. Object Destructuring mit `@`

```csharp
logger.LogInformation("Bestellung: {@Order}", order);
```

Das `@` sagt Serilog: Zerlege das Objekt in seine Properties.
Zusammen mit der `SensitiveDataDestructuringPolicy` werden dabei
Kreditkartennummern, E-Mails etc. automatisch maskiert.

### 4. Redaction / Sensitive Data

`SensitiveDataDestructuringPolicy` implementiert `IDestructuringPolicy` und greift
automatisch, wenn Objekte vom Typ `Order` oder `UserProfile` destrukturiert werden.

**Vorteile gegenüber manueller Maskierung:**
- Zentral definiert, gilt für ALLE Sinks gleichzeitig
- Testbar (Unit-Tests auf die Policy)
- Kann nicht vergessen werden – greift automatisch

### 5. Custom Sinks

**InMemorySink:** Sammelt Log-Events im Speicher. Nützlich für:
- Unit-Tests ("wurde die richtige Meldung geloggt?")
- Health-Endpoints ("zeige letzte 50 Logs")
- Debug-UI in der Anwendung

**BatchingSink:** Sammelt Events und verarbeitet sie in Batches.
In der Praxis für: Datenbank-Inserts, API-Calls, Message Queues.

Beide folgen der Serilog-Konvention:
- `ILogEventSink` implementieren
- Extension Method auf `LoggerSinkConfiguration` bereitstellen
- Nutzung via `.WriteTo.InMemory()` / `.WriteTo.Batching()`

### 6. Custom Enrichers

Enrichers hängen automatisch Properties an JEDES Log-Event an.

- **CorrelationIdEnricher:** AsyncLocal-basiert, Request-übergreifend
- **AssemblyVersionEnricher:** Build-Version für Deployment-Tracking

### 7. appsettings.json Konfiguration

Die `appsettings.json` zeigt:
- **MinimumLevel Overrides:** Pro Namespace unterschiedliche Log-Level
- **Multiple Sinks:** Console + File (alle Logs) + File (nur Errors)
- **Rolling Files:** Tägliches Rotieren mit Retention-Limit
- **Filter mit Expressions:** z.B. HealthCheck-Messages ausschließen
- **Enricher-Registrierung:** Machine, Thread, Process
- **Globale Properties:** Application-Name, Environment

### 8. Level Overrides

```json
"Override": {
    "SerilogShowcase.Services.NoisyService": "Warning"
	...
}
```

Der `NoisyService` zeigt, dass Info/Debug-Logs dieses Namespaces
unterdrückt werden, während Warnings durchkommen.

---

## Serilog vs. Custom Logger – Argumentationshilfe

| Aspekt | Custom Logger | Serilog |
|--------|--------------|---------|
| Structured Logging | Muss selbst gebaut werden | Out of the box |
| Sinks (Ziele) | Selbst implementieren | 100+ Community Sinks |
| Konfiguration | Eigenes Format | appsettings.json Standard |
| Enrichers | Manuell pro Log-Call | Automatisch global |
| Redaction | Pro Aufruf maskieren | Zentrale Policy |
| Performance | Unbekannt | Benchmarked & optimiert |
| Filtering | If/else-Logik | Expressions, Level Overrides |
| Community | Interne Wartung | Riesige Community & Support |
| Testbarkeit | Eigene Test-Infrastruktur | InMemorySink, Test-Helpers |
| Kosten | Wartung durch euer Team | Open Source, Community-maintained |

---

## Nächste Schritte

1. **Jetzt:** Services auf `ILogger<T>` umgestellt
2. **Parallel:** Serilog im DI registrieren (neben dem alten, zum Testen)
3. **Dann:** Legacy-Provider entfernen, Serilog übernimmt
4. **Optional:** Seq oder ähnliches für zentrales Log-Management einführen
5. **Optional:** Serilog.AspNetCore für Request-Logging in Web-APIs
