# Clean Architecture — a short, general explanation

This note explains Clean Architecture in general terms, independent of Zedo Go.
For how it maps to this project, see the main README.

## The core idea

Software is split into concentric layers. The most important rule is the
**Dependency Rule**: source code dependencies only point **inward**. Inner
layers know nothing about outer layers.

```
        ┌─────────────────────────────────────────┐
        │  Frameworks & Drivers (Web, DB, UI)       │   outer
        │   ┌───────────────────────────────────┐   │
        │   │  Interface Adapters                │   │
        │   │   ┌───────────────────────────┐   │   │
        │   │   │  Application (Use Cases)   │   │   │
        │   │   │   ┌───────────────────┐    │   │   │
        │   │   │   │   Domain (Entities)│    │   │   │   inner
        │   │   │   └───────────────────┘    │   │   │
        │   │   └───────────────────────────┘   │   │
        │   └───────────────────────────────────┘   │
        └─────────────────────────────────────────┘
                dependencies point inward →
```

## The layers

- **Domain (center):** the business entities and rules. Pure code with no
  dependency on any framework, database, or UI. This is the part that would
  stay true even if you changed everything around it.

- **Application (use cases):** orchestrates the domain to perform actions
  ("register a user", "complete a ride"). It defines *interfaces* (ports) for
  the things it needs from the outside (e.g. "a user repository") but does not
  implement them.

- **Interface Adapters:** convert data between the use cases and the outside
  world — controllers, presenters, DTOs, repository implementations.

- **Frameworks & Drivers (outer):** the web framework, the database, external
  services. Details that can be swapped.

## Why the dependency rule matters

Because inner layers depend only on abstractions they define themselves, the
outer details become **plug‑ins**:

- Swap the database (SQLite → MySQL) without touching business rules.
- Swap the delivery mechanism (REST → gRPC, add a mobile BFF) the same way.
- Test the domain and use cases in isolation, with no database or web server.

The technique that makes this possible is **Dependency Inversion**: the inner
layer declares an interface, and the outer layer implements it. At runtime a
dependency‑injection container wires the concrete implementation in.

## Trade‑offs (be honest)

- More layers and indirection than a "throw it all in the controller" app.
- Extra mapping (DTOs ↔ entities) and interfaces.
- Overkill for a tiny script; it pays off as the app grows and the team scales.

The benefit is long‑term: the code that expresses *what the business does*
stays stable and testable, while the volatile details (frameworks, databases,
UIs) can change without rippling through the whole system.

## Further reading

- Robert C. Martin, *Clean Architecture* (the book) and his original
  "The Clean Architecture" article.
- Related ideas: Hexagonal Architecture (Ports & Adapters), Onion Architecture.
